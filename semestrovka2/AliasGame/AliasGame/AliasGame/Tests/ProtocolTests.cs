using AliasGame.Shared.Protocol;
using AliasGame.Shared.Protocol.Packets;
using FluentAssertions;
using Xunit;

namespace AliasGame.Tests;

public class XPacketTests
{
    [Fact]
    public void Create_ShouldCreatePacketWithCorrectTypeAndSubtype()
    {
                var packet = XPacket.Create(1, 2);

                packet.PacketType.Should().Be(1);
        packet.PacketSubtype.Should().Be(2);
        packet.Fields.Should().BeEmpty();
    }

    [Fact]
    public void SetValue_ShouldAddFieldWithCorrectValue()
    {
                var packet = XPacket.Create(0, 0);

                packet.SetValue(0, 12345);

                packet.Fields.Should().HaveCount(1);
        packet.GetValue<int>(0).Should().Be(12345);
    }

    [Fact]
    public void SetValue_ShouldUpdateExistingField()
    {
                var packet = XPacket.Create(0, 0);
        packet.SetValue(0, 100);

                packet.SetValue(0, 200);

                packet.Fields.Should().HaveCount(1);
        packet.GetValue<int>(0).Should().Be(200);
    }

    [Fact]
    public void SetString_ShouldStoreAndRetrieveString()
    {
                var packet = XPacket.Create(0, 0);
        const string testString = "Hello, World!";

                packet.SetString(0, testString);

                packet.GetString(0).Should().Be(testString);
    }

    [Fact]
    public void ToPacket_ShouldCreateValidBinaryFormat()
    {
                var packet = XPacket.Create(1, 2);
        packet.SetValue(0, 123);

                var bytes = packet.ToPacket();

                bytes.Should().NotBeEmpty();
        bytes[0].Should().Be(0xAF);         bytes[1].Should().Be(0xAA);
        bytes[2].Should().Be(0xAF);
        bytes[3].Should().Be(1);            bytes[4].Should().Be(2);            bytes[^2].Should().Be(0xFF);         bytes[^1].Should().Be(0x00);
    }

    [Fact]
    public void Parse_ShouldCorrectlyParsePacket()
    {
                var original = XPacket.Create(5, 10);
        original.SetValue(0, 42);
        original.SetValue(1, 3.14);
        original.SetString(2, "test");
        var bytes = original.ToPacket();

                var parsed = XPacket.Parse(bytes);

                parsed.Should().NotBeNull();
        parsed!.PacketType.Should().Be(5);
        parsed.PacketSubtype.Should().Be(10);
        parsed.GetValue<int>(0).Should().Be(42);
        parsed.GetValue<double>(1).Should().BeApproximately(3.14, 0.001);
        parsed.GetString(2).Should().Be("test");
    }

    [Fact]
    public void Parse_ShouldReturnNullForInvalidHeader()
    {
                var invalidBytes = new byte[] { 0x00, 0x00, 0x00, 0x01, 0x02, 0xFF, 0x00 };

                var result = XPacket.Parse(invalidBytes);

                result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNullForTooShortPacket()
    {
                var shortBytes = new byte[] { 0xAF, 0xAA, 0xAF };

                var result = XPacket.Parse(shortBytes);

                result.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldReturnNullForInvalidEnding()
    {
                var invalidBytes = new byte[] { 0xAF, 0xAA, 0xAF, 0x01, 0x02, 0x00, 0x00 };

                var result = XPacket.Parse(invalidBytes);

                result.Should().BeNull();
    }

    [Fact]
    public void HasField_ShouldReturnTrueForExistingField()
    {
                var packet = XPacket.Create(0, 0);
        packet.SetValue(5, 100);

                packet.HasField(5).Should().BeTrue();
        packet.HasField(10).Should().BeFalse();
    }

    [Fact]
    public void MultipleFieldTypes_ShouldWorkCorrectly()
    {
                var packet = XPacket.Create(0, 0);

                packet.SetValue(0, 123);
        packet.SetValue(1, 123.456);
        packet.SetValue(2, 123.456f);
        packet.SetValue(3, true);
        packet.SetValue(4, false);
        packet.SetValue(5, (byte)255);
        packet.SetValue(6, (long)9999999999);

        var bytes = packet.ToPacket();
        var parsed = XPacket.Parse(bytes);

                parsed.Should().NotBeNull();
        parsed!.GetValue<int>(0).Should().Be(123);
        parsed.GetValue<double>(1).Should().BeApproximately(123.456, 0.001);
        parsed.GetValue<float>(2).Should().BeApproximately(123.456f, 0.001f);
        parsed.GetValue<bool>(3).Should().BeTrue();
        parsed.GetValue<bool>(4).Should().BeFalse();
        parsed.GetValue<byte>(5).Should().Be(255);
        parsed.GetValue<long>(6).Should().Be(9999999999);
    }

    [Fact]
    public void GetValue_ShouldThrowForMissingField()
    {
                var packet = XPacket.Create(0, 0);

                packet.Invoking(p => p.GetValue<int>(99))
            .Should().Throw<Exception>()
            .WithMessage("*wasn't found*");
    }
}

public class XPacketConverterTests
{
    [Fact]
    public void Serialize_ShouldCreatePacketFromObject()
    {
                var handshake = new XPacketHandshake
        {
            MagicHandshakeNumber = 12345,
            ProtocolVersion = 1
        };

                var packet = XPacketConverter.Serialize(XPacketType.Handshake, handshake);

                packet.Should().NotBeNull();
        packet.GetValue<int>(0).Should().Be(12345);
        packet.GetValue<int>(1).Should().Be(1);
    }

    [Fact]
    public void Deserialize_ShouldCreateObjectFromPacket()
    {
                var packet = XPacket.Create(1, 0);
        packet.SetValue(0, 54321);
        packet.SetValue(1, 2);

                var handshake = XPacketConverter.Deserialize<XPacketHandshake>(packet);

                handshake.MagicHandshakeNumber.Should().Be(54321);
        handshake.ProtocolVersion.Should().Be(2);
    }

    [Fact]
    public void SerializeDeserialize_ShouldRoundTrip()
    {
                var original = new XPacketLogin
        {
            Username = "testuser",
            PasswordHash = "hashedpassword123"
        };

                var packet = XPacketConverter.Serialize(XPacketType.Login, original);
        var bytes = packet.ToPacket();
        var parsedPacket = XPacket.Parse(bytes);
        var deserialized = XPacketConverter.Deserialize<XPacketLogin>(parsedPacket!);

                deserialized.Username.Should().Be("testuser");
        deserialized.PasswordHash.Should().Be("hashedpassword123");
    }
}

public class XPacketTypeManagerTests
{
    [Fact]
    public void GetType_ShouldReturnCorrectTypeAndSubtype()
    {
                var (type, subtype) = XPacketTypeManager.GetType(XPacketType.Handshake);

                type.Should().Be(1);
        subtype.Should().Be(0);
    }

    [Fact]
    public void GetTypeFromPacket_ShouldIdentifyPacketType()
    {
                var (type, subtype) = XPacketTypeManager.GetType(XPacketType.Login);
        var packet = XPacket.Create(type, subtype);

                var identifiedType = XPacketTypeManager.GetTypeFromPacket(packet);

                identifiedType.Should().Be(XPacketType.Login);
    }

    [Fact]
    public void GetTypeFromPacket_ShouldReturnUnknownForUnregisteredType()
    {
                var packet = XPacket.Create(255, 255);

                var type = XPacketTypeManager.GetTypeFromPacket(packet);

                type.Should().Be(XPacketType.Unknown);
    }
}

public class XProtocolEncryptorTests
{
    [Fact]
    public void EncryptDecrypt_ShouldRoundTrip()
    {
                var originalData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                var encrypted = XProtocolEncryptor.Encrypt(originalData);
        var decrypted = XProtocolEncryptor.Decrypt(encrypted);

                decrypted.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void Encrypt_ShouldProduceDifferentOutput()
    {
                var data = new byte[] { 1, 2, 3, 4, 5 };

                var encrypted = XProtocolEncryptor.Encrypt(data);

                encrypted.Should().NotBeEquivalentTo(data);
        encrypted.Length.Should().BeGreaterThan(data.Length);
    }

    [Fact]
    public void EncryptedPacket_ShouldWorkCorrectly()
    {
                var packet = XPacket.Create(5, 10);
        packet.SetValue(0, 42);
        packet.SetString(1, "secret message");

                var encrypted = packet.Encrypt();
        var encryptedBytes = encrypted.ToPacket();
        var parsed = XPacket.Parse(encryptedBytes);

                parsed.Should().NotBeNull();
        parsed!.GetValue<int>(0).Should().Be(42);
        parsed.GetString(1).Should().Be("secret message");
    }
}
