using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform.Identity;

public sealed class RefreshTokenFamily :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc
{
    private RefreshTokenFamily()
    {
    }

    private RefreshTokenFamily(
        Guid workspaceId,
        Guid userId,
        Guid deviceId,
        int userCredentialVersion,
        int deviceSecretVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (workspaceId == Guid.Empty ||
            userId == Guid.Empty ||
            deviceId == Guid.Empty)
        {
            throw new ArgumentException(
                "Non-empty ownership identifiers are required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (userCredentialVersion <= 0 || deviceSecretVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userCredentialVersion),
                "Credential versions must be positive.");
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Family expiry must follow creation.",
                nameof(expiresAtUtc));
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        UserId = userId;
        DeviceId = deviceId;
        UserCredentialVersion = userCredentialVersion;
        DeviceSecretVersion = deviceSecretVersion;
        CreatedAtUtc = createdAtUtc;
        LastUsedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid DeviceId { get; private set; }

    public int UserCredentialVersion { get; private set; }

    public int DeviceSecretVersion { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastUsedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RevocationReason { get; private set; }

    public long Version { get; private set; }

    public static RefreshTokenFamily Create(
        Guid workspaceId,
        Guid userId,
        Guid deviceId,
        int userCredentialVersion,
        int deviceSecretVersion,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new RefreshTokenFamily(
            workspaceId,
            userId,
            deviceId,
            userCredentialVersion,
            deviceSecretVersion,
            createdAtUtc,
            expiresAtUtc);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return EnsureUtc(now, nameof(now)) >= ExpiresAtUtc;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        now = EnsureUtc(now, nameof(now));
        if (RevokedAtUtc is not null || now >= ExpiresAtUtc)
        {
            throw new InvalidOperationException(
                "An inactive token family cannot be used.");
        }

        if (now > LastUsedAtUtc)
        {
            LastUsedAtUtc = now;
        }
    }

    public bool Revoke(DateTimeOffset now, string reason)
    {
        now = EnsureUtc(now, nameof(now));
        if (RevokedAtUtc is not null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 200)
        {
            throw new ArgumentException(
                "A bounded revocation reason is required.",
                nameof(reason));
        }

        RevokedAtUtc = now;
        RevocationReason = reason.Trim();
        return true;
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must have a zero UTC offset.",
                name);
        }

        return value;
    }
}
