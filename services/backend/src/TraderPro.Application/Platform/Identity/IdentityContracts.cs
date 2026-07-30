using TraderPro.Domain.Platform.Identity;

namespace TraderPro.Application.Platform.Identity;

public sealed record RedeemDeviceActivationRequest(
    string? WorkspaceCode,
    string? ActivationCode,
    string? ClientInstallationReference,
    string? DeviceLabel,
    string? Platform);

public sealed record RedeemDeviceActivationResult(
    Guid DeviceId,
    string DeviceSecret,
    DateTimeOffset ActivatedAtUtc,
    int SecretVersion);

public sealed record LoginRequest(
    string? WorkspaceCode,
    string? Login,
    string? Password,
    Guid DeviceId,
    string? DeviceSecret);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record AuthenticationTokenResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    AuthenticatedUserResult User,
    AuthenticatedWorkspaceResult Workspace,
    AuthenticatedCompanyResult Company,
    AuthenticatedDeviceResult Device);

public sealed record AuthenticatedUserResult(
    Guid UserId,
    string DisplayName,
    string Role);

public sealed record AuthenticatedWorkspaceResult(
    Guid WorkspaceId,
    string WorkspaceCode);

public sealed record AuthenticatedCompanyResult(
    Guid CompanyId,
    Guid DefaultBranchId);

public sealed record AuthenticatedDeviceResult(
    Guid DeviceId,
    string Label);

public sealed record CurrentIdentityResult(
    AuthenticatedUserResult User,
    AuthenticatedWorkspaceResult Workspace,
    AuthenticatedCompanyResult Company,
    AuthenticatedDeviceResult Device,
    Guid TokenFamilyId);

public sealed record DeviceActivationCodeResult(
    Guid DeviceId,
    string ActivationCode,
    DateTimeOffset ExpiresAtUtc);

public sealed record DevelopmentIdentityBootstrapRequest(
    string? WorkspaceCode,
    string? OwnerPassword,
    string? OperatorPassword);

public sealed record DevelopmentIdentityBootstrapResult(
    Guid WorkspaceId,
    string WorkspaceCode,
    Guid CompanyId,
    Guid DefaultBranchId,
    Guid OwnerUserId,
    Guid OperatorUserId,
    Guid OperatorDeviceId,
    Guid OwnerDeviceId,
    DeviceActivationCodeResult OperatorActivation,
    DeviceActivationCodeResult OwnerActivation);

public interface IProductionIdentityService
{
    Task<RedeemDeviceActivationResult> RedeemDeviceActivationAsync(
        RedeemDeviceActivationRequest request,
        string correlationId,
        CancellationToken cancellationToken);

    Task<AuthenticationTokenResult> LoginAsync(
        LoginRequest request,
        string correlationId,
        CancellationToken cancellationToken);

    Task<AuthenticationTokenResult> RefreshAsync(
        RefreshRequest request,
        string correlationId,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        string correlationId,
        CancellationToken cancellationToken);

    Task LogoutAllAsync(
        string correlationId,
        CancellationToken cancellationToken);

    Task<CurrentIdentityResult> GetCurrentAsync(
        CancellationToken cancellationToken);

    Task<DeviceActivationCodeResult> IssueDeviceActivationCodeAsync(
        Guid deviceId,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);

    Task<DevelopmentIdentityBootstrapResult> BootstrapDevelopmentAsync(
        DevelopmentIdentityBootstrapRequest request,
        string correlationId,
        CancellationToken cancellationToken);
}
