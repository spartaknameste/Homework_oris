namespace AliasGame.Shared.Protocol.Packets;

public class XPacketStartGame
{
    [XField(0)]
    public int LobbyId;
}

public class XPacketGameStarted
{
    [XField(0)]
    public string TeamsJson = string.Empty; 
    [XField(1)]
    public int TotalRounds;

    [XField(2)]
    public int FirstTeamId;

    [XField(3)]
    public int FirstExplainerId;
}

public class XPacketRoundStarted
{
    [XField(0)]
    public int RoundNumber;

    [XField(1)]
    public int ExplainingTeamId;

    [XField(2)]
    public int ExplainerId;

    [XField(3)]
    public string ExplainerName = string.Empty;

    [XField(4)]
    public int TimeRemainingSeconds;
}

public class XPacketRoundEnded
{
    [XField(0)]
    public int RoundNumber;

    [XField(1)]
    public int TeamId;

    [XField(2)]
    public int PointsEarned;

    [XField(3)]
    public int WordsGuessed;

    [XField(4)]
    public int WordsSkipped;

    [XField(5)]
    public string GuessedWordsJson = string.Empty; }

public class XPacketGameEnded
{
    [XField(0)]
    public int WinningTeamId;

    [XField(1)]
    public string FinalScoresJson = string.Empty; 
    [XField(2)]
    public string GameStatsJson = string.Empty; }

public class XPacketGameCountdown
{
    [XField(0)]
    public int SecondsRemaining;

    [XField(1)]
    public string Message = string.Empty;
}
