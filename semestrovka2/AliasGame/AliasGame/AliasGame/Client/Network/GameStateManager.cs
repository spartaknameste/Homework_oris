using System.Text.Json;
using AliasGame.Shared.Models;
using AliasGame.Shared.Protocol;
using AliasGame.Shared.Protocol.Packets;

namespace AliasGame.Client.Network;

public class GameStateManager
{
    private readonly GameClient _client;

        public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public bool IsAdmin { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public Lobby? CurrentLobby { get; private set; }
    public bool IsHost => CurrentLobby?.HostId == UserId;
    public int? CurrentExplainerId { get; private set; }
    public bool IsExplainer => CurrentExplainerId.HasValue && CurrentExplainerId == UserId;

    public string? CurrentWord { get; private set; }
    public int TimeRemaining { get; private set; }
    public bool IsLastWordPhase { get; private set; }

        public event Action<bool, string>? LoginResult;
    public event Action<bool, string>? RegisterResult;
    public event Action? LobbyUpdated;
    public event Action<List<LobbySummary>>? LobbyListReceived;
    public event Action<bool, string>? JoinLobbyResult;
    public event Action<bool, string>? CreateLobbyResult;
    public event Action<string, string>? ChatMessageReceived;
    public event Action<int>? GameCountdown;
    public event Action? GameStarted;
    public event Action<int, int, string>? RoundStarted;
    public event Action<string>? WordReceived;
    public event Action<string, int>? WordGuessed;
    public event Action<string, int>? WordSkipped;
    public event Action<int, bool>? TimerUpdated;
    public event Action? LastWordPhaseStarted;
    public event Action<int, int>? RoundEnded;
    public event Action<int, Dictionary<int, int>>? GameEnded;
    public event Action<string>? ErrorReceived;
    public event Action<string>? ServerMessage;

    public GameStateManager(GameClient client)
    {
        _client = client;
        _client.PacketReceived += HandlePacket;
    }

    #region Send Methods

    public void PerformHandshake()
    {
        var random = new Random();
        _client.Send(XPacketType.Handshake, new XPacketHandshake
        {
            MagicHandshakeNumber = random.Next(),
            ProtocolVersion = 1
        });
    }

    public void Login(string username, string password)
    {
        _client.Send(XPacketType.Login, new XPacketLogin
        {
            Username = username,
            PasswordHash = password         });
    }

    public void Register(string username, string password, string email)
    {
        _client.Send(XPacketType.Register, new XPacketRegister
        {
            Username = username,
            PasswordHash = password,
            Email = email
        });
    }

    public void RequestLobbyList()
    {
        _client.Send(XPacketType.LobbyList, new XPacketLobbyList { PageNumber = 1, PageSize = 50 });
    }

    public void CreateLobby(string name, int maxPlayers, string password = "")
    {
        _client.Send(XPacketType.CreateLobby, new XPacketCreateLobby
        {
            LobbyName = name,
            MaxPlayers = maxPlayers,
            Password = password
        });
    }

    public void JoinLobby(int lobbyId, string password = "")
    {
        _client.Send(XPacketType.JoinLobby, new XPacketJoinLobby
        {
            LobbyId = lobbyId,
            Password = password
        });
    }

    public void LeaveLobby()
    {
        _client.Send(XPacketType.LeaveLobby, new XPacketLeaveLobby
        {
            LobbyId = CurrentLobby?.Id ?? 0
        });
        CurrentLobby = null;
    }

    public void JoinTeam(int teamId)
    {
        _client.Send(XPacketType.JoinTeam, new XPacketJoinTeam { TeamId = teamId });
    }

    public void SendChatMessage(string message)
    {
        _client.Send(XPacketType.ChatMessage, new XPacketChatMessage
        {
            Message = message,
            ChatType = 0         });
    }

    public void UpdateSettings(GameSettings settings)
    {
        _client.Send(XPacketType.UpdateSettings, new XPacketUpdateSettings
        {
            RoundTimeSeconds = settings.RoundTimeSeconds,
            TotalRounds = settings.TotalRounds,
            ScoreToWin = settings.ScoreToWin,
            LastWordTimeSeconds = settings.LastWordTimeSeconds,
            AllowManualScoreChange = settings.AllowManualScoreChange,
            AllowHostPassTurn = settings.AllowHostPassTurn,
            CategoryId = settings.CategoryId,
            SkipPenalty = settings.SkipPenalty
        });
    }

    public void StartGame()
    {
        _client.Send(XPacketType.StartGame, new XPacketStartGame
        {
            LobbyId = CurrentLobby?.Id ?? 0
        });
    }

    public void NextWord(bool wasGuessed)
    {
        _client.Send(XPacketType.NextWord, new XPacketNextWord { WasGuessed = wasGuessed });
    }

    public void FinishRound(bool lastWordGuessed)
    {
        _client.Send(XPacketType.FinishRound, new XPacketFinishRound { LastWordGuessed = lastWordGuessed });
    }

    public void ChangeScore(int teamId, int change)
    {
        _client.Send(XPacketType.ManualScoreChange, new XPacketManualScoreChange
        {
            TeamId = teamId,
            PointChange = change,
            Reason = "Ручное изменение хостом"
        });
    }

    #endregion

    #region Packet Handling

    private void HandlePacket(XPacket packet)
    {
        var type = XPacketTypeManager.GetTypeFromPacket(packet);

        switch (type)
        {
            case XPacketType.LoginResponse:
                HandleLoginResponse(packet);
                break;
            case XPacketType.RegisterResponse:
                HandleRegisterResponse(packet);
                break;
            case XPacketType.LobbyListResponse:
                HandleLobbyListResponse(packet);
                break;
            case XPacketType.CreateLobbyResponse:
                HandleCreateLobbyResponse(packet);
                break;
            case XPacketType.JoinLobbyResponse:
                HandleJoinLobbyResponse(packet);
                break;
            case XPacketType.LobbyUpdate:
                HandleLobbyUpdate(packet);
                break;
            case XPacketType.ChatBroadcast:
                HandleChatBroadcast(packet);
                break;
            case XPacketType.GameCountdown:
                HandleGameCountdown(packet);
                break;
            case XPacketType.GameStarted:
                HandleGameStarted(packet);
                break;
            case XPacketType.RoundStarted:
                HandleRoundStarted(packet);
                break;
            case XPacketType.WordUpdate:
                HandleWordUpdate(packet);
                break;
            case XPacketType.WordGuessed:
                HandleWordGuessed(packet);
                break;
            case XPacketType.WordSkipped:
                HandleWordSkipped(packet);
                break;
            case XPacketType.TimerUpdate:
                HandleTimerUpdate(packet);
                break;
            case XPacketType.LastWordPhase:
                HandleLastWordPhase(packet);
                break;
            case XPacketType.RoundEnded:
                HandleRoundEnded(packet);
                break;
            case XPacketType.GameEnded:
                HandleGameEnded(packet);
                break;
            case XPacketType.ScoreUpdate:
                HandleScoreUpdate(packet);
                break;
            case XPacketType.Error:
                HandleError(packet);
                break;
            case XPacketType.ServerMessage:
                HandleServerMessage(packet);
                break;
        }
    }

    private void HandleLoginResponse(XPacket packet)
    {
        var response = XPacketConverter.Deserialize<XPacketLoginResponse>(packet);
        if (response.Success)
        {
            UserId = response.UserId;
            IsAdmin = response.IsAdmin;
        }
        LoginResult?.Invoke(response.Success, response.Message);
    }

    private void HandleRegisterResponse(XPacket packet)
    {
        var response = XPacketConverter.Deserialize<XPacketRegisterResponse>(packet);
        RegisterResult?.Invoke(response.Success, response.Message);
    }

    private void HandleLobbyListResponse(XPacket packet)
    {
        var response = XPacketConverter.Deserialize<XPacketLobbyListResponse>(packet);
        var lobbies = JsonSerializer.Deserialize<List<LobbySummary>>(response.LobbiesJson) ?? new();
        LobbyListReceived?.Invoke(lobbies);
    }

    private void HandleCreateLobbyResponse(XPacket packet)
    {
        var response = XPacketConverter.Deserialize<XPacketCreateLobbyResponse>(packet);
        CreateLobbyResult?.Invoke(response.Success, response.Message);
    }

    private void HandleJoinLobbyResponse(XPacket packet)
    {
        var response = XPacketConverter.Deserialize<XPacketJoinLobbyResponse>(packet);
        if (response.Success && !string.IsNullOrEmpty(response.LobbyDataJson))
        {
            CurrentLobby = JsonSerializer.Deserialize<Lobby>(response.LobbyDataJson);
        }
        JoinLobbyResult?.Invoke(response.Success, response.Message);
    }

    private void HandleLobbyUpdate(XPacket packet)
    {
        var update = XPacketConverter.Deserialize<XPacketLobbyUpdate>(packet);
        CurrentLobby = JsonSerializer.Deserialize<Lobby>(update.LobbyDataJson);
        LobbyUpdated?.Invoke();
    }

    private void HandleChatBroadcast(XPacket packet)
    {
        var chat = XPacketConverter.Deserialize<XPacketChatBroadcast>(packet);
        ChatMessageReceived?.Invoke(chat.SenderName, chat.Message);
    }

    private void HandleGameCountdown(XPacket packet)
    {
        var countdown = XPacketConverter.Deserialize<XPacketGameCountdown>(packet);
        GameCountdown?.Invoke(countdown.SecondsRemaining);
    }

    private void HandleGameStarted(XPacket packet)
    {
        var started = XPacketConverter.Deserialize<XPacketGameStarted>(packet);
        if (CurrentLobby != null)
        {
            CurrentLobby.State = GameState.Playing;
            CurrentLobby.Teams = JsonSerializer.Deserialize<List<Team>>(started.TeamsJson) ?? new();
        }
        GameStarted?.Invoke();
    }

    private void HandleRoundStarted(XPacket packet)
    {
        var round = XPacketConverter.Deserialize<XPacketRoundStarted>(packet);
        if (CurrentLobby != null)
        {
            CurrentLobby.CurrentRound = round.RoundNumber;
            CurrentLobby.TimeRemaining = round.TimeRemainingSeconds;
        }
        TimeRemaining = round.TimeRemainingSeconds;
        IsLastWordPhase = false;
        CurrentWord = null;
        CurrentExplainerId = round.ExplainerId;
        RoundStarted?.Invoke(round.RoundNumber, round.ExplainerId, round.ExplainerName);
    }

    private void HandleWordUpdate(XPacket packet)
    {
        var word = XPacketConverter.Deserialize<XPacketWordUpdate>(packet);
        CurrentWord = word.Word;
        WordReceived?.Invoke(word.Word);
    }

    private void HandleWordGuessed(XPacket packet)
    {
        var guessed = XPacketConverter.Deserialize<XPacketWordGuessed>(packet);
        WordGuessed?.Invoke(guessed.Word, guessed.PointsAwarded);
    }

    private void HandleWordSkipped(XPacket packet)
    {
        var skipped = XPacketConverter.Deserialize<XPacketWordSkipped>(packet);
        WordSkipped?.Invoke(skipped.Word, skipped.PenaltyPoints);
    }

    private void HandleTimerUpdate(XPacket packet)
    {
        var timer = XPacketConverter.Deserialize<XPacketTimerUpdate>(packet);
        TimeRemaining = timer.SecondsRemaining;
        IsLastWordPhase = timer.IsLastWordPhase;
        TimerUpdated?.Invoke(timer.SecondsRemaining, timer.IsLastWordPhase);
    }

    private void HandleLastWordPhase(XPacket packet)
    {
        var lastWord = XPacketConverter.Deserialize<XPacketLastWordPhase>(packet);
        IsLastWordPhase = true;
        if (!string.IsNullOrEmpty(lastWord.CurrentWord))
        {
            CurrentWord = lastWord.CurrentWord;
        }
        LastWordPhaseStarted?.Invoke();
    }

    private void HandleRoundEnded(XPacket packet)
    {
        var ended = XPacketConverter.Deserialize<XPacketRoundEnded>(packet);
        CurrentWord = null;
        CurrentExplainerId = null;
        RoundEnded?.Invoke(ended.RoundNumber, ended.PointsEarned);
    }

    private void HandleGameEnded(XPacket packet)
    {
        var ended = XPacketConverter.Deserialize<XPacketGameEnded>(packet);
        var scores = JsonSerializer.Deserialize<Dictionary<int, int>>(ended.FinalScoresJson) ?? new();
        if (CurrentLobby != null)
        {
            CurrentLobby.State = GameState.Finished;
        }
        CurrentExplainerId = null;
        GameEnded?.Invoke(ended.WinningTeamId, scores);
    }

    private void HandleScoreUpdate(XPacket packet)
    {
        var score = XPacketConverter.Deserialize<XPacketScoreUpdate>(packet);
        if (CurrentLobby != null)
        {
            var scores = JsonSerializer.Deserialize<Dictionary<int, int>>(score.ScoresJson);
            if (scores != null)
            {
                foreach (var team in CurrentLobby.Teams)
                {
                    if (scores.TryGetValue(team.Id, out var teamScore))
                    {
                        team.Score = teamScore;
                    }
                }
            }
        }
        LobbyUpdated?.Invoke();
    }

    private void HandleError(XPacket packet)
    {
        var error = XPacketConverter.Deserialize<XPacketError>(packet);
        ErrorReceived?.Invoke(error.Message);
    }

    private void HandleServerMessage(XPacket packet)
    {
        var msg = XPacketConverter.Deserialize<XPacketServerMessage>(packet);
        ServerMessage?.Invoke(msg.Message);
    }

    #endregion
}