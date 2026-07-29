using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Poc;

/// <summary>
/// Development-only Receiving Session aggregate for Task 6A.
/// It is intentionally not a commercially complete Procurement record.
/// </summary>
public sealed class ReceivingSessionPoc :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private const decimal MaximumStoredWeight = 99999999999999.999999m;

    private ReceivingSessionPoc()
    {
    }

    private ReceivingSessionPoc(
        Guid id,
        Guid workspaceId,
        Guid editorDeviceId,
        Guid leaseId,
        DateTimeOffset leaseExpiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || id.Version != 7)
        {
            throw new ArgumentException(
                "The session ID must be a non-empty UUIDv7.",
                nameof(id));
        }

        Id = id;
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        Status = ReceivingPocStatus.ReceivingInProgress;
        EditorDeviceId = RequiredId(editorDeviceId, nameof(editorDeviceId));
        LeaseId = RequiredId(leaseId, nameof(leaseId));
        LeaseExpiresAtUtc = RequireUtc(
            leaseExpiresAtUtc,
            nameof(leaseExpiresAtUtc));
        LastLeaseHeartbeatAtUtc = RequireUtc(createdAtUtc, nameof(createdAtUtc));
        NextExpectedLocalSequence = 2;
        EntryCount = 0;
        ProcessedTotalWeightKg = 0.000000m;
        CreatedAtUtc = LastLeaseHeartbeatAtUtc.Value;
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public long CloudReferenceSequence { get; private set; }

    public string CloudReference =>
        $"RS-POC-{CloudReferenceSequence:000000}";

    public ReceivingPocStatus Status { get; private set; }

    public Guid EditorDeviceId { get; private set; }

    public Guid? LeaseId { get; private set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public DateTimeOffset? LastLeaseHeartbeatAtUtc { get; private set; }

    public long NextExpectedLocalSequence { get; private set; }

    public int EntryCount { get; private set; }

    public decimal ProcessedTotalWeightKg { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    public Guid? ApprovedByDeviceId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static ReceivingSessionPoc Start(
        Guid id,
        Guid workspaceId,
        Guid editorDeviceId,
        Guid leaseId,
        DateTimeOffset leaseExpiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        if (leaseExpiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseExpiresAtUtc),
                "The lease expiry must be after the server time.");
        }

        return new ReceivingSessionPoc(
            id,
            workspaceId,
            editorDeviceId,
            leaseId,
            leaseExpiresAtUtc,
            createdAtUtc);
    }

    public void RenewLease(
        Guid requestingDeviceId,
        Guid leaseId,
        DateTimeOffset renewedAtUtc,
        DateTimeOffset newExpiryUtc)
    {
        EnsureEditableLease(requestingDeviceId, leaseId, renewedAtUtc);
        if (newExpiryUtc <= renewedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(newExpiryUtc),
                "The renewed lease expiry must be after the server time.");
        }

        LastLeaseHeartbeatAtUtc = RequireUtc(
            renewedAtUtc,
            nameof(renewedAtUtc));
        LeaseExpiresAtUtc = RequireUtc(newExpiryUtc, nameof(newExpiryUtc));
    }

    public void RecordEntry(
        Guid requestingDeviceId,
        Guid leaseId,
        long localSequence,
        decimal processedWeightKg,
        DateTimeOffset serverNowUtc)
    {
        EnsureEditableLease(requestingDeviceId, leaseId, serverNowUtc);
        EnsureNextSequence(localSequence);
        if (processedWeightKg < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedWeightKg),
                "Processed weight cannot be negative.");
        }

        decimal nextTotal;
        try
        {
            nextTotal = checked(
                ProcessedTotalWeightKg + processedWeightKg);
        }
        catch (OverflowException)
        {
            throw TotalWeightExceeded();
        }

        if (nextTotal > MaximumStoredWeight)
        {
            throw TotalWeightExceeded();
        }

        ProcessedTotalWeightKg = nextTotal;
        EntryCount = checked(EntryCount + 1);
        NextExpectedLocalSequence = checked(NextExpectedLocalSequence + 1);
    }

    public void Submit(
        Guid requestingDeviceId,
        Guid leaseId,
        long localSequence,
        DateTimeOffset submittedAtUtc)
    {
        EnsureEditableLease(requestingDeviceId, leaseId, submittedAtUtc);
        EnsureNextSequence(localSequence);
        if (EntryCount == 0)
        {
            throw Problem(
                "RECEIVING_POC_ENTRY_REQUIRED",
                "At least one accepted entry is required before submission.");
        }

        Status = ReceivingPocStatus.SubmittedForReview;
        SubmittedAtUtc = RequireUtc(submittedAtUtc, nameof(submittedAtUtc));
        LeaseId = null;
        LeaseExpiresAtUtc = null;
        LastLeaseHeartbeatAtUtc = null;
        NextExpectedLocalSequence = checked(NextExpectedLocalSequence + 1);
    }

    public void Approve(
        Guid approvingDeviceId,
        DateTimeOffset approvedAtUtc)
    {
        EnsureStatus(ReceivingPocStatus.SubmittedForReview);
        if (RequiredId(approvingDeviceId, nameof(approvingDeviceId)) ==
            EditorDeviceId)
        {
            throw Problem(
                "RECEIVING_POC_OWNER_DEVICE_REQUIRED",
                "A different active device must approve this POC session.");
        }

        ApprovedByDeviceId = approvingDeviceId;
        ApprovedAtUtc = RequireUtc(approvedAtUtc, nameof(approvedAtUtc));
        Status = ReceivingPocStatus.Approved;
    }

    public void Finalize(
        Guid finalizingDeviceId,
        DateTimeOffset finalizedAtUtc)
    {
        EnsureStatus(ReceivingPocStatus.Approved);
        if (RequiredId(finalizingDeviceId, nameof(finalizingDeviceId)) ==
            EditorDeviceId)
        {
            throw Problem(
                "RECEIVING_POC_OWNER_DEVICE_REQUIRED",
                "A different active device must finalize this POC session.");
        }

        _ = RequireUtc(finalizedAtUtc, nameof(finalizedAtUtc));
        Status = ReceivingPocStatus.Finalized;
    }

    public void EnsureNextSequence(long localSequence)
    {
        if (localSequence < NextExpectedLocalSequence)
        {
            throw Problem(
                "RECEIVING_POC_SEQUENCE_CONFLICT",
                "The local sequence was already consumed.");
        }

        if (localSequence > NextExpectedLocalSequence)
        {
            throw Problem(
                "RECEIVING_POC_SEQUENCE_GAP",
                "The next ordered local operation is missing.");
        }
    }

    public void EnsureEditableLease(
        Guid requestingDeviceId,
        Guid leaseId,
        DateTimeOffset serverNowUtc)
    {
        EnsureStatus(ReceivingPocStatus.ReceivingInProgress);
        if (RequiredId(requestingDeviceId, nameof(requestingDeviceId)) !=
            EditorDeviceId)
        {
            throw Problem(
                "RECEIVING_POC_EDITOR_DEVICE_MISMATCH",
                "Only the cloud-assigned editor device can change this session.");
        }

        if (LeaseId is null)
        {
            throw Problem(
                "RECEIVING_POC_LEASE_REQUIRED",
                "An active editing lease is required.");
        }

        if (RequiredId(leaseId, nameof(leaseId)) != LeaseId.Value)
        {
            throw Problem(
                "RECEIVING_POC_LEASE_INVALID",
                "The editing lease does not match.");
        }

        var now = RequireUtc(serverNowUtc, nameof(serverNowUtc));
        if (LeaseExpiresAtUtc is null || LeaseExpiresAtUtc <= now)
        {
            throw Problem(
                "RECEIVING_POC_LEASE_EXPIRED",
                "The editing lease has expired.");
        }
    }

    private void EnsureStatus(ReceivingPocStatus required)
    {
        if (Status != required)
        {
            throw Problem(
                "RECEIVING_POC_STATUS_INVALID",
                $"The POC session must be {required} for this operation.");
        }
    }

    private static ReceivingPocDomainException Problem(
        string code,
        string message)
    {
        return new ReceivingPocDomainException(code, message);
    }

    private static ReceivingPocDomainException TotalWeightExceeded()
    {
        return Problem(
            "RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED",
            "The POC processed total exceeds numeric(20,6) capacity.");
    }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty identifier is required.",
                parameterName);
        }

        return value;
    }

    private static DateTimeOffset RequireUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must use a zero UTC offset.",
                parameterName);
        }

        return value;
    }
}
