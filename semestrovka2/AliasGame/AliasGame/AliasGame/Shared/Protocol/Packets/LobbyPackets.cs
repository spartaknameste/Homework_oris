namespace AliasGame.Shared.Protocol.Packets;

public class XPacketCreateLobby
{
    [XField(0)]
    public string LobbyName = string.Empty;

    [XField(1)]
    public int MaxPlayers;

    [XField(2)]
    public string Password = string.Empty; }

public class XPacketCreateLobbyResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public int LobbyId;

    [XField(2)]
    public string Message = string.Empty;
}

public class XPacketJoinLobby
{
    [XField(0)]
    public int LobbyId;

    [XField(1)]
    public string Password = string.Empty;
}

public class XPacketJoinLobbyResponse
{
    [XField(0)]
    public bool Success;

    [XField(1)]
    public string Message = string.Empty;

    [XField(2)]
    public string LobbyDataJson = string.Empty; }

public class XPacketLeaveLobby
{
    [XField(0)]
    public int LobbyId;
}

public class XPacketLobbyList
{
    [XField(0)]
    public int PageNumber;

    [XField(1)]
    public int PageSize;
}

public class XPacketLobbyListResponse
{
    [XField(0)]
    public string LobbiesJson = string.Empty; 
    [XField(1)]
    public int TotalLobbies;
}

public class XPacketLobbyUpdate
{
    [XField(0)]
    public string LobbyDataJson = string.Empty; 
    [XField(1)]
    public byte UpdateType; }

public class XPacketKickPlayer
{
    [XField(0)]
    public int PlayerId;

    [XField(1)]
    public string Reason = string.Empty;
}
