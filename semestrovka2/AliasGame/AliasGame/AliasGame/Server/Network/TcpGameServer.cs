using System.Net;
using System.Net.Sockets;
using AliasGame.Shared.Protocol;
using Serilog;

namespace AliasGame.Server.Network;

public class TcpGameServer : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly int _maxConnections;
    private readonly int _bufferSize;
    
    private Socket? _listenerSocket;
    private readonly SessionManager _sessionManager;
    private readonly CancellationTokenSource _cts;
    private bool _isRunning;
    private bool _disposed;

    public event Action<ClientSession, XPacket>? PacketReceived;
    public event Action<ClientSession>? ClientConnected;
    public event Action<ClientSession>? ClientDisconnected;

    public SessionManager SessionManager => _sessionManager;
    public bool IsRunning => _isRunning;

    public TcpGameServer(string host, int port, int maxConnections = 100, int bufferSize = 8192)
    {
        _host = host;
        _port = port;
        _maxConnections = maxConnections;
        _bufferSize = bufferSize;
        _sessionManager = new SessionManager();
        _cts = new CancellationTokenSource();
    }

                public void Start()
    {
        if (_isRunning)
        {
            Log.Warning("Server is already running");
            return;
        }

        try
        {
            var ipAddress = _host == "0.0.0.0" 
                ? IPAddress.Any 
                : IPAddress.Parse(_host);

            _listenerSocket = new Socket(
                ipAddress.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            _listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listenerSocket.Bind(new IPEndPoint(ipAddress, _port));
            _listenerSocket.Listen(_maxConnections);

            _isRunning = true;
            Log.Information("Server started on {Host}:{Port}", _host, _port);

                        Task.Run(() => AcceptClientsAsync(_cts.Token));

                        Task.Run(() => CleanupTaskAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start server");
            throw;
        }
    }

                public void Stop()
    {
        if (!_isRunning) return;

        Log.Information("Stopping server...");
        _isRunning = false;
        _cts.Cancel();

        try
        {
            _listenerSocket?.Close();
        }
        catch { }

        Log.Information("Server stopped");
    }

    private async Task AcceptClientsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _isRunning)
        {
            try
            {
                var clientSocket = await _listenerSocket!.AcceptAsync(ct);
                
                if (_sessionManager.SessionCount >= _maxConnections)
                {
                    Log.Warning("Max connections reached, rejecting client");
                    clientSocket.Close();
                    continue;
                }

                var session = _sessionManager.CreateSession(clientSocket);
                Log.Information("Client connected: Session {SessionId} from {RemoteEndPoint}",
                    session.SessionId, clientSocket.RemoteEndPoint);

                ClientConnected?.Invoke(session);

                                _ = Task.Run(() => HandleClientAsync(session, ct), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error accepting client");
            }
        }
    }

    private async Task HandleClientAsync(ClientSession session, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && session.Socket.Connected)
            {
                var buffer = session.GetReceiveBuffer();
                
                                var bytesReceived = await Task.Run(() =>
                {
                    try
                    {
                        return session.Socket.Receive(buffer);
                    }
                    catch
                    {
                        return 0;
                    }
                }, ct);

                if (bytesReceived == 0)
                {
                                        break;
                }

                                var packets = session.ProcessReceivedData(bytesReceived);

                foreach (var packet in packets)
                {
                    try
                    {
                        PacketReceived?.Invoke(session, packet);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error processing packet for session {SessionId}", session.SessionId);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
                    }
        catch (SocketException)
        {
                    }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling client {SessionId}", session.SessionId);
        }
        finally
        {
            Log.Information("Client disconnected: Session {SessionId}", session.SessionId);
            ClientDisconnected?.Invoke(session);
            _sessionManager.RemoveSession(session.SessionId);
        }
    }

    private async Task CleanupTaskAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
                _sessionManager.CleanupInactiveSessions(TimeSpan.FromMinutes(5));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in cleanup task");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        Stop();
        _cts.Dispose();
        _listenerSocket?.Dispose();
    }
}
