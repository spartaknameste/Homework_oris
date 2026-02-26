using System.Net.Sockets;
using AliasGame.Shared.Protocol;

namespace AliasGame.Client.Network;

public class GameClient : IDisposable
{
    private Socket? _socket;
    private readonly string _host;
    private readonly int _port;
    private readonly byte[] _receiveBuffer = new byte[8192];
    private readonly MemoryStream _packetBuffer = new();
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private bool _isConnected;

    public event Action<XPacket>? PacketReceived;
    public event Action? Connected;
    public event Action<string>? Disconnected;
    public event Action<Exception>? Error;

    public bool IsConnected => _isConnected && _socket?.Connected == true;

    public GameClient(string host, int port)
    {
        _host = host;
        _port = port;
    }

                public async Task ConnectAsync()
    {
        if (_isConnected) return;

        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(_host, _port);
            _isConnected = true;
            _cts = new CancellationTokenSource();

            Connected?.Invoke();

                        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            throw;
        }
    }

                public void Disconnect(string reason = "Client disconnected")
    {
        if (!_isConnected) return;

        _isConnected = false;
        _cts?.Cancel();

        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
        }
        catch { }

        _socket?.Close();
        _socket = null;

        Disconnected?.Invoke(reason);
    }

                public void Send(XPacket packet)
    {
        if (!IsConnected) return;

        try
        {
            var data = packet.ToPacket();
            _socket?.Send(data);
        }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            Disconnect("Send error");
        }
    }

                public void Send(XPacketType type, object packetData)
    {
        var packet = XPacketConverter.Serialize(type, packetData);
        Send(packet);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                var bytesReceived = await Task.Run(() =>
                {
                    try
                    {
                        return _socket?.Receive(_receiveBuffer) ?? 0;
                    }
                    catch
                    {
                        return 0;
                    }
                }, ct);

                if (bytesReceived == 0)
                {
                    Disconnect("Server closed connection");
                    break;
                }

                ProcessReceivedData(bytesReceived);
            }
        }
        catch (OperationCanceledException)
        {
                    }
        catch (Exception ex)
        {
            Error?.Invoke(ex);
            Disconnect("Receive error");
        }
    }

    private void ProcessReceivedData(int bytesReceived)
    {
        _packetBuffer.Write(_receiveBuffer, 0, bytesReceived);
        var bufferData = _packetBuffer.ToArray();
        int offset = 0;

        while (offset < bufferData.Length)
        {
            if (bufferData.Length - offset < 7) break;

            if (!IsValidHeader(bufferData, offset))
            {
                offset++;
                continue;
            }

            int endIndex = FindPacketEnd(bufferData, offset);
            if (endIndex == -1) break;

            int packetLength = endIndex - offset + 1;
            var packetData = new byte[packetLength];
            Array.Copy(bufferData, offset, packetData, 0, packetLength);

            var packet = XPacket.Parse(packetData);
            if (packet != null)
            {
                PacketReceived?.Invoke(packet);
            }

            offset = endIndex + 1;
        }

        if (offset > 0)
        {
            var remaining = bufferData.Skip(offset).ToArray();
            _packetBuffer.SetLength(0);
            if (remaining.Length > 0)
            {
                _packetBuffer.Write(remaining, 0, remaining.Length);
            }
        }
    }

    private bool IsValidHeader(byte[] data, int offset)
    {
        if (offset + 3 > data.Length) return false;
        if (data[offset] == 0xAF && data[offset + 1] == 0xAA && data[offset + 2] == 0xAF) return true;
        if (data[offset] == 0x95 && data[offset + 1] == 0xAA && data[offset + 2] == 0xFF) return true;
        return false;
    }

    private int FindPacketEnd(byte[] data, int startOffset)
    {
        for (int i = startOffset + 6; i < data.Length; i++)
        {
            if (data[i - 1] == 0xFF && data[i] == 0x00) return i;
        }
        return -1;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        _cts?.Dispose();
        _packetBuffer.Dispose();
    }
}
