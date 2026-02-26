using System.Net.Sockets;
using System.Collections.Concurrent;
using AliasGame.Shared.Protocol;
using AliasGame.Shared.Models;

namespace AliasGame.Server.Network;

public class ClientSession : IDisposable
{
    public int SessionId { get; }
    public Socket Socket { get; }
    public DateTime ConnectedAt { get; }
    public DateTime LastActivityAt { get; private set; }
    
        public bool IsAuthenticated { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public bool IsAdmin { get; set; }
    
        public int? LobbyId { get; set; }
    public int? TeamId { get; set; }
    
        public bool HandshakeCompleted { get; set; }
    public int HandshakeMagic { get; set; }
    
        private readonly byte[] _receiveBuffer;
    private readonly MemoryStream _packetBuffer;
    private readonly object _sendLock = new();
    private bool _disposed;

    public ClientSession(int sessionId, Socket socket, int bufferSize = 8192)
    {
        SessionId = sessionId;
        Socket = socket;
        ConnectedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        _receiveBuffer = new byte[bufferSize];
        _packetBuffer = new MemoryStream();
    }

                public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

                public byte[] GetReceiveBuffer() => _receiveBuffer;

                public List<XPacket> ProcessReceivedData(int bytesReceived)
    {
        var packets = new List<XPacket>();
        
                _packetBuffer.Write(_receiveBuffer, 0, bytesReceived);
        
                var bufferData = _packetBuffer.ToArray();
        int offset = 0;
        
        while (offset < bufferData.Length)
        {
                        if (bufferData.Length - offset < 7)
                break;
            
                        if (!IsValidHeader(bufferData, offset))
            {
                offset++;
                continue;
            }
            
                        int endIndex = FindPacketEnd(bufferData, offset);
            if (endIndex == -1)
                break;             
                        int packetLength = endIndex - offset + 1;
            var packetData = new byte[packetLength];
            Array.Copy(bufferData, offset, packetData, 0, packetLength);
            
            var packet = XPacket.Parse(packetData);
            if (packet != null)
            {
                packets.Add(packet);
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
        
        UpdateActivity();
        return packets;
    }

    private bool IsValidHeader(byte[] data, int offset)
    {
        if (offset + 3 > data.Length) return false;
        
                if (data[offset] == 0xAF && data[offset + 1] == 0xAA && data[offset + 2] == 0xAF)
            return true;
        
                if (data[offset] == 0x95 && data[offset + 1] == 0xAA && data[offset + 2] == 0xFF)
            return true;
        
        return false;
    }

    private int FindPacketEnd(byte[] data, int startOffset)
    {
        for (int i = startOffset + 6; i < data.Length; i++)
        {
            if (data[i - 1] == 0xFF && data[i] == 0x00)
            {
                return i;
            }
        }
        return -1;
    }

                public void Send(XPacket packet)
    {
        if (_disposed || !Socket.Connected) return;
        
        try
        {
            var data = packet.ToPacket();
            lock (_sendLock)
            {
                Socket.Send(data);
            }
        }
        catch (SocketException)
        {
                    }
    }

                public void Send(byte[] data)
    {
        if (_disposed || !Socket.Connected) return;
        
        try
        {
            lock (_sendLock)
            {
                Socket.Send(data);
            }
        }
        catch (SocketException)
        {
                    }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        try
        {
            Socket.Shutdown(SocketShutdown.Both);
        }
        catch { }
        
        Socket.Close();
        Socket.Dispose();
        _packetBuffer.Dispose();
    }
}

public class SessionManager
{
    private readonly ConcurrentDictionary<int, ClientSession> _sessions = new();
    private int _nextSessionId = 1;

    public int SessionCount => _sessions.Count;

                public ClientSession CreateSession(Socket socket)
    {
        var sessionId = Interlocked.Increment(ref _nextSessionId);
        var session = new ClientSession(sessionId, socket);
        _sessions.TryAdd(sessionId, session);
        return session;
    }

                public ClientSession? GetSession(int sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

                public ClientSession? GetSessionByUserId(int userId)
    {
        return _sessions.Values.FirstOrDefault(s => s.UserId == userId);
    }

                public IEnumerable<ClientSession> GetSessionsInLobby(int lobbyId)
    {
        return _sessions.Values.Where(s => s.LobbyId == lobbyId);
    }

                public IEnumerable<ClientSession> GetAuthenticatedSessions()
    {
        return _sessions.Values.Where(s => s.IsAuthenticated);
    }

                public void RemoveSession(int sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
        }
    }

                public void BroadcastToLobby(int lobbyId, XPacket packet, int? excludeSessionId = null)
    {
        foreach (var session in GetSessionsInLobby(lobbyId))
        {
            if (excludeSessionId.HasValue && session.SessionId == excludeSessionId.Value)
                continue;
            session.Send(packet);
        }
    }

                public void BroadcastToAll(XPacket packet, int? excludeSessionId = null)
    {
        foreach (var session in GetAuthenticatedSessions())
        {
            if (excludeSessionId.HasValue && session.SessionId == excludeSessionId.Value)
                continue;
            session.Send(packet);
        }
    }

                public void CleanupInactiveSessions(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var inactive = _sessions.Values
            .Where(s => s.LastActivityAt < cutoff)
            .ToList();

        foreach (var session in inactive)
        {
            RemoveSession(session.SessionId);
        }
    }
}
