using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TraderPro.Application.Platform.Identity;

namespace TraderPro.Application.Common.MasterData;

public static class CommercialMasterDataRequestHash
{
    public static string Compute(
        string commandType,
        IAuthenticatedTraderProContext context,
        params (string Name, object? Value)[] values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(context);
        if (!context.IsBound)
        {
            throw new InvalidOperationException(
                "Authenticated commercial context is required.");
        }

        var builder = new StringBuilder();
        Append(builder, "commandType", commandType);
        Append(builder, "workspaceId", context.WorkspaceId.ToString("D"));
        Append(builder, "companyId", context.CompanyId.ToString("D"));
        Append(builder, "defaultBranchId", context.DefaultBranchId.ToString("D"));
        Append(builder, "userId", context.UserId.ToString("D"));
        Append(builder, "deviceId", context.DeviceId.ToString("D"));
        foreach (var (name, value) in values)
        {
            Append(builder, name, Format(value));
        }

        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    private static void Append(
        StringBuilder builder,
        string name,
        string value)
    {
        builder
            .Append(name)
            .Append(':')
            .Append(Encoding.UTF8.GetByteCount(value).ToString(
                CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value)
            .Append('\n');
    }

    private static string Format(object? value)
    {
        return value switch
        {
            null => "<null>",
            Guid guid => guid.ToString("D"),
            bool boolean => boolean ? "true" : "false",
            decimal number => number.ToString(
                "0.000000",
                CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(
                null,
                CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }
}
