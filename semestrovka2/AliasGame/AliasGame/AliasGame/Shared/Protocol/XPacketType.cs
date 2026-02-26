namespace AliasGame.Shared.Protocol;

public enum XPacketType
{
    Unknown = 0,

        Handshake = 1,
    Login = 2,
    LoginResponse = 3,
    Register = 4,
    RegisterResponse = 5,
    Disconnect = 6,
    Ping = 7,
    Pong = 8,

        CreateLobby = 10,
    CreateLobbyResponse = 11,
    JoinLobby = 12,
    JoinLobbyResponse = 13,
    LeaveLobby = 14,
    LobbyList = 15,
    LobbyListResponse = 16,
    LobbyUpdate = 17,
    KickPlayer = 18,

        JoinTeam = 20,
    LeaveTeam = 21,
    TeamUpdate = 22,
    CreateTeam = 23,

        ChatMessage = 30,
    ChatBroadcast = 31,

        UpdateSettings = 40,
    SettingsChanged = 41,
    GetSettings = 42,
    SettingsResponse = 43,

        StartGame = 50,
    GameStarted = 51,
    StartRound = 52,
    RoundStarted = 53,
    EndRound = 54,
    RoundEnded = 55,
    EndGame = 56,
    GameEnded = 57,
    GameCountdown = 58,

        NextWord = 60,
    WordUpdate = 61,
    WordGuessed = 62,
    WordSkipped = 63,
    ScoreUpdate = 64,
    TimerUpdate = 65,
    LastWordPhase = 66,
    FinishRound = 67,

        TurnStart = 70,
    TurnEnd = 71,
    PassTurn = 72,

        AdminLogin = 80,
    AdminLoginResponse = 81,
    GetWords = 82,
    WordsResponse = 83,
    AddWord = 84,
    AddWordResponse = 85,
    DeleteWord = 86,
    UpdateWord = 87,
    GetCategories = 88,
    CategoriesResponse = 89,
    AddCategory = 90,
    DeleteCategory = 91,
    GetUsers = 92,
    UsersResponse = 93,
    BanUser = 94,
    UnbanUser = 95,
    ManualScoreChange = 96,

        Error = 100,
    ServerMessage = 101
}
