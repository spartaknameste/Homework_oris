namespace AliasGame.Shared.Protocol.Packets;

public class XPacketNextWord
{
    [XField(0)]
    public bool WasGuessed; }

public class XPacketWordUpdate
{
    [XField(0)]
    public string Word = string.Empty;

    [XField(1)]
    public int WordId;

    [XField(2)]
    public string Category = string.Empty;
}

public class XPacketWordGuessed
{
    [XField(0)]
    public int WordId;

    [XField(1)]
    public string Word = string.Empty;

    [XField(2)]
    public int PointsAwarded;

    [XField(3)]
    public int TotalTeamScore;
}

public class XPacketWordSkipped
{
    [XField(0)]
    public int WordId;

    [XField(1)]
    public string Word = string.Empty;

    [XField(2)]
    public int PenaltyPoints;
}

public class XPacketScoreUpdate
{
    [XField(0)]
    public string ScoresJson = string.Empty; 
    [XField(1)]
    public int ChangedTeamId;

    [XField(2)]
    public int ChangeAmount;

    [XField(3)]
    public byte ChangeReason; }

public class XPacketTimerUpdate
{
    [XField(0)]
    public int SecondsRemaining;

    [XField(1)]
    public bool IsLastWordPhase;
}

public class XPacketLastWordPhase
{
    [XField(0)]
    public int TimeSeconds;

    [XField(1)]
    public string CurrentWord = string.Empty; }

public class XPacketFinishRound
{
    [XField(0)]
    public bool LastWordGuessed;
}
