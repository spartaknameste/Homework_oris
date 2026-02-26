using System.Text.Json;
using AliasGame.Shared.Protocol;
using AliasGame.Shared.Protocol.Packets;
using AliasGame.Shared.Models;
using AliasGame.Shared.ORM;
using AliasGame.Server.Network;
using AliasGame.Server.Game;
using Serilog;

namespace AliasGame.Server.Handlers;

public class PacketHandler
{
    private readonly SessionManager _sessionManager;
    private readonly LobbyManager _lobbyManager;
    private readonly GameManager _gameManager;
    private readonly UserService _userService;
    private readonly WordService _wordService;
    private readonly CategoryService _categoryService;

    private const int ProtocolVersion = 1;

    public PacketHandler(
        SessionManager sessionManager,
        LobbyManager lobbyManager,
        GameManager gameManager,
        UserService userService,
        WordService wordService,
        CategoryService categoryService)
    {
        _sessionManager = sessionManager;
        _lobbyManager = lobbyManager;
        _gameManager = gameManager;
        _userService = userService;
        _wordService = wordService;
        _categoryService = categoryService;
    }

                public async Task HandlePacketAsync(ClientSession session, XPacket packet)
    {
        var packetType = XPacketTypeManager.GetTypeFromPacket(packet);

                if (!session.IsAuthenticated)
        {
            switch (packetType)
            {
                case XPacketType.Handshake:
                    HandleHandshake(session, packet);
                    return;
                case XPacketType.Login:
                    await HandleLoginAsync(session, packet);
                    return;
                case XPacketType.Register:
                    await HandleRegisterAsync(session, packet);
                    return;
                case XPacketType.Ping:
                    HandlePing(session, packet);
                    return;
            }

            SendError(session, ErrorCodes.NotAuthenticated, "Требуется авторизация");
            return;
        }

                switch (packetType)
        {
            case XPacketType.Ping:
                HandlePing(session, packet);
                break;
            case XPacketType.Disconnect:
                HandleDisconnect(session);
                break;
            case XPacketType.CreateLobby:
                HandleCreateLobby(session, packet);
                break;
            case XPacketType.JoinLobby:
                HandleJoinLobby(session, packet);
                break;
            case XPacketType.LeaveLobby:
                HandleLeaveLobby(session);
                break;
            case XPacketType.LobbyList:
                HandleLobbyList(session);
                break;
            case XPacketType.KickPlayer:
                HandleKickPlayer(session, packet);
                break;
            case XPacketType.JoinTeam:
                HandleJoinTeam(session, packet);
                break;
            case XPacketType.ChatMessage:
                HandleChatMessage(session, packet);
                break;
            case XPacketType.UpdateSettings:
                HandleUpdateSettings(session, packet);
                break;
            case XPacketType.GetSettings:
                HandleGetSettings(session);
                break;
            case XPacketType.StartGame:
                await HandleStartGameAsync(session);
                break;
            case XPacketType.FinishRound:
                await HandleFinishRoundAsync(session, packet);
                break;
            case XPacketType.NextWord:
                await HandleNextWordAsync(session, packet);
                break;
            case XPacketType.ManualScoreChange:
                HandleManualScoreChange(session, packet);
                break;
            case XPacketType.GetWords:
                await HandleGetWordsAsync(session, packet);
                break;
            case XPacketType.AddWord:
                await HandleAddWordAsync(session, packet);
                break;
            case XPacketType.DeleteWord:
                await HandleDeleteWordAsync(session, packet);
                break;
            case XPacketType.GetCategories:
                await HandleGetCategoriesAsync(session, packet);
                break;
            case XPacketType.AddCategory:
                await HandleAddCategoryAsync(session, packet);
                break;
            case XPacketType.GetUsers:
                await HandleGetUsersAsync(session, packet);
                break;
            case XPacketType.BanUser:
                await HandleBanUserAsync(session, packet);
                break;
            case XPacketType.UnbanUser:
                await HandleUnbanUserAsync(session, packet);
                break;
            default:
                Log.Warning("Unknown packet type {Type} from session {SessionId}", packetType, session.SessionId);
                SendError(session, ErrorCodes.InvalidPacket, "Неизвестный тип пакета");
                break;
        }
    }

    private void HandleHandshake(ClientSession session, XPacket packet)
    {
        var handshake = XPacketConverter.Deserialize<XPacketHandshake>(packet);
        if (handshake.ProtocolVersion != ProtocolVersion)
        {
            SendError(session, ErrorCodes.InvalidPacket, $"Неподдерживаемая версия протокола");
            return;
        }
        var response = XPacketConverter.Serialize(XPacketType.Handshake, new XPacketHandshake
        {
            MagicHandshakeNumber = handshake.MagicHandshakeNumber - 15,
            ProtocolVersion = ProtocolVersion
        });
        session.Send(response);
        session.HandshakeCompleted = true;
    }

    private async Task HandleLoginAsync(ClientSession session, XPacket packet)
    {
        var login = XPacketConverter.Deserialize<XPacketLogin>(packet);
        var user = await _userService.GetByUsernameAsync(login.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(login.PasswordHash, user.PasswordHash))
        {
            session.Send(XPacketConverter.Serialize(XPacketType.LoginResponse, new XPacketLoginResponse
            {
                Success = false,
                Message = "Неверное имя пользователя или пароль"
            }));
            return;
        }

        if (await _userService.IsUserBannedAsync(user.Id))
        {
            session.Send(XPacketConverter.Serialize(XPacketType.LoginResponse, new XPacketLoginResponse
            {
                Success = false,
                Message = $"Аккаунт заблокирован: {user.BanReason}"
            }));
            return;
        }

        var existingSession = _sessionManager.GetSessionByUserId(user.Id);
        if (existingSession != null && existingSession.SessionId != session.SessionId)
        {
            SendError(existingSession, ErrorCodes.InvalidAction, "Выполнен вход с другого устройства");
            _sessionManager.RemoveSession(existingSession.SessionId);
        }

        session.IsAuthenticated = true;
        session.UserId = user.Id;
        session.Username = user.Username;
        session.IsAdmin = user.IsAdmin;
        await _userService.UpdateLastLoginAsync(user.Id);

        session.Send(XPacketConverter.Serialize(XPacketType.LoginResponse, new XPacketLoginResponse
        {
            Success = true,
            UserId = user.Id,
            Message = "Добро пожаловать!",
            IsAdmin = user.IsAdmin
        }));
        Log.Information("User {Username} logged in", user.Username);
    }

    private async Task HandleRegisterAsync(ClientSession session, XPacket packet)
    {
        var register = XPacketConverter.Deserialize<XPacketRegister>(packet);
        if (string.IsNullOrWhiteSpace(register.Username) || register.Username.Length < 3)
        {
            session.Send(XPacketConverter.Serialize(XPacketType.RegisterResponse, new XPacketRegisterResponse
            {
                Success = false,
                Message = "Имя пользователя должно быть не менее 3 символов"
            }));
            return;
        }

        if (await _userService.GetByUsernameAsync(register.Username) != null)
        {
            session.Send(XPacketConverter.Serialize(XPacketType.RegisterResponse, new XPacketRegisterResponse
            {
                Success = false,
                Message = "Имя пользователя уже занято"
            }));
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(register.PasswordHash);

        try
        {
            var user = await _userService.CreateUserAsync(register.Username, passwordHash, register.Email);
            session.Send(XPacketConverter.Serialize(XPacketType.RegisterResponse, new XPacketRegisterResponse
            {
                Success = true,
                Message = "Регистрация успешна!",
                UserId = user.Id
            }));
            Log.Information("User registered: {Username}", register.Username);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Registration failed for {Username}", register.Username);
            session.Send(XPacketConverter.Serialize(XPacketType.RegisterResponse, new XPacketRegisterResponse
            {
                Success = false,
                Message = "Ошибка регистрации. Возможно, email уже используется."
            }));
        }
    }

    private void HandlePing(ClientSession session, XPacket packet)
    {
        var ping = XPacketConverter.Deserialize<XPacketPing>(packet);
        session.Send(XPacketConverter.Serialize(XPacketType.Pong, new XPacketPong
        {
            Timestamp = ping.Timestamp,
            ServerTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }));
    }

    private void HandleDisconnect(ClientSession session)
    {
        if (session.LobbyId.HasValue) _lobbyManager.LeaveLobby(session);
        _sessionManager.RemoveSession(session.SessionId);
    }

    private void HandleCreateLobby(ClientSession session, XPacket packet)
    {
        var create = XPacketConverter.Deserialize<XPacketCreateLobby>(packet);
        if (session.LobbyId.HasValue) { SendError(session, ErrorCodes.InvalidAction, "Вы уже в лобби"); return; }

        var lobby = _lobbyManager.CreateLobby(session, create.LobbyName, create.MaxPlayers, create.Password);
        session.Send(XPacketConverter.Serialize(XPacketType.CreateLobbyResponse, new XPacketCreateLobbyResponse
        {
            Success = true,
            LobbyId = lobby.Id,
            Message = "Лобби создано"
        }));
        session.Send(XPacketConverter.Serialize(XPacketType.LobbyUpdate, new XPacketLobbyUpdate
        {
            LobbyDataJson = JsonSerializer.Serialize(lobby),
            UpdateType = 0
        }));
    }

    private void HandleJoinLobby(ClientSession session, XPacket packet)
    {
        var join = XPacketConverter.Deserialize<XPacketJoinLobby>(packet);
        var (success, message) = _lobbyManager.JoinLobby(session, join.LobbyId, join.Password);
        session.Send(XPacketConverter.Serialize(XPacketType.JoinLobbyResponse, new XPacketJoinLobbyResponse
        {
            Success = success,
            Message = message,
            LobbyDataJson = success ? JsonSerializer.Serialize(_lobbyManager.GetLobby(join.LobbyId)) : ""
        }));
    }

    private void HandleLeaveLobby(ClientSession session) => _lobbyManager.LeaveLobby(session);

    private void HandleLobbyList(ClientSession session)
    {
        var lobbies = _lobbyManager.GetLobbyList().ToList();
        session.Send(XPacketConverter.Serialize(XPacketType.LobbyListResponse, new XPacketLobbyListResponse
        {
            LobbiesJson = JsonSerializer.Serialize(lobbies),
            TotalLobbies = lobbies.Count
        }));
    }

    private void HandleKickPlayer(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) { SendError(session, ErrorCodes.NotInLobby, "Вы не в лобби"); return; }
        var lobby = _lobbyManager.GetLobby(session.LobbyId.Value);
        if (lobby == null || lobby.HostId != session.UserId) { SendError(session, ErrorCodes.NotHost, "Только хост может кикать"); return; }

        var kick = XPacketConverter.Deserialize<XPacketKickPlayer>(packet);
        var targetSession = _sessionManager.GetSessionByUserId(kick.PlayerId);
        if (targetSession != null)
        {
            SendError(targetSession, ErrorCodes.InvalidAction, kick.Reason ?? "Вы были исключены");
            _lobbyManager.LeaveLobby(targetSession);
        }
    }

    private void HandleJoinTeam(ClientSession session, XPacket packet)
    {
        var join = XPacketConverter.Deserialize<XPacketJoinTeam>(packet);
        var (success, message) = _lobbyManager.JoinTeam(session, join.TeamId);
        if (!success) SendError(session, ErrorCodes.InvalidTeam, message);
    }

    private void HandleChatMessage(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) { SendError(session, ErrorCodes.NotInLobby, "Вы не в лобби"); return; }
        var chat = XPacketConverter.Deserialize<XPacketChatMessage>(packet);
        _sessionManager.BroadcastToLobby(session.LobbyId.Value, XPacketConverter.Serialize(XPacketType.ChatBroadcast,
            new XPacketChatBroadcast
            {
                SenderId = session.UserId!.Value,
                SenderName = session.Username!,
                Message = chat.Message,
                ChatType = chat.ChatType,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }));
    }

    private void HandleUpdateSettings(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) { SendError(session, ErrorCodes.NotInLobby, "Вы не в лобби"); return; }
        var settings = XPacketConverter.Deserialize<XPacketUpdateSettings>(packet);
        var gameSettings = new GameSettings
        {
            RoundTimeSeconds = settings.RoundTimeSeconds,
            TotalRounds = settings.TotalRounds,
            ScoreToWin = settings.ScoreToWin,
            LastWordTimeSeconds = settings.LastWordTimeSeconds,
            AllowManualScoreChange = settings.AllowManualScoreChange,
            AllowHostPassTurn = settings.AllowHostPassTurn,
            CategoryId = settings.CategoryId,
            SkipPenalty = settings.SkipPenalty
        };
        var (success, message) = _lobbyManager.UpdateSettings(session, gameSettings);
        if (!success) SendError(session, ErrorCodes.NotHost, message);
    }

    private void HandleGetSettings(ClientSession session)
    {
        if (!session.LobbyId.HasValue) { SendError(session, ErrorCodes.NotInLobby, "Вы не в лобби"); return; }
        var lobby = _lobbyManager.GetLobby(session.LobbyId.Value);
        if (lobby == null) return;
        session.Send(XPacketConverter.Serialize(XPacketType.SettingsResponse, new XPacketSettingsResponse
        {
            SettingsJson = JsonSerializer.Serialize(lobby.Settings)
        }));
    }

    private async Task HandleStartGameAsync(ClientSession session)
    {
        var (success, message) = await _gameManager.StartGameAsync(session);
        if (!success) SendError(session, ErrorCodes.InvalidAction, message);
    }

    private async Task HandleFinishRoundAsync(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) return;
        var lobby = _lobbyManager.GetLobby(session.LobbyId.Value);
        if (lobby == null || lobby.CurrentExplainer?.Id != session.UserId) return;
        var finish = XPacketConverter.Deserialize<XPacketFinishRound>(packet);
        if (finish.LastWordGuessed) await _gameManager.WordGuessedAsync(lobby);
        await _gameManager.EndRoundAsync(lobby);
    }

    private async Task HandleNextWordAsync(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) return;
        var lobby = _lobbyManager.GetLobby(session.LobbyId.Value);
        if (lobby == null || lobby.CurrentExplainer?.Id != session.UserId) return;
        var nextWord = XPacketConverter.Deserialize<XPacketNextWord>(packet);
        if (nextWord.WasGuessed) await _gameManager.WordGuessedAsync(lobby);
        else await _gameManager.WordSkippedAsync(lobby);
        if (!lobby.IsLastWordPhase) await _gameManager.SendNextWordAsync(lobby);
    }

    private void HandleManualScoreChange(ClientSession session, XPacket packet)
    {
        if (!session.LobbyId.HasValue) return;
        var lobby = _lobbyManager.GetLobby(session.LobbyId.Value);
        if (lobby == null || lobby.HostId != session.UserId) { SendError(session, ErrorCodes.NotHost, "Только хост"); return; }
        if (!lobby.Settings.AllowManualScoreChange) { SendError(session, ErrorCodes.InvalidAction, "Отключено"); return; }
        var change = XPacketConverter.Deserialize<XPacketManualScoreChange>(packet);
        _gameManager.ChangeScore(lobby, change.TeamId, change.PointChange);
    }

    private async Task HandleGetWordsAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketGetWords>(packet);
        var words = await _wordService.GetWordsPagedAsync(request.PageNumber, request.PageSize,
            request.CategoryId > 0 ? request.CategoryId : null, request.SearchQuery);
        var total = await _wordService.GetTotalWordsCountAsync(request.CategoryId > 0 ? request.CategoryId : null, request.SearchQuery);
        session.Send(XPacketConverter.Serialize(XPacketType.WordsResponse, new XPacketWordsResponse
        {
            WordsJson = JsonSerializer.Serialize(words),
            TotalCount = total,
            PageNumber = request.PageNumber
        }));
    }

    private async Task HandleAddWordAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketAddWord>(packet);
        if (await _wordService.WordExistsAsync(request.Word, request.CategoryId))
        {
            session.Send(XPacketConverter.Serialize(XPacketType.AddWordResponse, new XPacketAddWordResponse
            { Success = false, Message = "Слово уже существует" }));
            return;
        }
        var word = await _wordService.AddWordAsync(request.Word, request.CategoryId, request.Difficulty);
        session.Send(XPacketConverter.Serialize(XPacketType.AddWordResponse, new XPacketAddWordResponse
        { Success = true, WordId = word.Id, Message = "Слово добавлено" }));
    }

    private async Task HandleDeleteWordAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketDeleteWord>(packet);
        await _wordService.DeleteWordAsync(request.WordId);
    }

    private async Task HandleGetCategoriesAsync(ClientSession session, XPacket packet)
    {
        var request = XPacketConverter.Deserialize<XPacketGetCategories>(packet);
        var categories = request.IncludeWordCount
            ? await _categoryService.GetAllWithWordCountAsync()
            : await _categoryService.GetAllAsync();
        session.Send(XPacketConverter.Serialize(XPacketType.CategoriesResponse, new XPacketCategoriesResponse
        { CategoriesJson = JsonSerializer.Serialize(categories) }));
    }

    private async Task HandleAddCategoryAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketAddCategory>(packet);
        await _categoryService.AddCategoryAsync(request.Name, request.Description);
    }

    private async Task HandleGetUsersAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketGetUsers>(packet);
        var users = await _userService.GetUsersPagedAsync(request.PageNumber, request.PageSize, request.SearchQuery);
        var total = await _userService.GetTotalUsersCountAsync(request.SearchQuery);
        session.Send(XPacketConverter.Serialize(XPacketType.UsersResponse, new XPacketUsersResponse
        {
            UsersJson = JsonSerializer.Serialize(users.Select(u => new { u.Id, u.Username, u.Email, u.IsAdmin, u.IsBanned, u.BanReason, u.CreatedAt, u.LastLogin, u.GamesPlayed, u.GamesWon })),
            TotalCount = total
        }));
    }

    private async Task HandleBanUserAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketBanUser>(packet);
        await _userService.BanUserAsync(request.UserId, request.Reason, request.DurationMinutes > 0 ? request.DurationMinutes : null);
        var bannedSession = _sessionManager.GetSessionByUserId(request.UserId);
        if (bannedSession != null)
        {
            SendError(bannedSession, ErrorCodes.UserBanned, $"Вы заблокированы: {request.Reason}");
            _sessionManager.RemoveSession(bannedSession.SessionId);
        }
    }

    private async Task HandleUnbanUserAsync(ClientSession session, XPacket packet)
    {
        if (!session.IsAdmin) { SendError(session, ErrorCodes.NotAdmin, "Требуются права администратора"); return; }
        var request = XPacketConverter.Deserialize<XPacketUnbanUser>(packet);
        await _userService.UnbanUserAsync(request.UserId);
    }

    private void SendError(ClientSession session, int errorCode, string message)
    {
        session.Send(XPacketConverter.Serialize(XPacketType.Error, new XPacketError
        { ErrorCode = errorCode, Message = message, Severity = errorCode >= 10 ? (byte)2 : (byte)1 }));
    }
}