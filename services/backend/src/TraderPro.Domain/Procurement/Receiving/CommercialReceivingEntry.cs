using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Suppliers;
using TraderPro.Domain.Catalog;

namespace TraderPro.Domain.Procurement.Receiving;

public sealed record CommercialReceivingEntryFacts(
    Guid Id,
    Guid WorkspaceId,
    Guid CompanyId,
    Guid ReceivingSessionId,
    Guid OperationId,
    long LocalSequence,
    Guid ProductId,
    long ProductVersionSnapshot,
    string ProductCodeSnapshot,
    string ProductNameSnapshot,
    ProductType ProductTypeSnapshot,
    string? ProcessingFamilyCodeSnapshot,
    Guid? SupplierProductScopeId,
    long? SupplierProductScopeVersionSnapshot,
    SupplierProductScopeMode SupplierScopeModeSnapshot,
    string SupplierScopeValidationResultSnapshot,
    Guid BagTypeId,
    long BagTypeVersionSnapshot,
    string BagTypeCodeSnapshot,
    string BagTypeNameSnapshot,
    BagConstructionClass BagConstructionClassSnapshot,
    decimal BagTareWeightKgSnapshot,
    bool BagReturnableSnapshot,
    Guid? ProductStandardBagWeightId,
    long? ProductStandardBagWeightVersionSnapshot,
    string? StandardBagWeightLabelSnapshot,
    decimal? StandardContentWeightKgSnapshot,
    int BagCount,
    string RawWeightKg,
    decimal ProcessedWeightKg,
    string DisplayWeightKg,
    int DecimalPlacesSnapshot,
    WeightProcessingMethod ProcessingMethodSnapshot,
    string WeightSource,
    DateTimeOffset CapturedAtDeviceUtc,
    DateTimeOffset AcceptedAtServerUtc);

public sealed class CommercialReceivingEntry : IWorkspaceScoped
{
    private CommercialReceivingEntry()
    {
    }

    private CommercialReceivingEntry(CommercialReceivingEntryFacts facts)
    {
        CommercialReceivingSession.RequireUuid7(facts.Id, nameof(facts.Id));
        CommercialReceivingSession.RequireUuid7(facts.OperationId, nameof(facts.OperationId));
        Id = facts.Id;
        WorkspaceId = CommercialReceivingSession.RequiredId(facts.WorkspaceId, nameof(facts.WorkspaceId));
        CompanyId = CommercialReceivingSession.RequiredId(facts.CompanyId, nameof(facts.CompanyId));
        ReceivingSessionId = CommercialReceivingSession.RequiredId(facts.ReceivingSessionId, nameof(facts.ReceivingSessionId));
        OperationId = facts.OperationId;
        LocalSequence = CommercialReceivingSession.Positive(facts.LocalSequence, nameof(facts.LocalSequence));
        ProductId = CommercialReceivingSession.RequiredId(facts.ProductId, nameof(facts.ProductId));
        ProductVersionSnapshot = CommercialReceivingSession.Positive(facts.ProductVersionSnapshot, nameof(facts.ProductVersionSnapshot));
        ProductCodeSnapshot = CommercialReceivingSession.Required(facts.ProductCodeSnapshot, 32, nameof(facts.ProductCodeSnapshot));
        ProductNameSnapshot = CommercialReceivingSession.Required(facts.ProductNameSnapshot, 200, nameof(facts.ProductNameSnapshot));
        ProductTypeSnapshot = CommercialReceivingSession.Defined(facts.ProductTypeSnapshot, nameof(facts.ProductTypeSnapshot));
        ProcessingFamilyCodeSnapshot = CommercialReceivingSession.Optional(facts.ProcessingFamilyCodeSnapshot, 32, nameof(facts.ProcessingFamilyCodeSnapshot));
        SupplierProductScopeId = facts.SupplierProductScopeId;
        SupplierProductScopeVersionSnapshot = facts.SupplierProductScopeVersionSnapshot;
        SupplierScopeModeSnapshot = CommercialReceivingSession.Defined(facts.SupplierScopeModeSnapshot, nameof(facts.SupplierScopeModeSnapshot));
        SupplierScopeValidationResultSnapshot = CommercialReceivingSession.Required(facts.SupplierScopeValidationResultSnapshot, 40, nameof(facts.SupplierScopeValidationResultSnapshot));
        ValidateScope();
        BagTypeId = CommercialReceivingSession.RequiredId(facts.BagTypeId, nameof(facts.BagTypeId));
        BagTypeVersionSnapshot = CommercialReceivingSession.Positive(facts.BagTypeVersionSnapshot, nameof(facts.BagTypeVersionSnapshot));
        BagTypeCodeSnapshot = CommercialReceivingSession.Required(facts.BagTypeCodeSnapshot, 32, nameof(facts.BagTypeCodeSnapshot));
        BagTypeNameSnapshot = CommercialReceivingSession.Required(facts.BagTypeNameSnapshot, 200, nameof(facts.BagTypeNameSnapshot));
        BagConstructionClassSnapshot = CommercialReceivingSession.Defined(facts.BagConstructionClassSnapshot, nameof(facts.BagConstructionClassSnapshot));
        if (facts.BagTareWeightKgSnapshot < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(facts.BagTareWeightKgSnapshot));
        }
        BagTareWeightKgSnapshot = facts.BagTareWeightKgSnapshot;
        BagReturnableSnapshot = facts.BagReturnableSnapshot;
        ProductStandardBagWeightId = facts.ProductStandardBagWeightId;
        ProductStandardBagWeightVersionSnapshot = facts.ProductStandardBagWeightVersionSnapshot;
        StandardBagWeightLabelSnapshot = CommercialReceivingSession.Optional(facts.StandardBagWeightLabelSnapshot, 100, nameof(facts.StandardBagWeightLabelSnapshot));
        StandardContentWeightKgSnapshot = facts.StandardContentWeightKgSnapshot;
        ValidateStandard();
        if (facts.BagCount <= 0)
        {
            throw new CommercialReceivingDomainException("RECEIVING_ENTRY_REQUIRED", "Bag count must be positive.");
        }
        if (facts.ProcessedWeightKg <= 0m)
        {
            throw new CommercialReceivingDomainException("RECEIVING_TOTAL_WEIGHT_REQUIRED", "Processed weight must be positive.");
        }
        BagCount = facts.BagCount;
        RawWeightKg = CommercialReceivingSession.Required(facts.RawWeightKg, 32, nameof(facts.RawWeightKg));
        ProcessedWeightKg = facts.ProcessedWeightKg;
        DisplayWeightKg = CommercialReceivingSession.Required(facts.DisplayWeightKg, 32, nameof(facts.DisplayWeightKg));
        if (facts.DecimalPlacesSnapshot is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(facts.DecimalPlacesSnapshot));
        }
        DecimalPlacesSnapshot = facts.DecimalPlacesSnapshot;
        ProcessingMethodSnapshot = CommercialReceivingSession.Defined(facts.ProcessingMethodSnapshot, nameof(facts.ProcessingMethodSnapshot));
        WeightSource = CommercialReceivingSession.Required(facts.WeightSource, 32, nameof(facts.WeightSource));
        CapturedAtDeviceUtc = CommercialReceivingSession.Utc(facts.CapturedAtDeviceUtc, nameof(facts.CapturedAtDeviceUtc));
        AcceptedAtServerUtc = CommercialReceivingSession.Utc(facts.AcceptedAtServerUtc, nameof(facts.AcceptedAtServerUtc));
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ReceivingSessionId { get; private set; }
    public Guid OperationId { get; private set; }
    public long LocalSequence { get; private set; }
    public Guid ProductId { get; private set; }
    public long ProductVersionSnapshot { get; private set; }
    public string ProductCodeSnapshot { get; private set; } = string.Empty;
    public string ProductNameSnapshot { get; private set; } = string.Empty;
    public ProductType ProductTypeSnapshot { get; private set; }
    public string? ProcessingFamilyCodeSnapshot { get; private set; }
    public Guid? SupplierProductScopeId { get; private set; }
    public long? SupplierProductScopeVersionSnapshot { get; private set; }
    public SupplierProductScopeMode SupplierScopeModeSnapshot { get; private set; }
    public string SupplierScopeValidationResultSnapshot { get; private set; } = string.Empty;
    public Guid BagTypeId { get; private set; }
    public long BagTypeVersionSnapshot { get; private set; }
    public string BagTypeCodeSnapshot { get; private set; } = string.Empty;
    public string BagTypeNameSnapshot { get; private set; } = string.Empty;
    public BagConstructionClass BagConstructionClassSnapshot { get; private set; }
    public decimal BagTareWeightKgSnapshot { get; private set; }
    public bool BagReturnableSnapshot { get; private set; }
    public Guid? ProductStandardBagWeightId { get; private set; }
    public long? ProductStandardBagWeightVersionSnapshot { get; private set; }
    public string? StandardBagWeightLabelSnapshot { get; private set; }
    public decimal? StandardContentWeightKgSnapshot { get; private set; }
    public int BagCount { get; private set; }
    public string RawWeightKg { get; private set; } = string.Empty;
    public decimal ProcessedWeightKg { get; private set; }
    public string DisplayWeightKg { get; private set; } = string.Empty;
    public int DecimalPlacesSnapshot { get; private set; }
    public WeightProcessingMethod ProcessingMethodSnapshot { get; private set; }
    public string WeightSource { get; private set; } = string.Empty;
    public DateTimeOffset CapturedAtDeviceUtc { get; private set; }
    public DateTimeOffset AcceptedAtServerUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc => AcceptedAtServerUtc;

    public static CommercialReceivingEntry Create(CommercialReceivingEntryFacts facts) => new(facts);

    private void ValidateScope()
    {
        if (SupplierScopeModeSnapshot is SupplierProductScopeMode.Restricted &&
            (SupplierProductScopeId is null || SupplierProductScopeVersionSnapshot is null or <= 0))
        {
            throw new CommercialReceivingDomainException("RECEIVING_PRODUCT_SCOPE_REQUIRED", "A Restricted Supplier requires scope evidence.");
        }
        if (SupplierScopeModeSnapshot is SupplierProductScopeMode.Unrestricted &&
            (SupplierProductScopeId is not null || SupplierProductScopeVersionSnapshot is not null))
        {
            throw new CommercialReceivingDomainException("RECEIVING_PRODUCT_SCOPE_INVALID", "An Unrestricted Supplier must not carry scope evidence.");
        }
    }

    private void ValidateStandard()
    {
        var absent = ProductStandardBagWeightId is null && ProductStandardBagWeightVersionSnapshot is null && StandardBagWeightLabelSnapshot is null && StandardContentWeightKgSnapshot is null;
        var complete = ProductStandardBagWeightId is not null && ProductStandardBagWeightVersionSnapshot is > 0 && StandardContentWeightKgSnapshot is > 0m;
        if (!absent && !complete)
        {
            throw new CommercialReceivingDomainException("RECEIVING_STANDARD_BAG_WEIGHT_INVALID", "The standard bag-weight snapshot is incomplete.");
        }
    }
}
