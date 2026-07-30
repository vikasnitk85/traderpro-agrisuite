using Microsoft.EntityFrameworkCore;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Platform.Identity;

internal sealed class PostgreSqlIdentitySessionLock(
    TraderProDbContext dbContext)
{
    // Lock hierarchy:
    // 05 development bootstrap
    // 10 user session
    // 20 activation device
    // 30 device credential
    // 40 token family (UUID order for collections)
    // 50 token identity
    // 60 command idempotency
    public Task AcquireDevelopmentBootstrapAsync(
        string workspaceCode,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"05:development-bootstrap:{workspaceCode}",
            cancellationToken);
    }

    public Task AcquireUserSessionAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"10:user-session:{workspaceId:D}:{userId:D}",
            cancellationToken);
    }

    public Task AcquireActivationDeviceAsync(
        Guid workspaceId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"20:activation-device:{workspaceId:D}:{deviceId:D}",
            cancellationToken);
    }

    public Task AcquireDeviceCredentialAsync(
        Guid workspaceId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"30:device-credential:{workspaceId:D}:{deviceId:D}",
            cancellationToken);
    }

    public Task AcquireTokenFamilyAsync(
        Guid workspaceId,
        Guid familyId,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"40:token-family:{workspaceId:D}:{familyId:D}",
            cancellationToken);
    }

    public async Task AcquireTokenFamiliesAsync(
        Guid workspaceId,
        IEnumerable<Guid> familyIds,
        CancellationToken cancellationToken)
    {
        foreach (var familyId in familyIds
                     .Where(id => id != Guid.Empty)
                     .Distinct()
                     .OrderBy(id => id))
        {
            await AcquireTokenFamilyAsync(
                workspaceId,
                familyId,
                cancellationToken);
        }
    }

    public Task AcquireTokenIdentityAsync(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"50:token-identity:{workspaceId:D}:{tokenId:D}",
            cancellationToken);
    }

    public Task AcquireCommandIdempotencyAsync(
        Guid workspaceId,
        string commandType,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return AcquireAsync(
            $"60:command-idempotency:{workspaceId:D}:{commandType}:{idempotencyKey}",
            cancellationToken);
    }

    private Task AcquireAsync(
        string scope,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Identity advisory locks require an active database transaction.");
        }

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))",
            cancellationToken);
    }
}
