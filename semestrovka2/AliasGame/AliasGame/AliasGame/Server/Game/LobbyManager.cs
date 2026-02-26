using System.Collections.Concurrent;
using System.Text.Json;
using AliasGame.Shared.Models;
using AliasGame.Shared.Protocol;
using AliasGame.Shared.Protocol.Packets;
using AliasGame.Server.Network;
using Serilog;

namespace AliasGame.Server.Game;

public class LobbyManager
{
    private readonly ConcurrentDictionary<int, Lobby> _lobbies = new();
    private readonly SessionManager _sessionManager;
    private int _nextLobbyId = 1;

    public LobbyManager(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

                public Lobby CreateLobby(ClientSession hostSession, string name, int maxPlayers, string password = "")
    {
        var lobbyId = Interlocked.Increment(ref _nextLobbyId);
        
        var lobby = new Lobby
        {
            Id = lobbyId,
            Name = name,
            MaxPlayers = maxPlayers,
            Password = password,
            HostId = hostSession.UserId!.Value
        };

                lobby.Teams.Add(new Team { Id = 1, Name = "Команда 1" });
        lobby.Teams.Add(new Team { Id = 2, Name = "Команда 2" });
        lobby.Teams.Add(new Team { Id = 3, Name = "Команда 3" }); 
                var hostPlayer = new Player
        {
            Id = hostSession.UserId!.Value,
            Username = hostSession.Username!,
            IsHost = true,
            TeamId = -1         };
        lobby.Players.Add(hostPlayer);

        _lobbies.TryAdd(lobbyId, lobby);
        
        hostSession.LobbyId = lobbyId;
        
        Log.Information("Lobby created: {LobbyId} '{LobbyName}' by {Username}", 
            lobbyId, name, hostSession.Username);

        return lobby;
    }

                public Lobby? GetLobby(int lobbyId)
    {
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

                public IEnumerable<LobbySummary> GetLobbyList()
    {
        return _lobbies.Values
            .Where(l => l.State == GameState.Waiting)
            .Select(l => new LobbySummary
            {
                Id = l.Id,
                Name = l.Name,
                PlayerCount = l.PlayerCount,
                MaxPlayers = l.MaxPlayers,
                HasPassword = l.HasPassword,
                State = l.State,
                HostName = l.Players.FirstOrDefault(p => p.IsHost)?.Username ?? "Unknown"
            });
    }

                public (bool Success, string Message) JoinLobby(ClientSession session, int lobbyId, string password = "")
    {
        var lobby = GetLobby(lobbyId);
        if (lobby == null)
            return (false, "Лобби не найдено");

        if (lobby.State != GameState.Waiting)
            return (false, "Игра уже началась");

        if (lobby.PlayerCount >= lobby.MaxPlayers)
            return (false, "Лобби заполнено");

        if (lobby.HasPassword && lobby.Password != password)
            return (false, "Неверный пароль");

                if (lobby.Players.Any(p => p.Id == session.UserId))
            return (false, "Вы уже в этом лобби");

        var player = new Player
        {
            Id = session.UserId!.Value,
            Username = session.Username!,
            TeamId = -1
        };

        lobby.Players.Add(player);
        session.LobbyId = lobbyId;

                EnsureEmptyTeam(lobby);

        Log.Information("Player {Username} joined lobby {LobbyId}", session.Username, lobbyId);
        
                BroadcastLobbyUpdate(lobby, 0); 
        return (true, "Успешно");
    }

                public void LeaveLobby(ClientSession session)
    {
        if (!session.LobbyId.HasValue) return;

        var lobby = GetLobby(session.LobbyId.Value);
        if (lobby == null) return;

        var player = lobby.Players.FirstOrDefault(p => p.Id == session.UserId);
        if (player == null) return;

                if (player.TeamId > 0)
        {
            var team = lobby.Teams.FirstOrDefault(t => t.Id == player.TeamId);
            team?.Players.Remove(player);
        }

        lobby.Players.Remove(player);
        session.LobbyId = null;
        session.TeamId = null;

        Log.Information("Player {Username} left lobby {LobbyId}", session.Username, lobby.Id);

                if (player.IsHost)
        {
            var newHost = lobby.Players.FirstOrDefault();
            if (newHost != null)
            {
                newHost.IsHost = true;
                lobby.HostId = newHost.Id;
                Log.Information("New host assigned: {Username}", newHost.Username);
            }
            else
            {
                                _lobbies.TryRemove(lobby.Id, out _);
                Log.Information("Lobby {LobbyId} removed (empty)", lobby.Id);
                return;
            }
        }

                CleanupTeams(lobby);

                BroadcastLobbyUpdate(lobby, 1);     }

                public (bool Success, string Message) JoinTeam(ClientSession session, int teamId)
    {
        if (!session.LobbyId.HasValue)
            return (false, "Вы не в лобби");

        var lobby = GetLobby(session.LobbyId.Value);
        if (lobby == null)
            return (false, "Лобби не найдено");

        if (lobby.State != GameState.Waiting)
            return (false, "Игра уже началась");

        var player = lobby.Players.FirstOrDefault(p => p.Id == session.UserId);
        if (player == null)
            return (false, "Игрок не найден");

        var newTeam = lobby.Teams.FirstOrDefault(t => t.Id == teamId);
        if (newTeam == null)
            return (false, "Команда не найдена");

                if (player.TeamId > 0)
        {
            var oldTeam = lobby.Teams.FirstOrDefault(t => t.Id == player.TeamId);
            oldTeam?.Players.Remove(player);
        }

                player.TeamId = teamId;
        session.TeamId = teamId;
        newTeam.Players.Add(player);

                EnsureEmptyTeam(lobby);

        Log.Information("Player {Username} joined team {TeamId} in lobby {LobbyId}", 
            session.Username, teamId, lobby.Id);

                BroadcastLobbyUpdate(lobby, 2); 
        return (true, "Успешно");
    }

                public (bool Success, string Message) UpdateSettings(ClientSession session, GameSettings settings)
    {
        if (!session.LobbyId.HasValue)
            return (false, "Вы не в лобби");

        var lobby = GetLobby(session.LobbyId.Value);
        if (lobby == null)
            return (false, "Лобби не найдено");

        if (lobby.HostId != session.UserId)
            return (false, "Только хост может менять настройки");

        if (lobby.State != GameState.Waiting)
            return (false, "Нельзя менять настройки во время игры");

        lobby.Settings = settings;

        Log.Information("Lobby {LobbyId} settings updated by {Username}", lobby.Id, session.Username);

                BroadcastLobbyUpdate(lobby, 3); 
        return (true, "Настройки обновлены");
    }

                private void EnsureEmptyTeam(Lobby lobby)
    {
        var emptyTeams = lobby.Teams.Where(t => t.Players.Count == 0).ToList();
        
        if (emptyTeams.Count == 0)
        {
                        var newTeamId = lobby.Teams.Max(t => t.Id) + 1;
            lobby.Teams.Add(new Team 
            { 
                Id = newTeamId, 
                Name = $"Команда {newTeamId}" 
            });
        }
        else if (emptyTeams.Count > 1)
        {
                        foreach (var team in emptyTeams.Skip(1))
            {
                lobby.Teams.Remove(team);
            }
        }
    }

                private void CleanupTeams(Lobby lobby)
    {
        var emptyTeams = lobby.Teams.Where(t => t.Players.Count == 0).ToList();
        
                foreach (var team in emptyTeams.Skip(1))
        {
            lobby.Teams.Remove(team);
        }

                if (!lobby.Teams.Any(t => t.Players.Count == 0))
        {
            var newTeamId = lobby.Teams.Any() ? lobby.Teams.Max(t => t.Id) + 1 : 1;
            lobby.Teams.Add(new Team 
            { 
                Id = newTeamId, 
                Name = $"Команда {newTeamId}" 
            });
        }
    }

                public void BroadcastLobbyUpdate(Lobby lobby, byte updateType)
    {
        var packet = XPacketConverter.Serialize(XPacketType.LobbyUpdate, new XPacketLobbyUpdate
        {
            LobbyDataJson = JsonSerializer.Serialize(lobby),
            UpdateType = updateType
        });

        _sessionManager.BroadcastToLobby(lobby.Id, packet);
    }

                public (bool CanStart, string Message) ValidateGameStart(Lobby lobby)
    {
        var teamsWithPlayers = lobby.Teams.Where(t => t.Players.Count > 0).ToList();

        if (teamsWithPlayers.Count < 2)
            return (false, "Нужно минимум 2 команды с игроками");

        foreach (var team in teamsWithPlayers)
        {
            if (team.Players.Count < 1)
                return (false, $"В команде '{team.Name}' нет игроков");
        }

        return (true, "OK");
    }
}
