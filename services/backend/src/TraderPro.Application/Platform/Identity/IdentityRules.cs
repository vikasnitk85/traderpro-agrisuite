using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Application.Platform.Identity;

public static class PasswordPolicy
{
    public const int MinimumLength = 10;
    public const int MaximumLength = 256;

    public static string Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) ||
            password.Length is < MinimumLength or > MaximumLength)
        {
            throw new ArgumentException(
                $"Password length must be between {MinimumLength} and {MaximumLength} characters.",
                nameof(password));
        }

        return password;
    }
}

public static class TraderProAuthorizationRules
{
    public static bool IsKnownRole(string? role)
    {
        return Enum.TryParse<TraderProRole>(
                role,
                ignoreCase: false,
                out var parsed) &&
            Enum.IsDefined(parsed);
    }

    public static bool IsCommercialUser(TraderProRole role)
    {
        return role is TraderProRole.Owner or TraderProRole.Operator;
    }

    public static bool IsOwner(TraderProRole role)
    {
        return role is TraderProRole.Owner;
    }

    public static bool IsOperator(TraderProRole role)
    {
        return role is TraderProRole.Operator;
    }

    public static bool IsOperatorOrOwner(TraderProRole role)
    {
        return role is TraderProRole.Owner or TraderProRole.Operator;
    }
}

public static class TraderProAuthorizationPolicies
{
    public const string CommercialUser = "TraderProCommercialUser";
    public const string Owner = "TraderProOwner";
    public const string Operator = "TraderProOperator";
    public const string OperatorOrOwner = "TraderProOperatorOrOwner";
}
