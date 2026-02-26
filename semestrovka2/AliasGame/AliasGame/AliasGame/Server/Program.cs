using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AliasGame.Server.Network;
using AliasGame.Server.Game;
using AliasGame.Server.Handlers;
using AliasGame.Shared.ORM;
using Serilog;

namespace AliasGame.Server;

class Program
{
    static async Task Main(string[] args)
    {
                var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

                Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(configuration["Logging:LogPath"] ?? "logs/server.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("=== Alias Game Server ===");
            Log.Information("Starting server...");

                        var connectionString = configuration["Database:ConnectionString"]!;

                        var startupContext = new AliasDbContext(connectionString);
            await startupContext.Database.EnsureCreatedAsync();
            await startupContext.DisposeAsync();
            Log.Information("Database connected");

                        var uowFactory = new UnitOfWorkFactory(connectionString);
            var userService = new UserService(uowFactory);
            var wordService = new WordService(uowFactory);
            var categoryService = new CategoryService(uowFactory);
            var gameHistoryService = new GameHistoryService(uowFactory);

                        var host = configuration["Server:Host"] ?? "0.0.0.0";
            var port = int.Parse(configuration["Server:Port"] ?? "7777");
            var maxConnections = int.Parse(configuration["Server:MaxConnections"] ?? "100");

            var server = new TcpGameServer(host, port, maxConnections);
            var lobbyManager = new LobbyManager(server.SessionManager);
            var gameManager = new GameManager(lobbyManager, server.SessionManager, wordService, gameHistoryService, userService);
            var packetHandler = new PacketHandler(
                server.SessionManager,
                lobbyManager,
                gameManager,
                userService,
                wordService,
                categoryService);

                        server.ClientConnected += session =>
            {
                Log.Information("Client connected: {SessionId}", session.SessionId);
            };

            server.ClientDisconnected += session =>
            {
                Log.Information("Client disconnected: {SessionId} ({Username})",
                    session.SessionId, session.Username ?? "unknown");

                if (session.LobbyId.HasValue)
                {
                    lobbyManager.LeaveLobby(session);
                }
            };

            server.PacketReceived += async (session, packet) =>
            {
                try
                {
                    await packetHandler.HandlePacketAsync(session, packet);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error handling packet for session {SessionId}", session.SessionId);
                }
            };

                        server.Start();

                        var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Log.Information("Shutdown requested...");
            };

            Log.Information("Server is running. Press Ctrl+C to stop.");

                        try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                            }

                        server.Stop();

            Log.Information("Server stopped gracefully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Server crashed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}