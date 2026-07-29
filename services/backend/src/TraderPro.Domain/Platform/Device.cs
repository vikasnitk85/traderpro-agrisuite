using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class Device :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Device()
    {
    }

    private Device(
        Guid workspaceId,
        string installationId,
        string name,
        string platform,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        InstallationId = PlatformEntityGuard.Required(
            installationId,
            200,
            nameof(installationId));
        Name = PlatformEntityGuard.Required(name, 200, nameof(name));
        Platform = PlatformEntityGuard.Required(platform, 50, nameof(platform));
        Status = DeviceStatus.Registered;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string InstallationId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Platform { get; private set; } = string.Empty;

    public DeviceStatus Status { get; private set; }

    public DateTimeOffset? LastSeenAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Device Create(
        Guid workspaceId,
        string installationId,
        string name,
        string platform,
        DateTimeOffset createdAtUtc)
    {
        return new Device(
            workspaceId,
            installationId,
            name,
            platform,
            createdAtUtc);
    }

    public void Rename(string name)
    {
        Name = PlatformEntityGuard.Required(name, 200, nameof(name));
    }

    public void MarkSeen(DateTimeOffset seenAtUtc)
    {
        LastSeenAtUtc = PlatformEntityGuard.Utc(seenAtUtc, nameof(seenAtUtc));
    }

    public void SetStatus(DeviceStatus status)
    {
        Status = PlatformEntityGuard.Defined(status, nameof(status));
    }
}
