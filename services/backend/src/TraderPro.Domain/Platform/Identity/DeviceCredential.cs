using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform.Identity;

public sealed class DeviceCredential :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private DeviceCredential()
    {
    }

    private DeviceCredential(
        Guid workspaceId,
        Guid deviceId,
        string deviceSecretHash,
        string? clientInstallationReferenceHash,
        DateTimeOffset now)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        DeviceId = RequiredId(deviceId, nameof(deviceId));
        DeviceSecretHash = RequiredHash(
            deviceSecretHash,
            nameof(deviceSecretHash));
        ClientInstallationReferenceHash = OptionalHash(
            clientInstallationReferenceHash,
            nameof(clientInstallationReferenceHash));
        SecretVersion = 1;
        ActivatedAtUtc = EnsureUtc(now, nameof(now));
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid DeviceId { get; private set; }

    public string DeviceSecretHash { get; private set; } = string.Empty;

    public int SecretVersion { get; private set; }

    public DateTimeOffset ActivatedAtUtc { get; private set; }

    public DateTimeOffset? LastUsedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? ClientInstallationReferenceHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static DeviceCredential Create(
        Guid workspaceId,
        Guid deviceId,
        string deviceSecretHash,
        string? clientInstallationReferenceHash,
        DateTimeOffset now)
    {
        return new DeviceCredential(
            workspaceId,
            deviceId,
            deviceSecretHash,
            clientInstallationReferenceHash,
            now);
    }

    public void Reactivate(
        string deviceSecretHash,
        string? clientInstallationReferenceHash,
        DateTimeOffset now)
    {
        DeviceSecretHash = RequiredHash(
            deviceSecretHash,
            nameof(deviceSecretHash));
        ClientInstallationReferenceHash = OptionalHash(
            clientInstallationReferenceHash,
            nameof(clientInstallationReferenceHash));
        SecretVersion = checked(SecretVersion + 1);
        ActivatedAtUtc = EnsureUtc(now, nameof(now));
        LastUsedAtUtc = null;
        RevokedAtUtc = null;
    }

    public void MarkUsed(DateTimeOffset now)
    {
        LastUsedAtUtc = EnsureUtc(now, nameof(now));
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAtUtc ??= EnsureUtc(now, nameof(now));
    }

    private static Guid RequiredId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty identifier is required.",
                name);
        }

        return value;
    }

    private static string RequiredHash(string? value, string name)
    {
        return OptionalHash(value, name) ??
            throw new ArgumentException("A hash is required.", name);
    }

    private static string? OptionalHash(string? value, string name)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length != 64 ||
            value.Any(character =>
                !char.IsAsciiHexDigit(character) ||
                char.IsAsciiLetterUpper(character)))
        {
            throw new ArgumentException(
                "A lowercase SHA-256 hash is required.",
                name);
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
