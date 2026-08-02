using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Suppliers;

namespace TraderPro.Domain.Procurement.Receiving;

public enum CommercialReceivingStatus : short
{
    ReceivingInProgress = 1,
    SubmittedForSettlementReview = 2,
}

public sealed record CommercialReceivingSessionStartFacts(
    Guid Id,
    Guid WorkspaceId,
    Guid CompanyId,
    Guid BranchId,
    string CloudReference,
    long CloudReferenceSequence,
    Guid ReferenceReservationId,
    long ReferencePolicyVersionSnapshot,
    string? ExternalReference,
    Guid SupplierId,
    long SupplierVersionSnapshot,
    string SupplierCodeSnapshot,
    string SupplierNameSnapshot,
    SupplierProductScopeMode SupplierProductScopeModeSnapshot,
    Guid CompanyProcurementSettingsId,
    long ProcurementSettingsVersionSnapshot,
    VehicleSelectionMode VehicleSelectionModeSnapshot,
    Guid DestinationLocationId,
    long DestinationLocationVersionSnapshot,
    string DestinationLocationCodeSnapshot,
    string DestinationLocationNameSnapshot,
    Guid WeightProcessingPolicyId,
    long WeightPolicyVersionSnapshot,
    int WeightDecimalPlacesSnapshot,
    WeightProcessingMethod WeightProcessingMethodSnapshot,
    Guid? ReceivingVehicleId,
    long? ReceivingVehicleVersionSnapshot,
    string? VehicleCodeSnapshot,
    string? VehicleRegistrationSnapshot,
    string? VehicleDisplayNameSnapshot,
    DateTimeOffset StartedAtDeviceUtc,
    DateTimeOffset StartedAtServerUtc);

public sealed class CommercialReceivingSession :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    public const decimal MaximumTotalWeightKg = 99999999999999.999999m;
    public const int MaximumExternalReferenceLength = 100;

    private CommercialReceivingSession()
    {
    }

    private CommercialReceivingSession(CommercialReceivingSessionStartFacts facts)
    {
        RequireUuid7(facts.Id, nameof(facts.Id));
        Id = facts.Id;
        WorkspaceId = RequiredId(facts.WorkspaceId, nameof(facts.WorkspaceId));
        CompanyId = RequiredId(facts.CompanyId, nameof(facts.CompanyId));
        BranchId = RequiredId(facts.BranchId, nameof(facts.BranchId));
        CloudReference = Required(facts.CloudReference, 100, nameof(facts.CloudReference));
        CloudReferenceSequence = Positive(facts.CloudReferenceSequence, nameof(facts.CloudReferenceSequence));
        ReferenceReservationId = RequiredId(facts.ReferenceReservationId, nameof(facts.ReferenceReservationId));
        ReferencePolicyVersionSnapshot = Positive(facts.ReferencePolicyVersionSnapshot, nameof(facts.ReferencePolicyVersionSnapshot));
        ExternalReference = Optional(facts.ExternalReference, MaximumExternalReferenceLength, nameof(facts.ExternalReference));
        SupplierId = RequiredId(facts.SupplierId, nameof(facts.SupplierId));
        SupplierVersionSnapshot = Positive(facts.SupplierVersionSnapshot, nameof(facts.SupplierVersionSnapshot));
        SupplierCodeSnapshot = Required(facts.SupplierCodeSnapshot, 32, nameof(facts.SupplierCodeSnapshot));
        SupplierNameSnapshot = Required(facts.SupplierNameSnapshot, 200, nameof(facts.SupplierNameSnapshot));
        SupplierProductScopeModeSnapshot = Defined(facts.SupplierProductScopeModeSnapshot, nameof(facts.SupplierProductScopeModeSnapshot));
        CompanyProcurementSettingsId = RequiredId(facts.CompanyProcurementSettingsId, nameof(facts.CompanyProcurementSettingsId));
        ProcurementSettingsVersionSnapshot = Positive(facts.ProcurementSettingsVersionSnapshot, nameof(facts.ProcurementSettingsVersionSnapshot));
        VehicleSelectionModeSnapshot = Defined(facts.VehicleSelectionModeSnapshot, nameof(facts.VehicleSelectionModeSnapshot));
        DestinationLocationId = RequiredId(facts.DestinationLocationId, nameof(facts.DestinationLocationId));
        DestinationLocationVersionSnapshot = Positive(facts.DestinationLocationVersionSnapshot, nameof(facts.DestinationLocationVersionSnapshot));
        DestinationLocationCodeSnapshot = Required(facts.DestinationLocationCodeSnapshot, 32, nameof(facts.DestinationLocationCodeSnapshot));
        DestinationLocationNameSnapshot = Required(facts.DestinationLocationNameSnapshot, 200, nameof(facts.DestinationLocationNameSnapshot));
        WeightProcessingPolicyId = RequiredId(facts.WeightProcessingPolicyId, nameof(facts.WeightProcessingPolicyId));
        WeightPolicyVersionSnapshot = Positive(facts.WeightPolicyVersionSnapshot, nameof(facts.WeightPolicyVersionSnapshot));
        if (facts.WeightDecimalPlacesSnapshot is < 1 or > 3 || !Enum.IsDefined(facts.WeightProcessingMethodSnapshot))
        {
            throw new CommercialReceivingDomainException("RECEIVING_DEFAULT_POLICY_MISMATCH", "The captured weight policy is invalid.");
        }

        WeightDecimalPlacesSnapshot = facts.WeightDecimalPlacesSnapshot;
        WeightProcessingMethodSnapshot = facts.WeightProcessingMethodSnapshot;
        SetVehicleSnapshots(facts);
        Status = CommercialReceivingStatus.ReceivingInProgress;
        OwnershipGeneration = 1;
        NextExpectedLocalSequence = 2;
        EntryCount = 0;
        ProcessedTotalWeightKg = 0m;
        StartedAtDeviceUtc = Utc(facts.StartedAtDeviceUtc, nameof(facts.StartedAtDeviceUtc));
        StartedAtServerUtc = Utc(facts.StartedAtServerUtc, nameof(facts.StartedAtServerUtc));
        CreatedAtUtc = StartedAtServerUtc;
        UpdatedAtUtc = StartedAtServerUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string CloudReference { get; private set; } = string.Empty;
    public long CloudReferenceSequence { get; private set; }
    public Guid ReferenceReservationId { get; private set; }
    public long ReferencePolicyVersionSnapshot { get; private set; }
    public string? ExternalReference { get; private set; }
    public CommercialReceivingStatus Status { get; private set; }
    public long OwnershipGeneration { get; private set; }
    public Guid SupplierId { get; private set; }
    public long SupplierVersionSnapshot { get; private set; }
    public string SupplierCodeSnapshot { get; private set; } = string.Empty;
    public string SupplierNameSnapshot { get; private set; } = string.Empty;
    public SupplierProductScopeMode SupplierProductScopeModeSnapshot { get; private set; }
    public Guid CompanyProcurementSettingsId { get; private set; }
    public long ProcurementSettingsVersionSnapshot { get; private set; }
    public VehicleSelectionMode VehicleSelectionModeSnapshot { get; private set; }
    public Guid DestinationLocationId { get; private set; }
    public long DestinationLocationVersionSnapshot { get; private set; }
    public string DestinationLocationCodeSnapshot { get; private set; } = string.Empty;
    public string DestinationLocationNameSnapshot { get; private set; } = string.Empty;
    public Guid WeightProcessingPolicyId { get; private set; }
    public long WeightPolicyVersionSnapshot { get; private set; }
    public int WeightDecimalPlacesSnapshot { get; private set; }
    public WeightProcessingMethod WeightProcessingMethodSnapshot { get; private set; }
    public Guid? ReceivingVehicleId { get; private set; }
    public long? ReceivingVehicleVersionSnapshot { get; private set; }
    public string? VehicleCodeSnapshot { get; private set; }
    public string? VehicleRegistrationSnapshot { get; private set; }
    public string? VehicleDisplayNameSnapshot { get; private set; }
    public long NextExpectedLocalSequence { get; private set; }
    public int EntryCount { get; private set; }
    public decimal ProcessedTotalWeightKg { get; private set; }
    public DateTimeOffset StartedAtDeviceUtc { get; private set; }
    public DateTimeOffset StartedAtServerUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static CommercialReceivingSession Start(CommercialReceivingSessionStartFacts facts) => new(facts);

    public void AcceptEntry(CommercialReceivingEntry entry, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(entry);
        RequireInProgress();
        RequireSequence(entry.LocalSequence);
        if (entry.WorkspaceId != WorkspaceId || entry.CompanyId != CompanyId || entry.ReceivingSessionId != Id)
        {
            throw new CommercialReceivingDomainException("RECEIVING_SESSION_NOT_FOUND", "The Receiving Session was not found.");
        }

        var total = ProcessedTotalWeightKg + entry.ProcessedWeightKg;
        if (total > MaximumTotalWeightKg)
        {
            throw new CommercialReceivingDomainException("RECEIVING_TOTAL_WEIGHT_EXCEEDED", "The Receiving Session total weight exceeds storage capacity.");
        }

        ProcessedTotalWeightKg = total;
        EntryCount = checked(EntryCount + 1);
        NextExpectedLocalSequence = checked(NextExpectedLocalSequence + 1);
        Advance(now);
    }

    public void Submit(long localSequence, DateTimeOffset now)
    {
        RequireInProgress();
        RequireSequence(localSequence);
        if (EntryCount <= 0)
        {
            throw new CommercialReceivingDomainException("RECEIVING_ENTRY_REQUIRED", "At least one accepted Entry is required.");
        }

        if (ProcessedTotalWeightKg <= 0m)
        {
            throw new CommercialReceivingDomainException("RECEIVING_TOTAL_WEIGHT_REQUIRED", "A positive processed total is required.");
        }

        Status = CommercialReceivingStatus.SubmittedForSettlementReview;
        SubmittedAtUtc = Utc(now, nameof(now));
        NextExpectedLocalSequence = checked(NextExpectedLocalSequence + 1);
        Advance(now);
    }

    public void RecordOwnershipTransfer(
        long expectedVersion,
        long expectedOwnershipGeneration,
        DateTimeOffset now)
    {
        RequireInProgress();
        if (expectedVersion != Version)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_TRANSFER_CONFLICT", "The Receiving Session changed before ownership transfer.");
        }

        if (expectedOwnershipGeneration != OwnershipGeneration)
        {
            throw new CommercialReceivingDomainException("RECEIVING_OWNERSHIP_TRANSFER_CONFLICT", "The ownership generation changed before transfer.");
        }

        OwnershipGeneration = checked(OwnershipGeneration + 1);
        Advance(now);
    }

    public void RequireSequence(long localSequence)
    {
        if (localSequence < NextExpectedLocalSequence)
        {
            throw new CommercialReceivingDomainException("RECEIVING_SEQUENCE_CONFLICT", "The local sequence was already passed.");
        }

        if (localSequence > NextExpectedLocalSequence)
        {
            throw new CommercialReceivingDomainException("RECEIVING_SEQUENCE_GAP", "The next local sequence is missing.");
        }
    }

    public void RequireInProgress()
    {
        if (Status is not CommercialReceivingStatus.ReceivingInProgress)
        {
            throw new CommercialReceivingDomainException("RECEIVING_STATUS_INVALID", "The Receiving Session is not in progress.");
        }
    }

    private void Advance(DateTimeOffset now)
    {
        UpdatedAtUtc = Utc(now, nameof(now));
        Version = checked(Version + 1);
    }

    private void SetVehicleSnapshots(CommercialReceivingSessionStartFacts facts)
    {
        if (facts.ReceivingVehicleId is null)
        {
            if (facts.ReceivingVehicleVersionSnapshot is not null || facts.VehicleCodeSnapshot is not null || facts.VehicleRegistrationSnapshot is not null || facts.VehicleDisplayNameSnapshot is not null)
            {
                throw new CommercialReceivingDomainException("RECEIVING_VEHICLE_INACTIVE", "Vehicle snapshot fields are inconsistent.");
            }

            return;
        }

        ReceivingVehicleId = RequiredId(facts.ReceivingVehicleId.Value, nameof(facts.ReceivingVehicleId));
        ReceivingVehicleVersionSnapshot = Positive(facts.ReceivingVehicleVersionSnapshot ?? 0, nameof(facts.ReceivingVehicleVersionSnapshot));
        VehicleCodeSnapshot = Required(facts.VehicleCodeSnapshot, 32, nameof(facts.VehicleCodeSnapshot));
        VehicleRegistrationSnapshot = Required(facts.VehicleRegistrationSnapshot, 50, nameof(facts.VehicleRegistrationSnapshot));
        VehicleDisplayNameSnapshot = Optional(facts.VehicleDisplayNameSnapshot, 200, nameof(facts.VehicleDisplayNameSnapshot));
    }

    internal static void RequireUuid7(Guid value, string name)
    {
        if (value == Guid.Empty || value.Version != 7)
        {
            throw new CommercialReceivingDomainException("RECEIVING_SESSION_ID_INVALID", $"{name} must be a canonical UUIDv7.");
        }
    }

    internal static Guid RequiredId(Guid value, string name) => value != Guid.Empty ? value : throw new ArgumentException("A non-empty identifier is required.", name);
    internal static long Positive(long value, string name) => value > 0 ? value : throw new ArgumentOutOfRangeException(name);
    internal static T Defined<T>(T value, string name) where T : struct, Enum => Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(name);
    internal static DateTimeOffset Utc(DateTimeOffset value, string name) => value.Offset == TimeSpan.Zero ? value : throw new ArgumentException("The timestamp must be UTC.", name);
    internal static string Required(string? value, int max, string name) => !string.IsNullOrWhiteSpace(value) && value == value.Trim() && value.Length <= max && !value.Any(char.IsControl) ? value : throw new ArgumentException("A trimmed control-free value is required.", name);
    internal static string? Optional(string? value, int max, string name) => string.IsNullOrWhiteSpace(value) ? null : Required(value, max, name);
}
