using System.Reflection;
using System.Runtime.InteropServices;

namespace AliasGame.Shared.Protocol;

public static class XPacketConverter
{
                                public static XPacket Serialize(byte type, byte subtype, object obj, bool strict = false)
    {
        var fields = GetFields(obj.GetType());

        if (strict)
        {
            var usedUp = new List<byte>();
            foreach (var field in fields)
            {
                if (usedUp.Contains(field.FieldId))
                    throw new Exception("One field ID used two times.");
                usedUp.Add(field.FieldId);
            }
        }

        var packet = XPacket.Create(type, subtype);

        foreach (var field in fields)
        {
            var value = field.Info.GetValue(obj);
            if (value != null)
            {
                if (value is string strValue)
                {
                    packet.SetString(field.FieldId, strValue);
                }
                else if (value.GetType().IsValueType)
                {
                    packet.SetValue(field.FieldId, value);
                }
            }
        }

        return packet;
    }

                public static XPacket Serialize(XPacketType type, object obj, bool strict = false)
    {
        var (btype, bsubtype) = XPacketTypeManager.GetType(type);
        return Serialize(btype, bsubtype, obj, strict);
    }

                            public static T Deserialize<T>(XPacket packet, bool strict = false) where T : new()
    {
        var fields = GetFields(typeof(T));
        var instance = new T();

        if (fields.Count == 0)
            return instance;

        foreach (var tuple in fields)
        {
            var field = tuple.Info;
            var packetFieldId = tuple.FieldId;

            if (!packet.HasField(packetFieldId))
            {
                if (strict)
                    throw new Exception($"Couldn't get field[{packetFieldId}] for {field.Name}");
                continue;
            }

            object? value = null;

            try
            {
                if (field.FieldType == typeof(string))
                {
                    value = packet.GetString(packetFieldId);
                }
                else if (field.FieldType.IsValueType)
                {
                                        var method = typeof(XPacket).GetMethod("GetValue")?.MakeGenericMethod(field.FieldType);
                    value = method?.Invoke(packet, new object[] { packetFieldId });
                }
            }
            catch (Exception ex)
            {
                if (strict)
                    throw new Exception($"Couldn't get value for field[{packetFieldId}] for {field.Name}: {ex.Message}");
                continue;
            }

            if (value == null)
            {
                if (strict)
                    throw new Exception($"Couldn't get value for field[{packetFieldId}] for {field.Name}");
                continue;
            }

            field.SetValue(instance, value);
        }

        return instance;
    }

                private static List<(FieldInfo Info, byte FieldId)> GetFields(Type t)
    {
        return t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(field => field.GetCustomAttribute<XFieldAttribute>() != null)
            .Select(field => (
                Info: field,
                FieldId: field.GetCustomAttribute<XFieldAttribute>()!.FieldID
            ))
            .ToList();
    }
}
