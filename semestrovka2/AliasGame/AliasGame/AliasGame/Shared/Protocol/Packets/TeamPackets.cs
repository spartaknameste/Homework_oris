namespace AliasGame.Shared.Protocol.Packets;

public class XPacketJoinTeam
{
    [XField(0)]
    public int TeamId;
}

public class XPacketLeaveTeam
{
    [XField(0)]
    public int TeamId;
}

public class XPacketTeamUpdate
{
    [XField(0)]
    public string TeamsJson = string.Empty; }

public class XPacketCreateTeam
{
    [XField(0)]
    public string TeamName = string.Empty;
}
