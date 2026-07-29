using TraderPro.Application.Common.Commands;

namespace TraderPro.Application.Procurement.Poc;

public static class ProcurementPocOperationTypes
{
    public const string StartReceivingSession = "StartReceivingSession";
    public const string RecordReceivingEntry = "RecordReceivingEntry";
    public const string SubmitReceivingSession = "SubmitReceivingSession";
}

public static class ProcurementPocCommandTypes
{
    public const string MobileSyncOperation =
        "Procurement.Poc.MobileSyncOperation";
    public const string Heartbeat =
        "Procurement.Poc.ReceivingSession.Heartbeat";
    public const string Approve =
        "Procurement.Poc.ReceivingSession.Approve";
    public const string Finalize =
        "Procurement.Poc.ReceivingSession.Finalize";
}

public sealed record ProcurementPocOptions(int LeaseMinutes)
{
    public static ProcurementPocOptions DevelopmentDefault { get; } = new(5);
}

public sealed record MobileSyncOperationsCommand(
    IReadOnlyList<MobileSyncOperationCommand>? Operations);

public sealed record MobileSyncOperationCommand(
    string? OperationId,
    string? OperationType,
    string? AggregateId,
    long LocalSequence,
    long? ExpectedCloudVersion,
    string? PayloadJson,
    string? PayloadHash,
    string? LeaseId = null);

public sealed record MobileSyncOperationsResult(
    IReadOnlyList<MobileSyncOperationResult> Operations);

public sealed record MobileSyncOperationResult(
    Guid OperationId,
    Guid AggregateId,
    long LocalSequence,
    string ResultStatus,
    long? CloudAggregateVersion = null,
    string? CloudReference = null,
    Guid? LeaseId = null,
    DateTimeOffset? LeaseExpiresAtUtc = null,
    string? ErrorCode = null,
    string? Message = null);

public sealed record StartReceivingSessionPocPayload(
    string? OperationId,
    string? LocalSessionId,
    string? TemporaryReference,
    DateTimeOffset? CreatedAtDeviceUtc);

public sealed record RecordReceivingEntryPocPayload(
    string? ProductReference,
    string? BagTypeReference,
    int BagCount,
    string? RawWeightKg,
    string? ProcessedWeightKg,
    string? DisplayWeightKg,
    int DecimalPlaces,
    string? ProcessingMethod,
    string? WeightSource,
    DateTimeOffset CapturedAtDeviceUtc,
    string? OperationId = null,
    string? LocalSessionId = null,
    string? CloudSessionId = null,
    long? LocalSequence = null);

public sealed record SubmitReceivingSessionPocPayload(
    string? OperationId = null,
    string? LocalSessionId = null);

public sealed record ReceivingPocCommandResult(
    Guid SessionId,
    string CloudReference,
    string Status,
    Guid EditorDeviceId,
    Guid? LeaseId,
    DateTimeOffset? LeaseExpiresAtUtc,
    int EntryCount,
    string ProcessedTotalWeightKg,
    long Version,
    Guid? FinalizationId = null);

public sealed record ReceivingPocHeartbeatCommand(
    Guid SessionId,
    Guid LeaseId);

public sealed record ReceivingPocVersionedCommand(
    Guid SessionId,
    long ExpectedVersion);

public sealed record ReceivingPocEntryView(
    Guid EntryId,
    long LocalSequence,
    string ProductReference,
    string BagTypeReference,
    int BagCount,
    string RawWeightKg,
    string ProcessedWeightKg,
    string DisplayWeightKg,
    int DecimalPlaces,
    string ProcessingMethod,
    string WeightSource,
    DateTimeOffset CapturedAtDeviceUtc,
    DateTimeOffset AcceptedAtServerUtc);

public sealed record ReceivingPocLiveView(
    Guid SessionId,
    string CloudReference,
    string Status,
    Guid EditorDeviceId,
    DateTimeOffset? LeaseExpiresAtUtc,
    int EntryCount,
    string ProcessedTotalWeightKg,
    long Version,
    IReadOnlyList<ReceivingPocEntryView> RecentEntries,
    DateTimeOffset LastCloudUpdateAtUtc);

public sealed record ReceivingPocListItem(
    Guid SessionId,
    string CloudReference,
    string Status,
    Guid EditorDeviceId,
    DateTimeOffset? LeaseExpiresAtUtc,
    int EntryCount,
    string ProcessedTotalWeightKg,
    long Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record ReceivingPocListResult(
    IReadOnlyList<ReceivingPocListItem> Sessions,
    long? NextCursor,
    bool HasMore);

public sealed record ProcurementPocBootstrapResult(
    Guid WorkspaceId,
    Guid CompanyId,
    Guid BranchId,
    Guid OperatorDeviceId,
    Guid OwnerDeviceId,
    string SetupCode);

public interface IReceivingPocService
{
    Task<MobileSyncOperationsResult> ProcessOperationsAsync(
        MobileSyncOperationsCommand command,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ReceivingPocCommandResult>> HeartbeatAsync(
        ReceivingPocHeartbeatCommand command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ReceivingPocCommandResult>> ApproveAsync(
        ReceivingPocVersionedCommand command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ReceivingPocCommandResult>> FinalizeAsync(
        ReceivingPocVersionedCommand command,
        string idempotencyKey,
        string correlationId,
        CancellationToken cancellationToken);

    Task<ReceivingPocLiveView?> FindLiveViewAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<ReceivingPocListResult> ListAsync(
        string? status,
        long? after,
        int limit,
        CancellationToken cancellationToken);

    Task<ProcurementPocBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Binds temporary development/testing workspace and device headers.
/// This is not authentication or authorization.
/// </summary>
public interface ITemporaryDeviceContextResolver
{
    Task BindAsync(
        Guid workspaceId,
        Guid deviceId,
        CancellationToken cancellationToken);
}
