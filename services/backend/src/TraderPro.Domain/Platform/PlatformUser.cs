using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class PlatformUser :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private PlatformUser()
    {
    }

    private PlatformUser(
        Guid workspaceId,
        string username,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        Username = PlatformEntityGuard.Required(username, 100, nameof(username));
        DisplayName = PlatformEntityGuard.Required(
            displayName,
            200,
            nameof(displayName));
        Status = PlatformUserStatus.Active;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public PlatformUserStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static PlatformUser Create(
        Guid workspaceId,
        string username,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        return new PlatformUser(
            workspaceId,
            username,
            displayName,
            createdAtUtc);
    }

    public void Rename(string displayName)
    {
        DisplayName = PlatformEntityGuard.Required(
            displayName,
            200,
            nameof(displayName));
    }

    public void SetStatus(PlatformUserStatus status)
    {
        Status = PlatformEntityGuard.Defined(status, nameof(status));
    }
}
