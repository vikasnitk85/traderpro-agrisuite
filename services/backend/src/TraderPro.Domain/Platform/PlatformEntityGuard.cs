using System.Text.Json;

namespace TraderPro.Domain.Platform;

internal static class PlatformEntityGuard
{
    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }

        return value;
    }

    public static string Required(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    public static string? Optional(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Required(value, maximumLength, parameterName);
    }

    public static TEnum Defined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "A defined status value is required.");
        }

        return value;
    }

    public static DateTimeOffset Utc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must have a zero UTC offset.",
                parameterName);
        }

        return value;
    }

    public static DateTimeOffset? OptionalUtc(
        DateTimeOffset? value,
        string parameterName)
    {
        return value is null ? null : Utc(value.Value, parameterName);
    }

    public static string Json(string? value, string parameterName)
    {
        var json = Required(value, int.MaxValue, parameterName);

        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "The value must contain valid JSON.",
                parameterName,
                exception);
        }

        return json;
    }

    public static string? OptionalJson(string? value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Json(value, parameterName);
    }
}
