using TraderPro.Domain.Common;
using TraderPro.Domain.Platform.Identity;

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
        TraderProRole role,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        Username = PlatformEntityGuard.Required(username, 100, nameof(username));
        DisplayName = PlatformEntityGuard.Required(
            displayName,
            200,
            nameof(displayName));
        Role = PlatformEntityGuard.Defined(role, nameof(role));
        Status = PlatformUserStatus.Active;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public TraderProRole Role { get; private set; }

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
            TraderProRole.Operator,
            createdAtUtc);
    }

    public static PlatformUser Create(
        Guid workspaceId,
        string username,
        string displayName,
        TraderProRole role,
        DateTimeOffset createdAtUtc)
    {
        return new PlatformUser(
            workspaceId,
            username,
            displayName,
            role,
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

    public void SetRole(TraderProRole role)
    {
        Role = PlatformEntityGuard.Defined(role, nameof(role));
    }
}
