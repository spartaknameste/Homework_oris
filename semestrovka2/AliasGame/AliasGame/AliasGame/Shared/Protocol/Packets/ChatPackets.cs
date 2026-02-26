namespace AliasGame.Shared.Protocol.Packets;

public class XPacketChatMessage
{
    [XField(0)]
    public string Message = string.Empty;

    [XField(1)]
    public byte ChatType;     
    [XField(2)]
    public int TargetId; }

public class XPacketChatBroadcast
{
    [XField(0)]
    public int SenderId;

    [XField(1)]
    public string SenderName = string.Empty;

    [XField(2)]
    public string Message = string.Empty;

    [XField(3)]
    public byte ChatType; 
    [XField(4)]
    public long Timestamp;
}
