using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TraderPro.Application.Platform.CommandProbes;

public static class CommandProbeRequestHash
{
    public static string ForCreate(string name)
    {
        var normalizedName = NormalizeName(name);
        return Compute(
            string.Create(
                CultureInfo.InvariantCulture,
                $"commandType:{CommandProbeCommandTypes.Create}\n" +
                $"name:{Encoding.UTF8.GetByteCount(normalizedName)}:{normalizedName}"));
    }

    public static string ForIncrement(
        Guid id,
        long expectedVersion,
        int delta)
    {
        return Compute(
            string.Create(
                CultureInfo.InvariantCulture,
                $"commandType:{CommandProbeCommandTypes.Increment}\n" +
                $"aggregateId:{id:D}\n" +
                $"expectedVersion:{expectedVersion}\n" +
                $"delta:{delta}"));
    }

    public static string NormalizeName(string name)
    {
        return name?.Trim() ?? string.Empty;
    }

    private static string Compute(string canonicalRequest)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalRequest);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
