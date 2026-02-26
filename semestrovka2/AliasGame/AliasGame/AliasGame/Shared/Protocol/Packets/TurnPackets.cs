namespace AliasGame.Shared.Protocol.Packets;

public class XPacketTurnStart
{
    [XField(0)]
    public int TeamId;

    [XField(1)]
    public int ExplainerId;

    [XField(2)]
    public string ExplainerName = string.Empty;

    [XField(3)]
    public int RoundNumber;
}

public class XPacketTurnEnd
{
    [XField(0)]
    public int TeamId;

    [XField(1)]
    public int PointsThisTurn;

    [XField(2)]
    public int NextTeamId;

    [XField(3)]
    public int NextExplainerId;
}

public class XPacketPassTurn
{
    [XField(0)]
    public int Reason; }
