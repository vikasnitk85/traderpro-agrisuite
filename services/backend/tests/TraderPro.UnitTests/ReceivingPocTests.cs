using TraderPro.Application.Procurement.Poc;
using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Procurement.Poc;

namespace TraderPro.UnitTests;

public sealed class ReceivingPocTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 29, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Client_uuid_v7_starts_leased_session_with_sequence_two()
    {
        var sessionId = Uuid7.NewGuid();
        var editor = Uuid7.NewGuid();
        var lease = Uuid7.NewGuid();

        var session = ReceivingSessionPoc.Start(
            sessionId,
            Uuid7.NewGuid(),
            editor,
            lease,
            UtcNow.AddMinutes(5),
            UtcNow);

        Assert.Equal(sessionId, session.Id);
        Assert.Equal(ReceivingPocStatus.ReceivingInProgress, session.Status);
        Assert.Equal(editor, session.EditorDeviceId);
        Assert.Equal(lease, session.LeaseId);
        Assert.Equal(2, session.NextExpectedLocalSequence);
        Assert.Equal(0, session.EntryCount);
        Assert.Equal(0.000000m, session.ProcessedTotalWeightKg);
        Assert.Equal(1, session.Version);
    }

    [Fact]
    public void Sequence_validation_distinguishes_conflict_and_gap()
    {
        var session = Start();

        var conflict = Assert.Throws<ReceivingPocDomainException>(
            () => session.EnsureNextSequence(1));
        var gap = Assert.Throws<ReceivingPocDomainException>(
            () => session.EnsureNextSequence(3));

        Assert.Equal("RECEIVING_POC_SEQUENCE_CONFLICT", conflict.Code);
        Assert.Equal("RECEIVING_POC_SEQUENCE_GAP", gap.Code);
    }

    [Fact]
    public void Lease_enforces_editor_identity_expiry_and_renewal()
    {
        var editor = Uuid7.NewGuid();
        var lease = Uuid7.NewGuid();
        var session = Start(editor, lease);

        var mismatch = Assert.Throws<ReceivingPocDomainException>(
            () => session.RecordEntry(
                Uuid7.NewGuid(),
                lease,
                2,
                1.000000m,
                UtcNow));
        var expired = Assert.Throws<ReceivingPocDomainException>(
            () => session.RecordEntry(
                editor,
                lease,
                2,
                1.000000m,
                UtcNow.AddMinutes(5)));
        session.RenewLease(
            editor,
            lease,
            UtcNow.AddMinutes(4),
            UtcNow.AddMinutes(9));
        session.RecordEntry(
            editor,
            lease,
            2,
            1.000000m,
            UtcNow.AddMinutes(6));

        Assert.Equal("RECEIVING_POC_EDITOR_DEVICE_MISMATCH", mismatch.Code);
        Assert.Equal("RECEIVING_POC_LEASE_EXPIRED", expired.Code);
        Assert.Equal(UtcNow.AddMinutes(9), session.LeaseExpiresAtUtc);
        Assert.Equal(1, session.EntryCount);
    }

    [Fact]
    public void Exact_decimal_total_and_status_transitions_are_controlled()
    {
        var editor = Uuid7.NewGuid();
        var owner = Uuid7.NewGuid();
        var lease = Uuid7.NewGuid();
        var session = Start(editor, lease);

        session.RecordEntry(editor, lease, 2, 50.230000m, UtcNow);
        session.RecordEntry(editor, lease, 3, 0.100000m, UtcNow);
        session.Submit(editor, lease, 4, UtcNow);

        Assert.Equal(50.330000m, session.ProcessedTotalWeightKg);
        Assert.Equal(ReceivingPocStatus.SubmittedForReview, session.Status);
        Assert.Null(session.LeaseId);
        Assert.Null(session.LeaseExpiresAtUtc);
        var separation = Assert.Throws<ReceivingPocDomainException>(
            () => session.Approve(editor, UtcNow));
        Assert.Equal("RECEIVING_POC_OWNER_DEVICE_REQUIRED", separation.Code);

        session.Approve(owner, UtcNow);
        session.Finalize(owner, UtcNow);

        Assert.Equal(ReceivingPocStatus.Finalized, session.Status);
        Assert.Equal(owner, session.ApprovedByDeviceId);
    }

    [Fact]
    public void Entry_preserves_raw_weight_and_captured_processing_result()
    {
        var processed = WeightProcessor.Process("00050.237000", 2, "Floor");
        var entry = ReceivingEntryPoc.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            2,
            "H Aman",
            "Plastic",
            1,
            processed,
            "TestScale",
            UtcNow,
            UtcNow);

        Assert.Equal("00050.237000", entry.RawWeightKg);
        Assert.Equal(50.230000m, entry.ProcessedWeightKg);
        Assert.Equal(50.23m, entry.DisplayWeightKg);
        Assert.Equal("Floor", entry.ProcessingMethod);
    }

    [Fact]
    public void Canonical_hash_covers_order_version_lease_and_weight_facts()
    {
        var deviceId = Uuid7.NewGuid();
        var sessionId = Uuid7.NewGuid();
        var leaseId = Uuid7.NewGuid();
        var baseline = ProcurementPocRequestHash.ForRecordEntry(
            deviceId,
            ProcurementPocOperationTypes.RecordReceivingEntry,
            sessionId,
            2,
            null,
            leaseId,
            "H Aman",
            "Plastic",
            1,
            "50.237",
            "50.230000",
            "50.23",
            2,
            "Floor",
            "TestScale",
            UtcNow);

        Assert.Equal(64, baseline.Length);
        Assert.Matches("^[0-9a-f]{64}$", baseline);
        Assert.NotEqual(
            baseline,
            ProcurementPocRequestHash.ForRecordEntry(
                deviceId,
                ProcurementPocOperationTypes.RecordReceivingEntry,
                sessionId,
                3,
                null,
                leaseId,
                "H Aman",
                "Plastic",
                1,
                "50.237",
                "50.230000",
                "50.23",
                2,
                "Floor",
                "TestScale",
                UtcNow));
        Assert.NotEqual(
            baseline,
            ProcurementPocRequestHash.ForRecordEntry(
                deviceId,
                ProcurementPocOperationTypes.RecordReceivingEntry,
                sessionId,
                2,
                1,
                leaseId,
                "H Aman",
                "Plastic",
                1,
                "50.237",
                "50.230000",
                "50.23",
                2,
                "Floor",
                "TestScale",
                UtcNow));
        Assert.NotEqual(
            baseline,
            ProcurementPocRequestHash.ForRecordEntry(
                Uuid7.NewGuid(),
                ProcurementPocOperationTypes.RecordReceivingEntry,
                sessionId,
                2,
                null,
                leaseId,
                "H Aman",
                "Plastic",
                1,
                "50.237",
                "50.230000",
                "50.23",
                2,
                "Floor",
                "TestScale",
                UtcNow));
        Assert.NotEqual(
            baseline,
            ProcurementPocRequestHash.ForRecordEntry(
                deviceId,
                ProcurementPocOperationTypes.SubmitReceivingSession,
                sessionId,
                2,
                null,
                leaseId,
                "H Aman",
                "Plastic",
                1,
                "50.237",
                "50.230000",
                "50.23",
                2,
                "Floor",
                "TestScale",
                UtcNow));
    }

    [Fact]
    public void Total_weight_capacity_returns_stable_domain_error_without_change()
    {
        var editor = Uuid7.NewGuid();
        var lease = Uuid7.NewGuid();
        var session = Start(editor, lease);
        session.RecordEntry(
            editor,
            lease,
            2,
            99999999999999.000000m,
            UtcNow);

        var exception = Assert.Throws<ReceivingPocDomainException>(
            () => session.RecordEntry(
                editor,
                lease,
                3,
                1.000000m,
                UtcNow));

        Assert.Equal("RECEIVING_POC_TOTAL_WEIGHT_EXCEEDED", exception.Code);
        Assert.Equal(1, session.EntryCount);
        Assert.Equal(3, session.NextExpectedLocalSequence);
        Assert.Equal(
            99999999999999.000000m,
            session.ProcessedTotalWeightKg);
    }

    [Fact]
    public void Finalization_copies_approved_snapshot_and_starts_at_version_one()
    {
        var finalization = ReceivingFinalizationPoc.Create(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            5,
            251.150000m,
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            UtcNow);

        Assert.Equal(5, finalization.FinalEntryCount);
        Assert.Equal(251.150000m, finalization.FinalProcessedTotalWeightKg);
        Assert.Equal(1, finalization.Version);
        Assert.Equal(7, finalization.Id.Version);
    }

    private static ReceivingSessionPoc Start(
        Guid? editor = null,
        Guid? lease = null)
    {
        return ReceivingSessionPoc.Start(
            Uuid7.NewGuid(),
            Uuid7.NewGuid(),
            editor ?? Uuid7.NewGuid(),
            lease ?? Uuid7.NewGuid(),
            UtcNow.AddMinutes(5),
            UtcNow);
    }
}
