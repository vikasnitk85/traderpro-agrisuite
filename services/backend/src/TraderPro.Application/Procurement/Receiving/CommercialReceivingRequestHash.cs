using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TraderPro.Application.Procurement.Receiving;

public static class CommercialReceivingRequestHash
{
    public const string ContractVersion = "v1";

    public static string PayloadHash(string payloadJson) =>
        Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson)))
            .ToLowerInvariant();

    public static string ForMobileOperation(
        string operationType,
        Guid operationId,
        Guid sessionId,
        long localSequence,
        long? ownershipGeneration,
        Guid workspaceId,
        Guid companyId,
        Guid userId,
        Guid deviceId,
        string payloadHash,
        string payloadJson)
    {
        var builder = new StringBuilder();
        Append(builder, "scope", CommercialReceivingOperationTypes.SharedCommandScope);
        Append(builder, "contract", ContractVersion);
        Append(builder, "operationType", operationType);
        Append(builder, "operationId", operationId.ToString("D"));
        Append(builder, "sessionId", sessionId.ToString("D"));
        Append(builder, "localSequence", localSequence.ToString(CultureInfo.InvariantCulture));
        Append(builder, "ownershipGeneration", ownershipGeneration?.ToString(CultureInfo.InvariantCulture) ?? "null");
        Append(builder, "workspaceId", workspaceId.ToString("D"));
        Append(builder, "companyId", companyId.ToString("D"));
        Append(builder, "userId", userId.ToString("D"));
        Append(builder, "deviceId", deviceId.ToString("D"));
        Append(builder, "payloadHash", payloadHash);
        Append(builder, "payloadJson", payloadJson);
        return Hash(builder.ToString());
    }

    public static string ForDirectCommand(
        string commandType,
        Guid workspaceId,
        Guid companyId,
        Guid userId,
        Guid deviceId,
        params object?[] values)
    {
        var builder = new StringBuilder();
        Append(builder, "commandType", commandType);
        Append(builder, "workspaceId", workspaceId.ToString("D"));
        Append(builder, "companyId", companyId.ToString("D"));
        Append(builder, "userId", userId.ToString("D"));
        Append(builder, "deviceId", deviceId.ToString("D"));
        for (var index = 0; index < values.Length; index++)
        {
            Append(builder, $"value{index}", Canonical(values[index]));
        }
        return Hash(builder.ToString());
    }

    private static string Canonical(object? value) => value switch
    {
        null => "null",
        Guid guid => guid.ToString("D"),
        DateTimeOffset timestamp => timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static void Append(StringBuilder builder, string label, string value)
    {
        var byteLength = Encoding.UTF8.GetByteCount(value);
        builder.Append(label).Append(':').Append(byteLength.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\n');
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
