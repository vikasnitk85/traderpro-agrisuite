using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class Workspace :
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Workspace()
    {
    }

    private Workspace(
        string code,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        Code = PlatformEntityGuard.Required(code, 64, nameof(code));
        DisplayName = PlatformEntityGuard.Required(
            displayName,
            200,
            nameof(displayName));
        Status = WorkspaceStatus.Active;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public WorkspaceStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Workspace Create(
        string code,
        string displayName,
        DateTimeOffset createdAtUtc)
    {
        return new Workspace(code, displayName, createdAtUtc);
    }

    public void Rename(string displayName)
    {
        DisplayName = PlatformEntityGuard.Required(
            displayName,
            200,
            nameof(displayName));
    }

    public void SetStatus(WorkspaceStatus status)
    {
        Status = PlatformEntityGuard.Defined(status, nameof(status));
    }
}
