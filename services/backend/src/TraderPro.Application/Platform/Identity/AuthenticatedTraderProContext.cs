using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Application.Platform.Identity;

public interface IAuthenticatedTraderProContext
{
    bool IsBound { get; }

    Guid WorkspaceId { get; }

    Guid UserId { get; }

    Guid DeviceId { get; }

    Guid CompanyId { get; }

    Guid DefaultBranchId { get; }

    TraderProRole Role { get; }

    bool IsOwner { get; }

    bool IsOperatorOrOwner { get; }

    Guid TokenFamilyId { get; }

    string CorrelationId { get; }
}

public sealed record AuthenticatedAccessToken(
    Guid WorkspaceId,
    Guid UserId,
    Guid DeviceId,
    Guid CompanyId,
    Guid DefaultBranchId,
    string ClaimedRole,
    int UserCredentialVersion,
    int DeviceSecretVersion,
    Guid TokenFamilyId,
    string JwtId);

public interface ICommercialIdentityContextResolver
{
    Task BindAsync(
        AuthenticatedAccessToken token,
        string correlationId,
        bool allowRevokedTokenFamily,
        CancellationToken cancellationToken);
}

public static class AuthenticatedContextInvariants
{
    public static void ValidateTokenIdentity(AuthenticatedAccessToken token)
    {
        if (token.WorkspaceId == Guid.Empty ||
            token.UserId == Guid.Empty ||
            token.DeviceId == Guid.Empty ||
            token.CompanyId == Guid.Empty ||
            token.DefaultBranchId == Guid.Empty ||
            token.TokenFamilyId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token.JwtId) ||
            token.UserCredentialVersion <= 0 ||
            token.DeviceSecretVersion <= 0 ||
            !TraderProAuthorizationRules.IsKnownRole(
                token.ClaimedRole))
        {
            throw new ArgumentException(
                "The authenticated token identity is incomplete.",
                nameof(token));
        }
    }
}
