using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class IdempotencyRecord :
    IWorkspaceScoped,
    ICreatedAtUtc
{
    private IdempotencyRecord()
    {
    }

    private IdempotencyRecord(
        Guid workspaceId,
        string idempotencyKey,
        string commandType,
        string requestHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        IdempotencyKey = PlatformEntityGuard.Required(
            idempotencyKey,
            200,
            nameof(idempotencyKey));
        CommandType = PlatformEntityGuard.Required(
            commandType,
            200,
            nameof(commandType));
        RequestHash = PlatformEntityGuard.Required(
            requestHash,
            128,
            nameof(requestHash));
        Status = IdempotencyRecordStatus.Pending;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        ExpiresAtUtc = PlatformEntityGuard.OptionalUtc(
            expiresAtUtc,
            nameof(expiresAtUtc));
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string CommandType { get; private set; } = string.Empty;

    public string RequestHash { get; private set; } = string.Empty;

    public IdempotencyRecordStatus Status { get; private set; }

    public string? ResultPayloadJson { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public static IdempotencyRecord Create(
        Guid workspaceId,
        string idempotencyKey,
        string commandType,
        string requestHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? expiresAtUtc = null)
    {
        return new IdempotencyRecord(
            workspaceId,
            idempotencyKey,
            commandType,
            requestHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public void Complete(
        string? resultPayloadJson,
        DateTimeOffset completedAtUtc)
    {
        EnsurePending();
        ResultPayloadJson = PlatformEntityGuard.OptionalJson(
            resultPayloadJson,
            nameof(resultPayloadJson));
        CompletedAtUtc = PlatformEntityGuard.Utc(
            completedAtUtc,
            nameof(completedAtUtc));
        Status = IdempotencyRecordStatus.Completed;
    }

    public void Fail(
        string? resultPayloadJson,
        DateTimeOffset completedAtUtc)
    {
        EnsurePending();
        ResultPayloadJson = PlatformEntityGuard.OptionalJson(
            resultPayloadJson,
            nameof(resultPayloadJson));
        CompletedAtUtc = PlatformEntityGuard.Utc(
            completedAtUtc,
            nameof(completedAtUtc));
        Status = IdempotencyRecordStatus.Failed;
    }

    private void EnsurePending()
    {
        if (Status != IdempotencyRecordStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending idempotency record can be completed.");
        }
    }
}
