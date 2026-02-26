using System.Runtime.InteropServices;
using System.Text;

namespace AliasGame.Shared.Protocol;

public class XPacket
{
        private static readonly byte[] NormalHeader = { 0xAF, 0xAA, 0xAF };
        private static readonly byte[] EncryptedHeader = { 0x95, 0xAA, 0xFF };
        private static readonly byte[] PacketEnding = { 0xFF, 0x00 };

                public byte PacketType { get; private set; }

                public byte PacketSubtype { get; private set; }

                public bool Protected { get; set; }

                public List<XPacketField> Fields { get; set; } = new();

                private bool ChangeHeaders { get; set; }

    private XPacket() { }

                public static XPacket Create(byte type, byte subtype)
    {
        return new XPacket
        {
            PacketType = type,
            PacketSubtype = subtype
        };
    }

                public static XPacket Create(XPacketType type)
    {
        var (btype, bsubtype) = XPacketTypeManager.GetType(type);
        return Create(btype, bsubtype);
    }

                public byte[] ToPacket()
    {
        using var packet = new MemoryStream();

                var header = ChangeHeaders ? EncryptedHeader : NormalHeader;
        packet.Write(header, 0, 3);
        packet.WriteByte(PacketType);
        packet.WriteByte(PacketSubtype);

                var fields = Fields.OrderBy(field => field.FieldID);
        foreach (var field in fields)
        {
            packet.WriteByte(field.FieldID);
                        packet.WriteByte((byte)(field.FieldSize & 0xFF));
            packet.WriteByte((byte)((field.FieldSize >> 8) & 0xFF));
            if (field.Contents.Length > 0)
            {
                packet.Write(field.Contents, 0, field.Contents.Length);
            }
        }

                packet.Write(PacketEnding, 0, 2);

        return packet.ToArray();
    }

                public static XPacket? Parse(byte[] packet, bool markAsEncrypted = false)
    {
                if (packet.Length < 7)
            return null;

        bool encrypted = false;

                if (packet[0] == NormalHeader[0] &&
            packet[1] == NormalHeader[1] &&
            packet[2] == NormalHeader[2])
        {
            encrypted = false;
        }
                else if (packet[0] == EncryptedHeader[0] &&
                 packet[1] == EncryptedHeader[1] &&
                 packet[2] == EncryptedHeader[2])
        {
            encrypted = true;
        }
        else
        {
            return null;
        }

                var lastIndex = packet.Length - 1;
        if (packet[lastIndex - 1] != PacketEnding[0] ||
            packet[lastIndex] != PacketEnding[1])
        {
            return null;
        }

        var type = packet[3];
        var subtype = packet[4];

        var xpacket = Create(type, subtype);
        xpacket.Protected = markAsEncrypted;

                var fields = packet.Skip(5).ToArray();

        while (true)
        {
                        if (fields.Length == 2)
            {
                return encrypted ? DecryptPacket(xpacket) : xpacket;
            }

                        if (fields.Length < 3)
                return null;

            var id = fields[0];
                        ushort size = (ushort)(fields[1] | (fields[2] << 8));

                        if (fields.Length < 3 + size)
                return null;

            var contents = size != 0
                ? fields.Skip(3).Take(size).ToArray()
                : Array.Empty<byte>();

            xpacket.Fields.Add(new XPacketField
            {
                FieldID = id,
                FieldSize = size,
                Contents = contents
            });

            fields = fields.Skip(3 + size).ToArray();
        }
    }

                public XPacketField? GetField(byte id)
    {
        return Fields.FirstOrDefault(field => field.FieldID == id);
    }

                public bool HasField(byte id)
    {
        return GetField(id) != null;
    }

                public T GetValue<T>(byte id) where T : struct
    {
        var field = GetField(id);

        if (field == null)
            throw new Exception($"Field with ID {id} wasn't found.");

        var neededSize = Marshal.SizeOf(typeof(T));

        if (field.FieldSize != neededSize)
            throw new Exception($"Can't convert field to type {typeof(T).FullName}. " +
                              $"We have {field.FieldSize} bytes but need exactly {neededSize}.");

        return ByteArrayToFixedObject<T>(field.Contents);
    }

                public void SetValue(byte id, object structure)
    {
        if (!structure.GetType().IsValueType)
            throw new Exception("Only value types are available.");

        var field = GetField(id);

        if (field == null)
        {
            field = new XPacketField { FieldID = id };
            Fields.Add(field);
        }

        var bytes = FixedObjectToByteArray(structure);

        if (bytes.Length > ushort.MaxValue)
            throw new Exception("Object is too big. Max length is 65535 bytes.");

        field.FieldSize = (ushort)bytes.Length;
        field.Contents = bytes;
    }

                public byte[] GetValueRaw(byte id)
    {
        var field = GetField(id);

        if (field == null)
            throw new Exception($"Field with ID {id} wasn't found.");

        return field.Contents;
    }

                public void SetValueRaw(byte id, byte[] rawData)
    {
        var field = GetField(id);

        if (field == null)
        {
            field = new XPacketField { FieldID = id };
            Fields.Add(field);
        }

        if (rawData.Length > ushort.MaxValue)
            throw new Exception("Object is too big. Max length is 65535 bytes.");

        field.FieldSize = (ushort)rawData.Length;
        field.Contents = rawData;
    }

                public void SetString(byte id, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        SetValueRaw(id, bytes);
    }

                public string GetString(byte id)
    {
        var bytes = GetValueRaw(id);
        return Encoding.UTF8.GetString(bytes);
    }

                public XPacket Encrypt()
    {
        return EncryptPacket(this);
    }

                public XPacket? Decrypt()
    {
        return DecryptPacket(this);
    }

    private static XPacket EncryptPacket(XPacket packet)
    {
        var rawBytes = packet.ToPacket();
        var encrypted = XProtocolEncryptor.Encrypt(rawBytes);

        var p = Create(0, 0);
        p.SetValueRaw(0, encrypted);
        p.ChangeHeaders = true;

        return p;
    }

    private static XPacket? DecryptPacket(XPacket packet)
    {
        if (!packet.HasField(0))
            return null;

        var rawData = packet.GetValueRaw(0);
        var decrypted = XProtocolEncryptor.Decrypt(rawData);

        return Parse(decrypted, true);
    }

                public static byte[] FixedObjectToByteArray(object value)
    {
        var rawsize = Marshal.SizeOf(value);
        var rawdata = new byte[rawsize];

        var handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);

        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
        }
        finally
        {
            handle.Free();
        }

        return rawdata;
    }

                private static T ByteArrayToFixedObject<T>(byte[] bytes) where T : struct
    {
        T structure;

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        try
        {
            structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T))!;
        }
        finally
        {
            handle.Free();
        }

        return structure;
    }
}