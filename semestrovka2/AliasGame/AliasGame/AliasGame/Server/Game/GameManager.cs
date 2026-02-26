using System.Text.Json;
using System.Timers;
using AliasGame.Shared.Models;
using AliasGame.Shared.Protocol;
using AliasGame.Shared.Protocol.Packets;
using AliasGame.Shared.ORM;
using AliasGame.Server.Network;
using Serilog;
using Timer = System.Timers.Timer;

namespace AliasGame.Server.Game;

public class GameManager
{
    private readonly LobbyManager _lobbyManager;
    private readonly SessionManager _sessionManager;
    private readonly WordService _wordService;
    private readonly GameHistoryService _gameHistoryService;
    private readonly UserService _userService;
    
    private readonly Dictionary<int, Timer> _gameTimers = new();
    private readonly Dictionary<int, DateTime> _gameStartTimes = new();

    public GameManager(
        LobbyManager lobbyManager, 
        SessionManager sessionManager,
        WordService wordService,
        GameHistoryService gameHistoryService,
        UserService userService)
    {
        _lobbyManager = lobbyManager;
        _sessionManager = sessionManager;
        _wordService = wordService;
        _gameHistoryService = gameHistoryService;
        _userService = userService;
    }

                public async Task<(bool Success, string Message)> StartGameAsync(ClientSession hostSession)
    {
        if (!hostSession.LobbyId.HasValue)
            return (false, "Вы не в лобби");

        var lobby = _lobbyManager.GetLobby(hostSession.LobbyId.Value);
        if (lobby == null)
            return (false, "Лобби не найдено");

        if (lobby.HostId != hostSession.UserId)
            return (false, "Только хост может начать игру");

        var (canStart, validationMessage) = _lobbyManager.ValidateGameStart(lobby);
        if (!canStart)
            return (false, validationMessage);

                lobby.Teams.RemoveAll(t => t.Players.Count == 0);

                await StartCountdownAsync(lobby);

        return (true, "Игра начинается");
    }

                private async Task StartCountdownAsync(Lobby lobby)
    {
        lobby.State = GameState.Starting;

        for (int i = 3; i > 0; i--)
        {
            var countdownPacket = XPacketConverter.Serialize(XPacketType.GameCountdown, new XPacketGameCountdown
            {
                SecondsRemaining = i,
                Message = $"ИГРА НАЧИНАЕТСЯ ЧЕРЕЗ {i}..."
            });
            _sessionManager.BroadcastToLobby(lobby.Id, countdownPacket);
            
            await Task.Delay(1000);
        }

                await InitializeGameAsync(lobby);
    }

                private async Task InitializeGameAsync(Lobby lobby)
    {
        lobby.State = GameState.Playing;
        lobby.CurrentRound = 0;
        lobby.CurrentTeamIndex = 0;
        lobby.UsedWordIds.Clear();

        _gameStartTimes[lobby.Id] = DateTime.UtcNow;

                var gameStartedPacket = XPacketConverter.Serialize(XPacketType.GameStarted, new XPacketGameStarted
        {
            TeamsJson = JsonSerializer.Serialize(lobby.Teams),
            TotalRounds = lobby.Settings.TotalRounds,
            FirstTeamId = lobby.Teams[0].Id,
            FirstExplainerId = lobby.Teams[0].Players[0].Id
        });
        _sessionManager.BroadcastToLobby(lobby.Id, gameStartedPacket);

        Log.Information("Game started in lobby {LobbyId}", lobby.Id);

                await StartRoundAsync(lobby);
    }

                public async Task StartRoundAsync(Lobby lobby)
    {
        lobby.StartNewRound();

        var currentTeam = lobby.CurrentTeam!;
        var explainer = currentTeam.CurrentExplainer!;

                var roundStartedPacket = XPacketConverter.Serialize(XPacketType.RoundStarted, new XPacketRoundStarted
        {
            RoundNumber = lobby.CurrentRound,
            ExplainingTeamId = currentTeam.Id,
            ExplainerId = explainer.Id,
            ExplainerName = explainer.Username,
            TimeRemainingSeconds = lobby.Settings.RoundTimeSeconds
        });
        _sessionManager.BroadcastToLobby(lobby.Id, roundStartedPacket);

                await SendNextWordAsync(lobby);

                StartRoundTimer(lobby);

        Log.Information("Round {Round} started in lobby {LobbyId}, explainer: {Explainer}",
            lobby.CurrentRound, lobby.Id, explainer.Username);
    }

                public async Task SendNextWordAsync(Lobby lobby)
    {
        var word = await _wordService.GetRandomWordAsync(
            lobby.Settings.CategoryId > 0 ? lobby.Settings.CategoryId : null,
            lobby.UsedWordIds);

        if (word == null)
        {
                        Log.Warning("No more words available in lobby {LobbyId}", lobby.Id);
            await EndRoundAsync(lobby);
            return;
        }

        lobby.CurrentWord = word.WordText;
        lobby.CurrentWordId = word.Id;
        lobby.UsedWordIds.Add(word.Id);

                var explainerSession = _sessionManager.GetSessionByUserId(lobby.CurrentExplainer!.Id);
        if (explainerSession != null)
        {
            var wordPacket = XPacketConverter.Serialize(XPacketType.WordUpdate, new XPacketWordUpdate
            {
                Word = word.WordText,
                WordId = word.Id,
                Category = word.Category?.Name ?? "Общие"
            });
            explainerSession.Send(wordPacket);
        }
    }

                public async Task WordGuessedAsync(Lobby lobby)
    {
        if (lobby.CurrentWordId == null) return;

        var currentTeam = lobby.CurrentTeam!;
        currentTeam.Score += 1;

        lobby.GuessedWordsThisRound.Add(lobby.CurrentWord!);

                await _wordService.RecordWordUsageAsync(lobby.CurrentWordId.Value, true);

                var wordGuessedPacket = XPacketConverter.Serialize(XPacketType.WordGuessed, new XPacketWordGuessed
        {
            WordId = lobby.CurrentWordId.Value,
            Word = lobby.CurrentWord!,
            PointsAwarded = 1,
            TotalTeamScore = currentTeam.Score
        });
        _sessionManager.BroadcastToLobby(lobby.Id, wordGuessedPacket);

                BroadcastScoreUpdate(lobby, currentTeam.Id, 1, 0);

                if (currentTeam.Score >= lobby.Settings.ScoreToWin)
        {
            await EndGameAsync(lobby, currentTeam.Id);
            return;
        }

                if (!lobby.IsLastWordPhase)
        {
            await SendNextWordAsync(lobby);
        }
    }

                public async Task WordSkippedAsync(Lobby lobby)
    {
        if (lobby.CurrentWordId == null) return;

        var currentTeam = lobby.CurrentTeam!;
        var penalty = lobby.Settings.SkipPenalty;

        if (penalty > 0)
        {
            currentTeam.Score = Math.Max(0, currentTeam.Score - penalty);
        }

                await _wordService.RecordWordUsageAsync(lobby.CurrentWordId.Value, false);

                var wordSkippedPacket = XPacketConverter.Serialize(XPacketType.WordSkipped, new XPacketWordSkipped
        {
            WordId = lobby.CurrentWordId.Value,
            Word = lobby.CurrentWord!,
            PenaltyPoints = penalty
        });
        _sessionManager.BroadcastToLobby(lobby.Id, wordSkippedPacket);

        if (penalty > 0)
        {
            BroadcastScoreUpdate(lobby, currentTeam.Id, -penalty, 1);
        }

                if (!lobby.IsLastWordPhase)
        {
            await SendNextWordAsync(lobby);
        }
    }

                private void StartRoundTimer(Lobby lobby)
    {
        StopRoundTimer(lobby.Id);

        var timer = new Timer(1000);
        timer.Elapsed += async (s, e) => await OnTimerTickAsync(lobby);
        timer.Start();
        _gameTimers[lobby.Id] = timer;
    }

                private async Task OnTimerTickAsync(Lobby lobby)
    {
        lobby.TimeRemaining--;

                var timerPacket = XPacketConverter.Serialize(XPacketType.TimerUpdate, new XPacketTimerUpdate
        {
            SecondsRemaining = lobby.TimeRemaining,
            IsLastWordPhase = lobby.IsLastWordPhase
        });
        _sessionManager.BroadcastToLobby(lobby.Id, timerPacket);

        if (lobby.TimeRemaining <= 0)
        {
            if (!lobby.IsLastWordPhase && lobby.Settings.LastWordTimeSeconds > 0)
            {
                                await EnterLastWordPhaseAsync(lobby);
            }
            else
            {
                                await EndRoundAsync(lobby);
            }
        }
    }

                private async Task EnterLastWordPhaseAsync(Lobby lobby)
    {
        lobby.IsLastWordPhase = true;
        lobby.TimeRemaining = lobby.Settings.LastWordTimeSeconds;
        lobby.State = GameState.LastWord;

                var lastWordPacket = XPacketConverter.Serialize(XPacketType.LastWordPhase, new XPacketLastWordPhase
        {
            TimeSeconds = lobby.Settings.LastWordTimeSeconds,
            CurrentWord = lobby.CurrentWord!
        });

                var explainerSession = _sessionManager.GetSessionByUserId(lobby.CurrentExplainer!.Id);
        explainerSession?.Send(lastWordPacket);

                var otherPacket = XPacketConverter.Serialize(XPacketType.LastWordPhase, new XPacketLastWordPhase
        {
            TimeSeconds = lobby.Settings.LastWordTimeSeconds,
            CurrentWord = ""         });

        foreach (var session in _sessionManager.GetSessionsInLobby(lobby.Id))
        {
            if (session.UserId != lobby.CurrentExplainer!.Id)
            {
                session.Send(otherPacket);
            }
        }

        Log.Information("Last word phase started in lobby {LobbyId}", lobby.Id);
    }

                public async Task EndRoundAsync(Lobby lobby)
    {
        StopRoundTimer(lobby.Id);
        lobby.State = GameState.RoundEnd;

        var currentTeam = lobby.CurrentTeam!;
        var pointsThisRound = lobby.GuessedWordsThisRound.Count;

                var roundEndedPacket = XPacketConverter.Serialize(XPacketType.RoundEnded, new XPacketRoundEnded
        {
            RoundNumber = lobby.CurrentRound,
            TeamId = currentTeam.Id,
            PointsEarned = pointsThisRound,
            WordsGuessed = lobby.GuessedWordsThisRound.Count,
            WordsSkipped = 0,             GuessedWordsJson = JsonSerializer.Serialize(lobby.GuessedWordsThisRound)
        });
        _sessionManager.BroadcastToLobby(lobby.Id, roundEndedPacket);

        Log.Information("Round {Round} ended in lobby {LobbyId}, team {TeamId} scored {Points}",
            lobby.CurrentRound, lobby.Id, currentTeam.Id, pointsThisRound);

                if (lobby.CurrentRound >= lobby.Settings.TotalRounds)
        {
                        var winningTeam = lobby.Teams.OrderByDescending(t => t.Score).First();
            await EndGameAsync(lobby, winningTeam.Id);
            return;
        }

                await Task.Delay(3000);
        
        lobby.NextTeam();
        await StartRoundAsync(lobby);
    }

                public async Task EndGameAsync(Lobby lobby, int winningTeamId)
    {
        StopRoundTimer(lobby.Id);
        lobby.State = GameState.Finished;

        var gameDuration = (int)(DateTime.UtcNow - _gameStartTimes[lobby.Id]).TotalSeconds;

                var scores = lobby.Teams.ToDictionary(t => t.Id, t => t.Score);

                var gameEndedPacket = XPacketConverter.Serialize(XPacketType.GameEnded, new XPacketGameEnded
        {
            WinningTeamId = winningTeamId,
            FinalScoresJson = JsonSerializer.Serialize(scores),
            GameStatsJson = JsonSerializer.Serialize(new
            {
                Duration = gameDuration,
                TotalRounds = lobby.CurrentRound,
                TotalWordsUsed = lobby.UsedWordIds.Count
            })
        });
        _sessionManager.BroadcastToLobby(lobby.Id, gameEndedPacket);

                var winningTeam = lobby.Teams.FirstOrDefault(t => t.Id == winningTeamId);
        foreach (var player in lobby.Players)
        {
            var playerTeam = lobby.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == player.Id));
            if (playerTeam != null)
            {
                var isWinner = playerTeam.Id == winningTeamId;
                await _gameHistoryService.RecordGameAsync(
                    player.Id,
                    playerTeam.Name,
                    playerTeam.Score,
                    isWinner,
                    0,                     0,                     gameDuration
                );

                await _userService.UpdateStatsAsync(player.Id, isWinner, playerTeam.Score);
            }
        }

                lobby.CurrentRound = 0;
        lobby.CurrentTeamIndex = 0;
        lobby.UsedWordIds.Clear();
        foreach (var team in lobby.Teams)
        {
            team.Score = 0;
            team.CurrentExplainerIndex = 0;
        }

        _gameStartTimes.Remove(lobby.Id);

        Log.Information("Game ended in lobby {LobbyId}, winner: Team {TeamId}", lobby.Id, winningTeamId);
    }

                public void ChangeScore(Lobby lobby, int teamId, int change)
    {
        var team = lobby.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team == null) return;

        team.Score = Math.Max(0, team.Score + change);

        BroadcastScoreUpdate(lobby, teamId, change, 2);

        Log.Information("Score manually changed in lobby {LobbyId}: Team {TeamId} {Change:+#;-#;0}",
            lobby.Id, teamId, change);
    }

                private void BroadcastScoreUpdate(Lobby lobby, int teamId, int change, byte reason)
    {
        var scores = lobby.Teams.ToDictionary(t => t.Id, t => t.Score);

        var scorePacket = XPacketConverter.Serialize(XPacketType.ScoreUpdate, new XPacketScoreUpdate
        {
            ScoresJson = JsonSerializer.Serialize(scores),
            ChangedTeamId = teamId,
            ChangeAmount = change,
            ChangeReason = reason
        });
        _sessionManager.BroadcastToLobby(lobby.Id, scorePacket);
    }

                private void StopRoundTimer(int lobbyId)
    {
        if (_gameTimers.TryGetValue(lobbyId, out var timer))
        {
            timer.Stop();
            timer.Dispose();
            _gameTimers.Remove(lobbyId);
        }
    }
}
