using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform.Identity;

public sealed class DeviceActivationCode :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc
{
    private DeviceActivationCode()
    {
    }

    private DeviceActivationCode(
        Guid workspaceId,
        Guid deviceId,
        string codeHash,
        DateTimeOffset expiresAtUtc,
        Guid issuedByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (workspaceId == Guid.Empty ||
            deviceId == Guid.Empty ||
            issuedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Non-empty ownership identifiers are required.");
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentException(
                "Activation code expiry must follow creation.",
                nameof(expiresAtUtc));
        }

        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        DeviceId = deviceId;
        CodeHash = RequiredHash(codeHash);
        ExpiresAtUtc = expiresAtUtc;
        IssuedByUserId = issuedByUserId;
        CreatedAtUtc = createdAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid DeviceId { get; private set; }

    public string CodeHash { get; private set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? RedemptionIdempotencyKeyHash { get; private set; }

    public string? RedemptionRequestHash { get; private set; }

    public string? ReplayProtectedResult { get; private set; }

    public DateTimeOffset? ReplayAllowedUntilUtc { get; private set; }

    public Guid IssuedByUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static DeviceActivationCode Create(
        Guid workspaceId,
        Guid deviceId,
        string codeHash,
        DateTimeOffset expiresAtUtc,
        Guid issuedByUserId,
        DateTimeOffset createdAtUtc)
    {
        return new DeviceActivationCode(
            workspaceId,
            deviceId,
            codeHash,
            expiresAtUtc,
            issuedByUserId,
            createdAtUtc);
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return EnsureUtc(now, nameof(now)) >= ExpiresAtUtc;
    }

    public void Consume(
        DateTimeOffset now,
        string redemptionIdempotencyKeyHash,
        string redemptionRequestHash,
        string replayProtectedResult,
        DateTimeOffset replayAllowedUntilUtc)
    {
        EnsureAvailable(now);
        EnsureUtc(replayAllowedUntilUtc, nameof(replayAllowedUntilUtc));
        if (replayAllowedUntilUtc <= now ||
            replayAllowedUntilUtc > now.AddDays(1))
        {
            throw new ArgumentException(
                "Activation-result replay expiry must be within one day after consumption.",
                nameof(replayAllowedUntilUtc));
        }

        UsedAtUtc = now;
        RedemptionIdempotencyKeyHash = RequiredHash(
            redemptionIdempotencyKeyHash);
        RedemptionRequestHash = RequiredHash(redemptionRequestHash);
        ReplayProtectedResult = string.IsNullOrWhiteSpace(
            replayProtectedResult) || replayProtectedResult.Length > 4096
            ? throw new ArgumentException(
                "Bounded protected activation-result replay material is required.",
                nameof(replayProtectedResult))
            : replayProtectedResult;
        ReplayAllowedUntilUtc = replayAllowedUntilUtc;
    }

    public void ClearReplayMaterial()
    {
        ReplayProtectedResult = null;
    }

    public void Revoke(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        if (now < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(now),
                "Revocation cannot precede creation.");
        }

        if (UsedAtUtc is not null)
        {
            throw new InvalidOperationException(
                "A consumed activation code cannot be revoked.");
        }

        RevokedAtUtc ??= now;
    }

    private void EnsureAvailable(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));
        if (now < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(now),
                "Consumption cannot precede creation.");
        }

        if (UsedAtUtc is not null)
        {
            throw new InvalidOperationException(
                "The activation code was already used.");
        }

        if (RevokedAtUtc is not null)
        {
            throw new InvalidOperationException(
                "The activation code was revoked.");
        }

        if (now >= ExpiresAtUtc)
        {
            throw new InvalidOperationException(
                "The activation code expired.");
        }
    }

    private static string RequiredHash(string value)
    {
        if (value.Length != 64 ||
            value.Any(character =>
                !char.IsAsciiHexDigit(character) ||
                char.IsAsciiLetterUpper(character)))
        {
            throw new ArgumentException(
                "A lowercase SHA-256 hash is required.",
                nameof(value));
        }

        return value;
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
