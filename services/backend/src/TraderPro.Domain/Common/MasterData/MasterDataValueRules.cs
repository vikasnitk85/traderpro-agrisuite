using System.Globalization;

namespace TraderPro.Domain.Common.MasterData;

public static class MasterDataValueRules
{
    public const int MaximumCodeLength = 32;
    public const int MaximumNameLength = 200;
    public const int MaximumNotesLength = 2000;

    public static string NormalizeCode(string? value, string field = "code")
    {
        if (value is null ||
            value.Length is < 2 or > MaximumCodeLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw Invalid(
                "MASTER_CODE_INVALID",
                "Code must contain 2 to 32 characters without surrounding whitespace.",
                field);
        }

        var normalized = value.ToUpperInvariant();
        if (normalized.Any(character =>
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9') &&
                character != '-'))
        {
            throw Invalid(
                "MASTER_CODE_INVALID",
                "Code may contain only letters, digits, and hyphens.",
                field);
        }

        return normalized;
    }

    public static string RequiredText(
        string? value,
        int maximumLength,
        string field,
        string code = "MASTER_NAME_INVALID")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid(code, $"{field} is required.", field);
        }

        return ValidateText(value.Trim(), maximumLength, field, code);
    }

    public static string? OptionalText(
        string? value,
        int maximumLength,
        string field,
        string code = "MASTER_NAME_INVALID")
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : ValidateText(value.Trim(), maximumLength, field, code);
    }

    public static string NormalizeRegistration(string? value)
    {
        var registration = RequiredText(
            value,
            64,
            "registrationNumber",
            "RECEIVING_VEHICLE_INVALID");
        var normalizedCharacters = registration
            .Where(character => character is not ' ' and not '-')
            .Select(character => char.ToUpper(character, CultureInfo.InvariantCulture))
            .ToArray();
        var normalized = new string(normalizedCharacters);
        if (normalized.Length is < 2 or > 32 ||
            normalized.Any(character =>
                character is not (>= 'A' and <= 'Z') &&
                character is not (>= '0' and <= '9')))
        {
            throw Invalid(
                "RECEIVING_VEHICLE_INVALID",
                "Registration number must normalize to 2 to 32 letters or digits.",
                "registrationNumber");
        }

        return normalized;
    }

    public static decimal ValidateTareWeight(decimal value)
    {
        const decimal maximum = 99999999999999.999999m;
        var scale = (decimal.GetBits(value)[3] >> 16) & 0x7F;
        if (value < 0m || value > maximum || scale > 6)
        {
            throw Invalid(
                "BAG_TYPE_TARE_WEIGHT_INVALID",
                "Standard tare weight must be a non-negative decimal with at most 14 integer and 6 fractional digits.",
                "standardTareWeightKg");
        }

        return value;
    }

    public static void RequireActive(MasterDataStatus status)
    {
        if (status is not MasterDataStatus.Inactive)
        {
            throw Invalid(
                "MASTER_STATUS_INVALID",
                "Only an inactive master record can be reactivated.",
                "status");
        }
    }

    public static void RequireInactive(MasterDataStatus status)
    {
        if (status is not MasterDataStatus.Active)
        {
            throw Invalid(
                "MASTER_STATUS_INVALID",
                "Only an active master record can be deactivated.",
                "status");
        }
    }

    public static Guid RequiredId(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw Invalid(
                "MASTER_NAME_INVALID",
                $"{field} must be a non-empty UUID.",
                field);
        }

        return value;
    }

    public static DateTimeOffset RequireUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must have a zero UTC offset.",
                nameof(value));
        }

        return value;
    }

    public static DateTimeOffset RequireRevisionTimestamp(
        DateTimeOffset currentUpdatedAtUtc,
        DateTimeOffset nextUpdatedAtUtc)
    {
        var current = RequireUtc(currentUpdatedAtUtc);
        var next = RequireUtc(nextUpdatedAtUtc);
        if (next < current)
        {
            throw new ArgumentException(
                "The revision timestamp cannot move backwards.",
                nameof(nextUpdatedAtUtc));
        }

        return next;
    }

    public static long NextVersion(long currentVersion)
    {
        if (currentVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentVersion),
                "The current version must be positive.");
        }

        return checked(currentVersion + 1);
    }

    private static string ValidateText(
        string value,
        int maximumLength,
        string field,
        string code)
    {
        if (value.Length == 0 ||
            value.Length > maximumLength ||
            value.IndexOfAny(['<', '>']) >= 0 ||
            value.Any(char.IsControl))
        {
            throw Invalid(
                code,
                $"{field} must contain safe text no longer than {maximumLength} characters.",
                field);
        }

        return value;
    }

    private static MasterDataDomainException Invalid(
        string code,
        string message,
        string field)
    {
        return new MasterDataDomainException(code, message, field);
    }
}
