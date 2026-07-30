using System.Security.Cryptography;
using System.Text;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

public static class IdentitySecretCryptography
{
    public static string GenerateUrlSafeToken(int byteCount = 32)
    {
        if (byteCount < 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount),
                "Security tokens require at least 128 bits of entropy.");
        }

        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteCount))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    public static bool VerifyHash(string value, string expectedHash)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (expectedHash.Length != 64)
        {
            return false;
        }

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        Span<byte> actual = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(value), actual);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
