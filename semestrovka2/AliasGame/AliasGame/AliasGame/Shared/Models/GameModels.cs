using System.Text.Json.Serialization;

namespace AliasGame.Shared.Models;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int TeamId { get; set; } = -1;     public bool IsHost { get; set; }
    public bool IsConnected { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public int Score { get; set; } = 0; }

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = new();
    public int Score { get; set; } = 0;
    public int CurrentExplainerIndex { get; set; } = 0;
    
    [JsonIgnore]
    public Player? CurrentExplainer => Players.Count > CurrentExplainerIndex 
        ? Players[CurrentExplainerIndex] 
        : null;

    public void NextExplainer()
    {
        if (Players.Count > 0)
        {
            CurrentExplainerIndex = (CurrentExplainerIndex + 1) % Players.Count;
        }
    }
}

public class GameSettings
{
    public int RoundTimeSeconds { get; set; } = 60;
    public int TotalRounds { get; set; } = 10;
    public int ScoreToWin { get; set; } = 50;
    public int LastWordTimeSeconds { get; set; } = 10;     public bool AllowManualScoreChange { get; set; } = true;
    public bool AllowHostPassTurn { get; set; } = true;
    public int CategoryId { get; set; } = 0;     public int SkipPenalty { get; set; } = 0;
}

public class Lobby
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;     public int MaxPlayers { get; set; } = 20;
    public int HostId { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<Team> Teams { get; set; } = new();
    public GameSettings Settings { get; set; } = new();
    public GameState State { get; set; } = GameState.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CurrentRound { get; set; } = 0;
    public int CurrentTeamIndex { get; set; } = 0;
    public int TimeRemaining { get; set; } = 0;
    public bool IsLastWordPhase { get; set; } = false;
    public string? CurrentWord { get; set; }
    public int? CurrentWordId { get; set; }
    public List<string> GuessedWordsThisRound { get; set; } = new();
    public List<int> UsedWordIds { get; set; } = new();

    [JsonIgnore]
    public bool HasPassword => !string.IsNullOrEmpty(Password);

    [JsonIgnore]
    public int PlayerCount => Players.Count;

    [JsonIgnore]
    public Team? CurrentTeam => Teams.Count > CurrentTeamIndex 
        ? Teams[CurrentTeamIndex] 
        : null;

    [JsonIgnore]
    public Player? CurrentExplainer => CurrentTeam?.CurrentExplainer;

    public Player? GetPlayer(int playerId) => Players.FirstOrDefault(p => p.Id == playerId);

    public Team? GetTeam(int teamId) => Teams.FirstOrDefault(t => t.Id == teamId);

    public void NextTeam()
    {
        if (Teams.Count > 0)
        {
            CurrentTeamIndex = (CurrentTeamIndex + 1) % Teams.Count;
            CurrentTeam?.NextExplainer();
        }
    }

    public void StartNewRound()
    {
        CurrentRound++;
        TimeRemaining = Settings.RoundTimeSeconds;
        IsLastWordPhase = false;
        GuessedWordsThisRound.Clear();
        CurrentWord = null;
        CurrentWordId = null;
    }
}

public class LobbySummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public bool HasPassword { get; set; }
    public GameState State { get; set; }
    public string HostName { get; set; } = string.Empty;
}

public enum GameState
{
    Waiting = 0,          Starting = 1,         Playing = 2,          RoundEnd = 3,         LastWord = 4,         Finished = 5      }

public class RoundResult
{
    public int RoundNumber { get; set; }
    public int TeamId { get; set; }
    public int ExplainerId { get; set; }
    public int PointsEarned { get; set; }
    public int WordsGuessed { get; set; }
    public int WordsSkipped { get; set; }
    public List<string> GuessedWords { get; set; } = new();
}

public class GameResult
{
    public int WinningTeamId { get; set; }
    public Dictionary<int, int> TeamScores { get; set; } = new();
    public List<RoundResult> Rounds { get; set; } = new();
    public int TotalDurationSeconds { get; set; }
    public DateTime EndTime { get; set; }
}
