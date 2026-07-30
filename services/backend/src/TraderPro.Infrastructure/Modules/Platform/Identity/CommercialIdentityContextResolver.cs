using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Tenancy;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

internal sealed class CommercialIdentityContextResolver(
    TraderProDbContext dbContext,
    CurrentWorkspaceAccessor currentWorkspace,
    CurrentAuthenticatedTraderProContext currentIdentity,
    IClock clock) : ICommercialIdentityContextResolver
{
    public async Task BindAsync(
        AuthenticatedAccessToken token,
        string correlationId,
        bool allowRevokedTokenFamily,
        CancellationToken cancellationToken)
    {
        try
        {
            AuthenticatedContextInvariants.ValidateTokenIdentity(token);
        }
        catch (ArgumentException exception)
        {
            throw InvalidContext(exception);
        }

        var workspace = await dbContext.Workspaces
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == token.WorkspaceId,
                cancellationToken);
        if (workspace?.Status is not WorkspaceStatus.Active)
        {
            throw new ApplicationProblemException(
                "AUTHENTICATED_CONTEXT_INVALID",
                "The authenticated TraderPro context is no longer valid.",
                ApplicationErrorCategory.Authentication);
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == token.UserId &&
                    item.WorkspaceId == token.WorkspaceId,
                cancellationToken);
        if (user?.Status is not PlatformUserStatus.Active)
        {
            throw new ApplicationProblemException(
                "USER_NOT_ACTIVE",
                "The authenticated user is not active.",
                ApplicationErrorCategory.Authentication);
        }

        var device = await dbContext.Devices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == token.DeviceId &&
                    item.WorkspaceId == token.WorkspaceId,
                cancellationToken);
        if (device?.Status is not DeviceStatus.Active)
        {
            throw new ApplicationProblemException(
                "DEVICE_NOT_ACTIVE",
                "The authenticated device is not active.",
                ApplicationErrorCategory.Authentication);
        }

        var userCredential = await dbContext.UserCredentials
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.UserId == token.UserId &&
                    item.WorkspaceId == token.WorkspaceId,
                cancellationToken);
        var deviceCredential = await dbContext.DeviceCredentials
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.DeviceId == token.DeviceId &&
                    item.WorkspaceId == token.WorkspaceId,
                cancellationToken);
        var family = await dbContext.RefreshTokenFamilies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.Id == token.TokenFamilyId &&
                    item.WorkspaceId == token.WorkspaceId,
                cancellationToken);
        if (userCredential is null ||
            deviceCredential is null ||
            deviceCredential.RevokedAtUtc is not null ||
            family is null ||
            family.UserId != token.UserId ||
            family.DeviceId != token.DeviceId ||
            (family.RevokedAtUtc is not null &&
                !allowRevokedTokenFamily) ||
            family.ExpiresAtUtc <= clock.UtcNow ||
            userCredential.CredentialVersion != token.UserCredentialVersion ||
            deviceCredential.SecretVersion != token.DeviceSecretVersion ||
            family.UserCredentialVersion !=
                userCredential.CredentialVersion ||
            family.DeviceSecretVersion != deviceCredential.SecretVersion)
        {
            throw new ApplicationProblemException(
                "AUTHENTICATED_CONTEXT_INVALID",
                "The authenticated TraderPro context is no longer valid.",
                ApplicationErrorCategory.Authentication);
        }

        var companies = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item =>
                item.WorkspaceId == token.WorkspaceId &&
                item.Status == CompanyStatus.Active)
            .ToListAsync(cancellationToken);
        if (companies.Count != 1)
        {
            throw WorkspaceConfigurationInvalid();
        }

        var company = companies[0];
        var branches = await dbContext.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item =>
                item.WorkspaceId == token.WorkspaceId &&
                item.CompanyId == company.Id &&
                item.IsDefault &&
                item.Status == BranchStatus.Active)
            .ToListAsync(cancellationToken);
        if (branches.Count != 1 ||
            company.Id != token.CompanyId ||
            branches[0].Id != token.DefaultBranchId)
        {
            throw WorkspaceConfigurationInvalid();
        }

        currentWorkspace.SetWorkspace(token.WorkspaceId);
        currentIdentity.Bind(
            token.WorkspaceId,
            token.UserId,
            token.DeviceId,
            company.Id,
            branches[0].Id,
            user.Role,
            token.TokenFamilyId,
            correlationId);
    }

    private static ApplicationProblemException InvalidContext(
        Exception? inner = null)
    {
        return new ApplicationProblemException(
            "AUTHENTICATED_CONTEXT_INVALID",
            "The authenticated TraderPro context is invalid.",
            ApplicationErrorCategory.Authentication,
            innerException: inner);
    }

    private static ApplicationProblemException WorkspaceConfigurationInvalid()
    {
        return new ApplicationProblemException(
            "WORKSPACE_CONFIGURATION_INVALID",
            "The workspace does not have one valid commercial company and default branch.",
            ApplicationErrorCategory.Authentication);
    }
}
