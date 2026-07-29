using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

/// <summary>
/// Development-only aggregate used to prove the cloud command pipeline.
/// It is not a business document.
/// </summary>
public sealed class CommandProbe :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    public const int MaximumNameLength = 200;

    private CommandProbe()
    {
    }

    private CommandProbe(
        Guid workspaceId,
        string name,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        Name = PlatformEntityGuard.Required(
            name,
            MaximumNameLength,
            nameof(name));
        Counter = 0;
        CreatedAtUtc = PlatformEntityGuard.Utc(
            createdAtUtc,
            nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public long Counter { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static CommandProbe Create(
        Guid workspaceId,
        string name,
        DateTimeOffset createdAtUtc)
    {
        return new CommandProbe(workspaceId, name, createdAtUtc);
    }

    public void Increment(int delta)
    {
        if (delta <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delta),
                delta,
                "The increment must be a positive whole number.");
        }

        Counter = checked(Counter + delta);
    }
}
