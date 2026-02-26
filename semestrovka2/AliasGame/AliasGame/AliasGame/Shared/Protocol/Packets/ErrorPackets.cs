namespace AliasGame.Shared.Protocol.Packets;

public class XPacketError
{
    [XField(0)]
    public int ErrorCode;

    [XField(1)]
    public string Message = string.Empty;

    [XField(2)]
    public byte Severity; }

public class XPacketServerMessage
{
    [XField(0)]
    public string Message = string.Empty;

    [XField(1)]
    public byte MessageType; }

public static class ErrorCodes
{
    public const int Success = 0;
    public const int UnknownError = 1;
    public const int InvalidPacket = 2;
    public const int NotAuthenticated = 3;
    public const int InvalidCredentials = 4;
    public const int UsernameTaken = 5;
    public const int EmailTaken = 6;
    public const int InvalidLobby = 7;
    public const int LobbyFull = 8;
    public const int WrongPassword = 9;
    public const int NotInLobby = 10;
    public const int NotHost = 11;
    public const int GameAlreadyStarted = 12;
    public const int GameNotStarted = 13;
    public const int NotYourTurn = 14;
    public const int InvalidTeam = 15;
    public const int NotEnoughTeams = 16;
    public const int NotEnoughPlayers = 17;
    public const int UserBanned = 18;
    public const int NotAdmin = 19;
    public const int InvalidWord = 20;
    public const int InvalidCategory = 21;
    public const int RateLimited = 22;
    public const int ServerFull = 23;
    public const int MaintenanceMode = 24;
    public const int InvalidAction = 25;
}
