namespace AliasGame.Shared.Protocol;

public static class XPacketTypeManager
{
    private static readonly Dictionary<XPacketType, (byte Type, byte Subtype)> TypeDictionary = new();
    private static bool _initialized = false;

                static XPacketTypeManager()
    {
        Initialize();
    }

    private static void Initialize()
    {
        if (_initialized) return;

                RegisterType(XPacketType.Handshake, 1, 0);
        RegisterType(XPacketType.Login, 1, 1);
        RegisterType(XPacketType.LoginResponse, 1, 2);
        RegisterType(XPacketType.Register, 1, 3);
        RegisterType(XPacketType.RegisterResponse, 1, 4);
        RegisterType(XPacketType.Disconnect, 1, 5);
        RegisterType(XPacketType.Ping, 1, 6);
        RegisterType(XPacketType.Pong, 1, 7);

                RegisterType(XPacketType.CreateLobby, 2, 0);
        RegisterType(XPacketType.CreateLobbyResponse, 2, 1);
        RegisterType(XPacketType.JoinLobby, 2, 2);
        RegisterType(XPacketType.JoinLobbyResponse, 2, 3);
        RegisterType(XPacketType.LeaveLobby, 2, 4);
        RegisterType(XPacketType.LobbyList, 2, 5);
        RegisterType(XPacketType.LobbyListResponse, 2, 6);
        RegisterType(XPacketType.LobbyUpdate, 2, 7);
        RegisterType(XPacketType.KickPlayer, 2, 8);

                RegisterType(XPacketType.JoinTeam, 3, 0);
        RegisterType(XPacketType.LeaveTeam, 3, 1);
        RegisterType(XPacketType.TeamUpdate, 3, 2);
        RegisterType(XPacketType.CreateTeam, 3, 3);

                RegisterType(XPacketType.ChatMessage, 4, 0);
        RegisterType(XPacketType.ChatBroadcast, 4, 1);

                RegisterType(XPacketType.UpdateSettings, 5, 0);
        RegisterType(XPacketType.SettingsChanged, 5, 1);
        RegisterType(XPacketType.GetSettings, 5, 2);
        RegisterType(XPacketType.SettingsResponse, 5, 3);

                RegisterType(XPacketType.StartGame, 6, 0);
        RegisterType(XPacketType.GameStarted, 6, 1);
        RegisterType(XPacketType.StartRound, 6, 2);
        RegisterType(XPacketType.RoundStarted, 6, 3);
        RegisterType(XPacketType.EndRound, 6, 4);
        RegisterType(XPacketType.RoundEnded, 6, 5);
        RegisterType(XPacketType.EndGame, 6, 6);
        RegisterType(XPacketType.GameEnded, 6, 7);
        RegisterType(XPacketType.GameCountdown, 6, 8);

                RegisterType(XPacketType.NextWord, 7, 0);
        RegisterType(XPacketType.WordUpdate, 7, 1);
        RegisterType(XPacketType.WordGuessed, 7, 2);
        RegisterType(XPacketType.WordSkipped, 7, 3);
        RegisterType(XPacketType.ScoreUpdate, 7, 4);
        RegisterType(XPacketType.TimerUpdate, 7, 5);
        RegisterType(XPacketType.LastWordPhase, 7, 6);
        RegisterType(XPacketType.FinishRound, 7, 7);

                RegisterType(XPacketType.TurnStart, 8, 0);
        RegisterType(XPacketType.TurnEnd, 8, 1);
        RegisterType(XPacketType.PassTurn, 8, 2);

                RegisterType(XPacketType.AdminLogin, 9, 0);
        RegisterType(XPacketType.AdminLoginResponse, 9, 1);
        RegisterType(XPacketType.GetWords, 9, 2);
        RegisterType(XPacketType.WordsResponse, 9, 3);
        RegisterType(XPacketType.AddWord, 9, 4);
        RegisterType(XPacketType.AddWordResponse, 9, 5);
        RegisterType(XPacketType.DeleteWord, 9, 6);
        RegisterType(XPacketType.UpdateWord, 9, 7);
        RegisterType(XPacketType.GetCategories, 9, 8);
        RegisterType(XPacketType.CategoriesResponse, 9, 9);
        RegisterType(XPacketType.AddCategory, 9, 10);
        RegisterType(XPacketType.DeleteCategory, 9, 11);
        RegisterType(XPacketType.GetUsers, 9, 12);
        RegisterType(XPacketType.UsersResponse, 9, 13);
        RegisterType(XPacketType.BanUser, 9, 14);
        RegisterType(XPacketType.UnbanUser, 9, 15);
        RegisterType(XPacketType.ManualScoreChange, 9, 16);

                RegisterType(XPacketType.Error, 10, 0);
        RegisterType(XPacketType.ServerMessage, 10, 1);

        _initialized = true;
    }

                public static void RegisterType(XPacketType type, byte btype, byte bsubtype)
    {
        if (TypeDictionary.ContainsKey(type))
            throw new Exception($"Packet type {type:G} is already registered.");

        TypeDictionary.Add(type, (btype, bsubtype));
    }

                public static (byte Type, byte Subtype) GetType(XPacketType type)
    {
        if (!TypeDictionary.ContainsKey(type))
            throw new Exception($"Packet type {type:G} is not registered.");

        return TypeDictionary[type];
    }

                public static XPacketType GetTypeFromPacket(XPacket packet)
    {
        var type = packet.PacketType;
        var subtype = packet.PacketSubtype;

        foreach (var tuple in TypeDictionary)
        {
            var value = tuple.Value;
            if (value.Type == type && value.Subtype == subtype)
            {
                return tuple.Key;
            }
        }

        return XPacketType.Unknown;
    }
}
