namespace AliasGame.Shared.Protocol.Packets;

public class XPacketHandshake
{
    [XField(0)]
    public int MagicHandshakeNumber;

    [XField(1)]
    public int ProtocolVersion;
}

public class XPacketLogin
{
    [XField(0)]
    public string Username = string.Empty;

    [XField(1)]
    public string PasswordHash = string.Empty;
}

public class XPacketLoginResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public int UserId;

    [XField(2)]
    public string Message = string.Empty;

    [XField(3)]
    public bool IsAdmin;
}

public class XPacketRegister
{
    [XField(0)]
    public string Username = string.Empty;

    [XField(1)]
    public string PasswordHash = string.Empty;

    [XField(2)]
    public string Email = string.Empty;
}

public class XPacketRegisterResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public string Message = string.Empty;

    [XField(2)]
    public int UserId;
}

public class XPacketDisconnect
{
    [XField(0)]
    public string Reason = string.Empty;
}

public class XPacketPing
{
    [XField(0)]
    public long Timestamp;
}

public class XPacketPong
{
    [XField(0)]
    public long Timestamp;

    [XField(1)]
    public long ServerTimestamp;
}
