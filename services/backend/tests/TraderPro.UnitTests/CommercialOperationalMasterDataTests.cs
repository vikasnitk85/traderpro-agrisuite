using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform.Identity;
using TraderPro.Domain.Procurement.MasterData;

namespace TraderPro.UnitTests;

public sealed class CommercialOperationalMasterDataTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 31, 7, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("yard-01", "YARD-01")]
    [InlineData("AB", "AB")]
    [InlineData("a1-b2", "A1-B2")]
    public void Master_codes_normalize_invariantly(
        string input,
        string expected)
    {
        Assert.Equal(expected, MasterDataValueRules.NormalizeCode(input));
    }

    [Theory]
    [InlineData(" A1")]
    [InlineData("A1 ")]
    [InlineData("A")]
    [InlineData("A_1")]
    [InlineData("A/1")]
    public void Invalid_master_codes_are_rejected(string code)
    {
        var exception = Assert.Throws<MasterDataDomainException>(
            () => MasterDataValueRules.NormalizeCode(code));
        Assert.Equal("MASTER_CODE_INVALID", exception.Code);
    }

    [Fact]
    public void Location_updates_details_and_uses_controlled_status_transitions()
    {
        var location = BusinessLocation.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "yard-01",
            "North Yard",
            null,
            BusinessLocationType.Yard,
            null,
            null,
            UtcNow);

        location.Update(
            "Main Yard",
            "முதன்மை வளாகம்",
            BusinessLocationType.Warehouse,
            "Market Road",
            "Primary destination",
            UtcNow.AddMinutes(1));
        location.Deactivate(UtcNow.AddMinutes(2));

        Assert.Equal("YARD-01", location.Code);
        Assert.Equal("Main Yard", location.Name);
        Assert.Equal(BusinessLocationType.Warehouse, location.LocationType);
        Assert.Equal(MasterDataStatus.Inactive, location.Status);
        Assert.Equal(3, location.Version);
        Assert.Equal(UtcNow.AddMinutes(2), location.UpdatedAtUtc);
        location.Reactivate(UtcNow.AddMinutes(3));
        Assert.Equal(MasterDataStatus.Active, location.Status);
        Assert.Equal(4, location.Version);
        Assert.Equal(UtcNow.AddMinutes(3), location.UpdatedAtUtc);
        var exception = Assert.Throws<MasterDataDomainException>(
            () => location.Reactivate(UtcNow.AddMinutes(4)));
        Assert.Equal("MASTER_STATUS_INVALID", exception.Code);
        Assert.Equal(4, location.Version);
    }

    [Theory]
    [InlineData("KA 01 AB 1234", "KA01AB1234")]
    [InlineData("ka-01-ab-1234", "KA01AB1234")]
    public void Vehicle_registration_normalization_is_deterministic(
        string input,
        string expected)
    {
        Assert.Equal(
            expected,
            MasterDataValueRules.NormalizeRegistration(input));
    }

    [Fact]
    public void Vehicle_update_persists_one_revision_and_canonical_registration()
    {
        var vehicle = ReceivingVehicle.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "truck-01",
            "KA 01 AB 1234",
            null,
            ReceivingVehicleType.Truck,
            null,
            null,
            null,
            UtcNow);

        vehicle.Update(
            "TN-02-CD-5678",
            "Corrected",
            ReceivingVehicleType.Van,
            null,
            null,
            null,
            UtcNow.AddMinutes(1));

        Assert.Equal("TN-02-CD-5678", vehicle.RegistrationNumber);
        Assert.Equal("TN02CD5678", vehicle.NormalizedRegistrationNumber);
        Assert.Equal(2, vehicle.Version);
        Assert.Equal(UtcNow.AddMinutes(1), vehicle.UpdatedAtUtc);
    }

    [Fact]
    public void Bag_type_keeps_exact_tare_and_classifies_double_plastic()
    {
        var bagType = BagType.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "double-50",
            "Double plastic bag",
            null,
            BagConstructionClass.DoublePlastic,
            0.200000m,
            true,
            null,
            UtcNow);

        Assert.Equal(0.200000m, bagType.StandardTareWeightKg);
        Assert.Equal(
            BagConstructionClass.DoublePlastic,
            bagType.ConstructionClass);
        var exception = Assert.Throws<MasterDataDomainException>(
            () => bagType.Update(
                "Invalid",
                null,
                BagConstructionClass.Jute,
                -0.000001m,
                false,
                null,
                UtcNow.AddMinutes(1)));
        Assert.Equal("BAG_TYPE_TARE_WEIGHT_INVALID", exception.Code);
        Assert.Equal(1, bagType.Version);
        Assert.Equal(UtcNow, bagType.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(1, WeightProcessingMethod.Standard)]
    [InlineData(2, WeightProcessingMethod.Floor)]
    [InlineData(3, WeightProcessingMethod.Ceiling)]
    public void Weight_policy_accepts_only_task_three_values(
        int decimalPlaces,
        WeightProcessingMethod method)
    {
        var policy =
            TraderPro.Domain.Procurement.MasterData.WeightProcessingPolicy
                .Create(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "policy-01",
                    "Capture policy",
                    decimalPlaces,
                    method,
                    null,
                    UtcNow);
        var processed = WeightProcessor.Process(
            "50.237000",
            TraderPro.Domain.Common.Measurements.WeightProcessingPolicy.Create(
                policy.DecimalPlaces,
                policy.ProcessingMethod));

        Assert.Equal(decimalPlaces, processed.DecimalPlaces);
        Assert.Equal(method, processed.Method);
    }

    [Fact]
    public void Invalid_weight_policy_precision_is_stable()
    {
        var exception = Assert.Throws<MasterDataDomainException>(
            () => TraderPro.Domain.Procurement.MasterData
                .WeightProcessingPolicy.Create(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "policy-01",
                    "Invalid policy",
                    4,
                    WeightProcessingMethod.Standard,
                    null,
                    UtcNow));
        Assert.Equal(
            "WEIGHT_POLICY_DECIMAL_PLACES_INVALID",
            exception.Code);
    }

    [Fact]
    public void Weight_policy_mutations_advance_exactly_once()
    {
        var policy = TraderPro.Domain.Procurement.MasterData
            .WeightProcessingPolicy.Create(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "rounding-01",
                "Rounding",
                1,
                WeightProcessingMethod.Standard,
                null,
                UtcNow);

        policy.Update(
            "Floor capture",
            3,
            WeightProcessingMethod.Floor,
            null,
            UtcNow.AddMinutes(1));
        policy.Deactivate(UtcNow.AddMinutes(2));
        policy.Reactivate(UtcNow.AddMinutes(3));

        Assert.Equal(4, policy.Version);
        Assert.Equal(UtcNow.AddMinutes(3), policy.UpdatedAtUtc);
        Assert.Equal(WeightProcessingMethod.Floor, policy.ProcessingMethod);
    }

    [Fact]
    public void Canonical_optional_text_and_required_returnability_are_stable()
    {
        Assert.Null(
            CommercialMasterDataInputRules.CanonicalOptionalText("   "));
        Assert.Null(
            CommercialMasterDataInputRules.CanonicalOptionalText(null));
        Assert.Equal(
            "safe",
            CommercialMasterDataInputRules.CanonicalOptionalText(" safe "));
        Assert.False(
            CommercialMasterDataInputRules.RequireBagReturnability(false));
        Assert.True(
            CommercialMasterDataInputRules.RequireBagReturnability(true));
        var exception = Assert.Throws<ApplicationProblemException>(
            () => CommercialMasterDataInputRules
                .RequireBagReturnability(null));
        Assert.Equal("BAG_TYPE_INVALID", exception.Code);
    }

    [Fact]
    public void Procurement_settings_keep_ownership_and_default_branch()
    {
        var workspaceId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var policyId = Guid.CreateVersion7();
        var settings = CompanyProcurementSettings.Create(
            workspaceId,
            companyId,
            branchId,
            locationId,
            policyId,
            VehicleSelectionMode.Optional,
            UtcNow);
        var secondLocationId = Guid.CreateVersion7();

        settings.Update(
            secondLocationId,
            policyId,
            VehicleSelectionMode.Disabled,
            UtcNow.AddMinutes(1));

        Assert.Equal(workspaceId, settings.WorkspaceId);
        Assert.Equal(companyId, settings.CompanyId);
        Assert.Equal(branchId, settings.DefaultBranchId);
        Assert.Equal(
            secondLocationId,
            settings.DefaultDestinationLocationId);
        Assert.Equal(
            VehicleSelectionMode.Disabled,
            settings.VehicleSelectionMode);
        Assert.Equal(2, settings.Version);
        Assert.Equal(UtcNow.AddMinutes(1), settings.UpdatedAtUtc);
    }

    [Fact]
    public void Canonical_hash_includes_authority_and_expected_version()
    {
        var context = new TestContext();
        var first = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateBagType,
            context,
            ("id", Guid.Parse("0198b552-c4a0-7000-8000-000000000001")),
            ("expectedVersion", 1L),
            ("tare", 0.2m));
        var identical = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateBagType,
            context,
            ("id", Guid.Parse("0198b552-c4a0-7000-8000-000000000001")),
            ("expectedVersion", 1L),
            ("tare", 0.200000m));
        var changedVersion = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateBagType,
            context,
            ("id", Guid.Parse("0198b552-c4a0-7000-8000-000000000001")),
            ("expectedVersion", 2L),
            ("tare", 0.2m));
        var changedDevice = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateBagType,
            context with { DeviceId = Guid.CreateVersion7() },
            ("id", Guid.Parse("0198b552-c4a0-7000-8000-000000000001")),
            ("expectedVersion", 1L),
            ("tare", 0.2m));

        Assert.Equal(first, identical);
        Assert.NotEqual(first, changedVersion);
        Assert.NotEqual(first, changedDevice);
    }

    private sealed record TestContext : IAuthenticatedTraderProContext
    {
        public bool IsBound => true;

        public Guid WorkspaceId { get; init; } = Guid.CreateVersion7();

        public Guid UserId { get; init; } = Guid.CreateVersion7();

        public Guid DeviceId { get; init; } = Guid.CreateVersion7();

        public Guid CompanyId { get; init; } = Guid.CreateVersion7();

        public Guid DefaultBranchId { get; init; } = Guid.CreateVersion7();

        public TraderProRole Role => TraderProRole.Owner;

        public bool IsOwner => true;

        public bool IsOperatorOrOwner => true;

        public Guid TokenFamilyId { get; } = Guid.CreateVersion7();

        public string CorrelationId { get; } =
            Guid.CreateVersion7().ToString("D");
    }
}
