namespace TraderPro.Domain.Common;

public interface IWorkspaceScoped
{
    Guid WorkspaceId { get; }
}

public interface IVersionedEntity
{
    long Version { get; }
}

public interface ICreatedAtUtc
{
    DateTimeOffset CreatedAtUtc { get; }
}

public interface IUpdatedAtUtc
{
    DateTimeOffset UpdatedAtUtc { get; }
}

public interface IOccurredAtUtc
{
    DateTimeOffset OccurredAtUtc { get; }
}
