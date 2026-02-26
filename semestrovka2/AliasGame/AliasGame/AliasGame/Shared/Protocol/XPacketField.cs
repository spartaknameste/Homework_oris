namespace AliasGame.Shared.Protocol;

public class XPacketField
{
                public byte FieldID { get; set; }

                public ushort FieldSize { get; set; }

                public byte[] Contents { get; set; } = Array.Empty<byte>();
}