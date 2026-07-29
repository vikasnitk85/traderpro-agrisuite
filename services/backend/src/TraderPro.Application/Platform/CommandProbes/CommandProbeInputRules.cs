using System.Text.RegularExpressions;
using TraderPro.Application.Common.Errors;
using TraderPro.Domain.Platform;

namespace TraderPro.Application.Platform.CommandProbes;

public static partial class CommandProbeInputRules
{
    public static string RequireIdempotencyKey(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw Problem(
                "IDEMPOTENCY_KEY_REQUIRED",
                "An Idempotency-Key header is required.");
        }

        if (!IdempotencyKeyPattern().IsMatch(value))
        {
            throw Problem(
                "IDEMPOTENCY_KEY_INVALID",
                "The idempotency key must contain 1 to 200 letters, digits, dots, underscores, colons, or hyphens.");
        }

        return value;
    }

    public static string RequireName(string? value)
    {
        var normalized = CommandProbeRequestHash.NormalizeName(value ?? string.Empty);
        if (normalized.Length is 0 or > CommandProbe.MaximumNameLength)
        {
            throw Problem(
                "COMMAND_PROBE_NAME_INVALID",
                $"Name must contain 1 to {CommandProbe.MaximumNameLength} characters.",
                "name");
        }

        return normalized;
    }

    public static int RequirePositiveDelta(int delta)
    {
        if (delta <= 0)
        {
            throw Problem(
                "COMMAND_PROBE_DELTA_INVALID",
                "Delta must be a positive whole number.",
                "delta");
        }

        return delta;
    }

    public static long RequireExpectedVersion(long expectedVersion)
    {
        if (expectedVersion <= 0)
        {
            throw Problem(
                "COMMAND_PROBE_EXPECTED_VERSION_REQUIRED",
                "X-Expected-Version must be a positive integer.");
        }

        return expectedVersion;
    }

    private static ApplicationProblemException Problem(
        string code,
        string message,
        string? field = null)
    {
        return new ApplicationProblemException(
            code,
            message,
            ApplicationErrorCategory.Validation,
            fieldErrors: field is null
                ? []
                : [new ApplicationFieldError(field, code, message)]);
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,200}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdempotencyKeyPattern();
}
