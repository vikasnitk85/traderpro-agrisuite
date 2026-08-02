using System.Text.Json;
using TraderPro.Application.Common.Commands;

namespace TraderPro.Application.Procurement.Receiving;

public static class CommercialReceivingOperationTypes
{
    public const string Start = "StartCommercialReceivingSession";
    public const string RecordEntry = "RecordCommercialReceivingEntry";
    public const string Submit = "SubmitCommercialReceivingSession";
    public const string SharedCommandScope =
        "Procurement.CommercialReceiving.MobileSyncOperation";
}

public static class CommercialReceivingCommandTypes
{
    public const string Heartbeat =
        "Procurement.CommercialReceiving.Lease.Heartbeat";
    public const string Reacquire =
        "Procurement.CommercialReceiving.Lease.Reacquire";
    public const string Transfer =
        "Procurement.CommercialReceiving.Ownership.Transfer";
    public const string UpdateReferencePolicy =
        "Procurement.CommercialReceiving.ReferencePolicy.Update";
}

public enum CommercialMobileOperationStatus
{
    Accepted,
    PreviouslyProcessed,
    NeedsAttention,
    Rejected,
}

public sealed record CommercialMobileOperationsCommand(
    IReadOnlyList<CommercialMobileOperationCommand> Operations);

public sealed record CommercialMobileOperationCommand(
    Guid OperationId,
    string OperationType,
    Guid SessionId,
    long LocalSequence,
    long? OwnershipGeneration,
    long? ExpectedCloudVersion,
    string PayloadJson,
    string PayloadHash,
    CommercialReceivingLeaseEnvelope? Lease);

public sealed record CommercialReceivingLeaseEnvelope(Guid LeaseId);

public sealed record StartCommercialReceivingSessionPayload(
    Guid OperationId,
    Guid SessionId,
    long LocalSequence,
    Guid SupplierId,
    long SupplierVersion,
    Guid CompanyProcurementSettingsId,
    long ProcurementSettingsVersion,
    Guid DestinationLocationId,
    long DestinationLocationVersion,
    Guid WeightProcessingPolicyId,
    long WeightProcessingPolicyVersion,
    string VehicleSelectionMode,
    Guid? ReceivingVehicleId,
    long? ReceivingVehicleVersion,
    string? ExternalReference,
    DateTimeOffset StartedAtDeviceUtc);

public sealed record RecordCommercialReceivingEntryPayload(
    Guid OperationId,
    Guid EntryId,
    Guid SessionId,
    long LocalSequence,
    Guid ProductId,
    long ProductVersion,
    Guid? SupplierProductScopeId,
    long? SupplierProductScopeVersion,
    Guid BagTypeId,
    long BagTypeVersion,
    Guid? ProductStandardBagWeightId,
    long? ProductStandardBagWeightVersion,
    int BagCount,
    string RawWeightKg,
    string ProcessedWeightKg,
    string DisplayWeightKg,
    int DecimalPlaces,
    string ProcessingMethod,
    string WeightSource,
    DateTimeOffset CapturedAtDeviceUtc);

public sealed record SubmitCommercialReceivingSessionPayload(
    Guid OperationId,
    Guid SessionId,
    long LocalSequence,
    DateTimeOffset SubmittedAtDeviceUtc);

public sealed record CommercialMobileOperationsResult(
    IReadOnlyList<CommercialMobileOperationResult> Operations);

public sealed record CommercialMobileOperationResult(
    Guid OperationId,
    Guid SessionId,
    long LocalSequence,
    CommercialMobileOperationStatus Status,
    CommercialReceivingCloudState? Cloud,
    CommercialReceivingOperationError? Error);

public sealed record CommercialReceivingOperationError(
    string Code,
    string Message,
    bool Retryable,
    bool RequiresAction);

public sealed record CommercialReceivingCloudState(
    string CloudReference,
    string SessionStatus,
    long SessionVersion,
    long OwnershipGeneration,
    Guid? LeaseId,
    DateTimeOffset? LeaseExpiresAtUtc,
    int EntryCount,
    string ProcessedTotalWeightKg);

public sealed record CommercialReceivingLeaseCommand(
    Guid SessionId,
    long OwnershipGeneration,
    Guid? LeaseId);

public sealed record CommercialReceivingTransferCommand(
    Guid SessionId,
    Guid TargetDeviceId,
    long ExpectedSessionVersion,
    long ExpectedOwnershipGeneration,
    string Reason);

public sealed record CommercialReceivingLeaseResult(
    Guid SessionId,
    long SessionVersion,
    Guid EditorDeviceId,
    long OwnershipGeneration,
    Guid? LeaseId,
    DateTimeOffset? LeaseExpiresAtUtc,
    long OwnershipVersion);

public sealed record CommercialReceivingReferencePolicyResult(
    Guid Id,
    string DocumentType,
    string FormatTemplate,
    string ResetPolicy,
    long StartingNumber,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateCommercialReceivingReferencePolicyCommand(
    string FormatTemplate,
    string ResetPolicy,
    long StartingNumber,
    long ExpectedVersion);

public sealed record CommercialReceivingListQuery(
    string? Status,
    string? Search,
    string? Cursor,
    int Limit = 50);

public sealed record CommercialReceivingListResult(
    IReadOnlyList<CommercialReceivingListItem> Items,
    string? NextCursor,
    bool HasMore);

public sealed record CommercialReceivingListItem(
    Guid SessionId,
    string CloudReference,
    string? ExternalReference,
    string Status,
    string SupplierCode,
    string SupplierName,
    int EntryCount,
    string ProcessedTotalWeightKg,
    Guid EditorDeviceId,
    long OwnershipGeneration,
    DateTimeOffset? LeaseExpiresAtUtc,
    string LeaseHealth,
    string? Attention,
    DateTimeOffset LastCloudUpdateUtc,
    long Version);

public sealed record CommercialReceivingLiveView(
    Guid SessionId,
    string CloudReference,
    string? ExternalReference,
    string Status,
    long Version,
    CommercialReceivingSnapshot Supplier,
    CommercialReceivingSnapshot Destination,
    CommercialReceivingPolicySnapshot WeightPolicy,
    CommercialReceivingVehicleSnapshot? Vehicle,
    int EntryCount,
    string ProcessedTotalWeightKg,
    Guid EditorDeviceId,
    long OwnershipGeneration,
    DateTimeOffset? LeaseExpiresAtUtc,
    string LeaseHealth,
    DateTimeOffset LastCloudUpdateUtc,
    DateTimeOffset? SubmittedAtUtc,
    IReadOnlyList<CommercialReceivingRecentEntry> RecentEntries,
    string? Attention);

public sealed record CommercialReceivingSnapshot(
    Guid Id,
    long Version,
    string Code,
    string Name);

public sealed record CommercialReceivingPolicySnapshot(
    Guid Id,
    long Version,
    int DecimalPlaces,
    string ProcessingMethod);

public sealed record CommercialReceivingVehicleSnapshot(
    Guid Id,
    long Version,
    string Code,
    string Registration,
    string? DisplayName);

public sealed record CommercialReceivingRecentEntry(
    Guid EntryId,
    long LocalSequence,
    string ProductCode,
    string ProductName,
    string BagTypeCode,
    int BagCount,
    string RawWeightKg,
    string ProcessedWeightKg,
    string DisplayWeightKg,
    DateTimeOffset CapturedAtDeviceUtc,
    DateTimeOffset AcceptedAtServerUtc);

public sealed record CommercialEventCursorQuery(
    string? Cursor,
    int Limit = 50);

public sealed record CommercialEventCursorResult(
    IReadOnlyList<CommercialEventCursorItem> Events,
    string NextCursor,
    bool HasMore);

public sealed record CommercialEventCursorItem(
    long Sequence,
    Guid EventId,
    string EventType,
    int EventVersion,
    Guid AggregateId,
    long AggregateVersion,
    string Audience,
    Guid? TargetDeviceId,
    DateTimeOffset OccurredAtUtc,
    string CorrelationId,
    JsonElement Payload);

public sealed record CommercialMasterCursorQuery(
    string? Cursor,
    int Limit = 50);

public sealed record CommercialMasterCursorResult(
    IReadOnlyList<CommercialMasterCursorItem> Changes,
    string NextCursor,
    long BootstrapHighWaterSequence,
    bool HasMore);

public sealed record CommercialMasterCursorItem(
    long Sequence,
    string MasterType,
    Guid MasterId,
    long MasterVersion,
    string Status,
    DateTimeOffset OccurredAtUtc,
    JsonElement Payload);

public interface ICommercialReceivingService
{
    Task<CommercialMobileOperationsResult> ProcessOperationsAsync(
        CommercialMobileOperationsCommand command,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> HeartbeatAsync(
        CommercialReceivingLeaseCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> ReacquireAsync(
        CommercialReceivingLeaseCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CommercialReceivingLeaseResult>> TransferAsync(
        CommercialReceivingTransferCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<CommercialReceivingReferencePolicyResult> GetReferencePolicyAsync(
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<CommercialReceivingReferencePolicyResult>>
        UpdateReferencePolicyAsync(
            UpdateCommercialReceivingReferencePolicyCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<CommercialReceivingListResult> ListAsync(
        CommercialReceivingListQuery query,
        CancellationToken cancellationToken);

    Task<CommercialReceivingLiveView> GetLiveViewAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<CommercialEventCursorResult> ReadEventsAsync(
        CommercialEventCursorQuery query,
        CancellationToken cancellationToken);

    Task<CommercialMasterCursorResult> ReadMastersAsync(
        CommercialMasterCursorQuery query,
        CancellationToken cancellationToken);
}
