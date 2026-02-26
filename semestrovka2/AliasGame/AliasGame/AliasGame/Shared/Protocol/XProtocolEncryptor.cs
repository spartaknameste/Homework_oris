using System.Security.Cryptography;
using System.Text;

namespace AliasGame.Shared.Protocol;

public static class XProtocolEncryptor
{
        private static readonly string Key = "2e985f930853919313c96d001cb5701f";

                public static byte[] Encrypt(byte[] data)
    {
        return RijndaelHandler.Encrypt(data, Key);
    }

                public static byte[] Decrypt(byte[] data)
    {
        return RijndaelHandler.Decrypt(data, Key);
    }
}

internal static class RijndaelHandler
{
    private const int Keysize = 256;
    private const int DerivationIterations = 1000;

    public static byte[] Encrypt(byte[] plainBytes, string passPhrase)
    {
        var saltStringBytes = Generate256BitsOfRandomEntropy();
        var ivStringBytes = Generate256BitsOfRandomEntropy();

        using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations, HashAlgorithmName.SHA256);
        var keyBytes = password.GetBytes(Keysize / 8);

        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(keyBytes, ivStringBytes.Take(16).ToArray());
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);

        cryptoStream.Write(plainBytes, 0, plainBytes.Length);
        cryptoStream.FlushFinalBlock();

        var cipherTextBytes = saltStringBytes;
        cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
        cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();

        return cipherTextBytes;
    }

    public static byte[] Decrypt(byte[] cipherBytes, string passPhrase)
    {
        var saltStringBytes = cipherBytes.Take(Keysize / 8).ToArray();
        var ivStringBytes = cipherBytes.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
        var cipherTextBytes = cipherBytes.Skip((Keysize / 8) * 2).ToArray();

        using var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations, HashAlgorithmName.SHA256);
        var keyBytes = password.GetBytes(Keysize / 8);

        using var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(keyBytes, ivStringBytes.Take(16).ToArray());
        using var memoryStream = new MemoryStream(cipherTextBytes);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var outputStream = new MemoryStream();

        cryptoStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] Generate256BitsOfRandomEntropy()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return randomBytes;
    }
}
