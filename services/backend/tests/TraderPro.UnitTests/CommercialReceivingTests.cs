using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Receiving;
using TraderPro.Domain.Procurement.Suppliers;

namespace TraderPro.UnitTests;

public sealed class CommercialReceivingTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reference_policy_renders_supported_periods_and_sequence()
    {
        var policy = CommercialReceivingReferencePolicy.Create(
            Uuid7.NewGuid(), Uuid7.NewGuid(),
            "RCV-{YYYY}-{MM}-{SEQ:0000}",
            CommercialReceivingReferenceResetPolicy.Monthly,
            7, Now);

        Assert.Equal("2026-08", policy.PeriodKey(Now));
        Assert.Equal("RCV-2026-08-0007", policy.Render(7, Now));
        Assert.Throws<CommercialReceivingDomainException>(() =>
            CommercialReceivingReferencePolicy.Create(
                Uuid7.NewGuid(), Uuid7.NewGuid(), "RCV-{SEQ:0000}",
                CommercialReceivingReferenceResetPolicy.Monthly, 1, Now));
    }

    [Fact]
    public void Reference_starting_number_freezes_after_issue()
    {
        var policy = CommercialReceivingReferencePolicy.CreateDefault(
            Uuid7.NewGuid(), Uuid7.NewGuid(), Now);

        var exception = Assert.Throws<CommercialReceivingDomainException>(() =>
            policy.Update(policy.FormatTemplate, policy.ResetPolicy, 2, true,
                Now.AddMinutes(1)));

        Assert.Equal("RECEIVING_REFERENCE_SERIES_STARTED", exception.Code);
    }

    [Fact]
    public void Session_accepts_positive_entry_and_submits_once()
    {
        var session = Session();
        var entry = Entry(session, 2, "10.125", "10.130000", "10.13");

        session.AcceptEntry(entry, Now.AddMinutes(1));
        session.Submit(3, Now.AddMinutes(2));

        Assert.Equal(1, session.EntryCount);
        Assert.Equal(10.13m, session.ProcessedTotalWeightKg);
        Assert.Equal(CommercialReceivingStatus.SubmittedForSettlementReview,
            session.Status);
        Assert.Throws<CommercialReceivingDomainException>(() =>
            session.AcceptEntry(Entry(session, 4), Now.AddMinutes(3)));
    }

    [Fact]
    public void Session_rejects_sequence_gap_and_zero_entry_submission()
    {
        var session = Session();

        var gap = Assert.Throws<CommercialReceivingDomainException>(() =>
            session.AcceptEntry(Entry(session, 3), Now.AddMinutes(1)));
        var empty = Assert.Throws<CommercialReceivingDomainException>(() =>
            session.Submit(2, Now.AddMinutes(2)));

        Assert.Equal("RECEIVING_SEQUENCE_GAP", gap.Code);
        Assert.Equal("RECEIVING_ENTRY_REQUIRED", empty.Code);
    }

    [Fact]
    public void Ownership_reacquisition_preserves_generation_and_rotates_lease()
    {
        var workspace = Uuid7.NewGuid();
        var company = Uuid7.NewGuid();
        var sessionId = Uuid7.NewGuid();
        var device = Uuid7.NewGuid();
        var ownership = CommercialReceivingOwnership.Start(
            workspace, company, sessionId, device, Now, TimeSpan.FromMinutes(60));
        var oldLease = ownership.LeaseId;

        ownership.Reacquire(device, 1, Now.AddMinutes(61),
            TimeSpan.FromMinutes(60));

        Assert.Equal(1, ownership.OwnershipGeneration);
        Assert.NotEqual(oldLease, ownership.LeaseId);
        Assert.Equal(Now.AddMinutes(121), ownership.LeaseExpiresAtUtc);
    }

    [Fact]
    public void Ownership_transfer_advances_generation_once_and_stales_old_owner()
    {
        var oldDevice = Uuid7.NewGuid();
        var ownership = CommercialReceivingOwnership.Start(
            Uuid7.NewGuid(), Uuid7.NewGuid(), Uuid7.NewGuid(), oldDevice,
            Now, TimeSpan.FromMinutes(60));
        var target = Uuid7.NewGuid();

        ownership.Transfer(target, Uuid7.NewGuid(), 1, Now.AddMinutes(5));

        Assert.Equal(2, ownership.OwnershipGeneration);
        Assert.Equal(target, ownership.EditorDeviceId);
        Assert.Null(ownership.LeaseId);
        Assert.Null(ownership.LeaseExpiresAtUtc);
        var stale = Assert.Throws<CommercialReceivingDomainException>(() =>
            ownership.RequireLease(oldDevice, 1, ownership.LeaseId,
                Now.AddMinutes(6)));
        Assert.Equal("RECEIVING_OWNERSHIP_DEVICE_MISMATCH", stale.Code);
    }

    [Fact]
    public void Mobile_hash_binds_type_and_generation_but_has_no_lease_input()
    {
        var ids = Enumerable.Range(0, 6).Select(_ => Uuid7.NewGuid()).ToArray();
        var payload = "{\"rawWeightKg\":\"10.125\"}";
        var payloadHash = CommercialReceivingRequestHash.PayloadHash(payload);

        var first = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.RecordEntry, ids[0], ids[1], 2,
            1, ids[2], ids[3], ids[4], ids[5], payloadHash, payload);
        var same = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.RecordEntry, ids[0], ids[1], 2,
            1, ids[2], ids[3], ids[4], ids[5], payloadHash, payload);
        var otherType = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.Submit, ids[0], ids[1], 2,
            1, ids[2], ids[3], ids[4], ids[5], payloadHash, payload);
        var otherGeneration = CommercialReceivingRequestHash.ForMobileOperation(
            CommercialReceivingOperationTypes.RecordEntry, ids[0], ids[1], 2,
            2, ids[2], ids[3], ids[4], ids[5], payloadHash, payload);

        Assert.Equal(first, same);
        Assert.NotEqual(first, otherType);
        Assert.NotEqual(first, otherGeneration);
    }

    private static CommercialReceivingSession Session()
    {
        return CommercialReceivingSession.Start(new(
            Uuid7.NewGuid(), Uuid7.NewGuid(), Uuid7.NewGuid(), Uuid7.NewGuid(),
            "RCV-000001", 1, Uuid7.NewGuid(), 1, null,
            Uuid7.NewGuid(), 1, "SUP-1", "Supplier", SupplierProductScopeMode.Unrestricted,
            Uuid7.NewGuid(), 1, VehicleSelectionMode.Optional,
            Uuid7.NewGuid(), 1, "DEST-1", "Destination",
            Uuid7.NewGuid(), 1, 2, WeightProcessingMethod.Standard,
            null, null, null, null, null, Now, Now));
    }

    private static CommercialReceivingEntry Entry(
        CommercialReceivingSession session,
        long sequence,
        string raw = "1.000",
        string processed = "1.000000",
        string display = "1.00")
    {
        return CommercialReceivingEntry.Create(new(
            Uuid7.NewGuid(), session.WorkspaceId, session.CompanyId, session.Id,
            Uuid7.NewGuid(), sequence,
            Uuid7.NewGuid(), 1, "PROD-1", "Product", ProductType.RawMaterial,
            null, null, null, SupplierProductScopeMode.Unrestricted,
            "Unrestricted", Uuid7.NewGuid(), 1, "BAG-1", "Bag",
            BagConstructionClass.Jute, 0.5m, true,
            null, null, null, null, 1, raw,
            decimal.Parse(processed, System.Globalization.CultureInfo.InvariantCulture),
            display, 2, WeightProcessingMethod.Standard, "Manual", Now, Now));
    }
}
