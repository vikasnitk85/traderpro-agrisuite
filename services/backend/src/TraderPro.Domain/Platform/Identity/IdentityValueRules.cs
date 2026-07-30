using System.Globalization;
using System.Text.RegularExpressions;

namespace TraderPro.Domain.Platform.Identity;

public enum TraderProRole : short
{
    Owner = 1,
    Operator = 2,
}

public static partial class WorkspaceCodeNormalizer
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 64;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Workspace code is required and cannot contain surrounding whitespace.",
                nameof(value));
        }

        var normalized = value.ToUpper(CultureInfo.InvariantCulture);
        if (normalized.Length is < MinimumLength or > MaximumLength ||
            !WorkspaceCodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Workspace code must contain 3-64 ASCII letters, numbers, or hyphens.",
                nameof(value));
        }

        return normalized;
    }

    [GeneratedRegex(
        "^[A-Z0-9]+(?:-[A-Z0-9]+)*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex WorkspaceCodePattern();
}

public static class LoginNormalizer
{
    public const int MaximumLength = 100;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Login is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpper(CultureInfo.InvariantCulture);
        if (normalized.Length > MaximumLength)
        {
            throw new ArgumentException(
                $"Login cannot exceed {MaximumLength} characters.",
                nameof(value));
        }

        return normalized;
    }
}
