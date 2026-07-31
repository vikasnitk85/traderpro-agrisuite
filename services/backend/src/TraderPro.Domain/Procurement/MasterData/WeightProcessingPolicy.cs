using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Common.Measurements;

namespace TraderPro.Domain.Procurement.MasterData;

public sealed class WeightProcessingPolicy :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private WeightProcessingPolicy()
    {
    }

    private WeightProcessingPolicy(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        int decimalPlaces,
        WeightProcessingMethod processingMethod,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = MasterDataValueRules.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = MasterDataValueRules.RequiredId(
            companyId,
            nameof(companyId));
        NormalizedCode = MasterDataValueRules.NormalizeCode(code);
        Code = NormalizedCode;
        ApplyDetails(name, decimalPlaces, processingMethod, notes);
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string NormalizedCode { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public int DecimalPlaces { get; private set; }

    public WeightProcessingMethod ProcessingMethod { get; private set; }

    public string? Notes { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static WeightProcessingPolicy Create(
        Guid workspaceId,
        Guid companyId,
        string code,
        string name,
        int decimalPlaces,
        WeightProcessingMethod processingMethod,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        return new WeightProcessingPolicy(
            workspaceId,
            companyId,
            code,
            name,
            decimalPlaces,
            processingMethod,
            notes,
            createdAtUtc);
    }

    public void Update(
        string name,
        int decimalPlaces,
        WeightProcessingMethod processingMethod,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        ApplyDetails(name, decimalPlaces, processingMethod, notes);
        AdvanceRevision(updatedAtUtc);
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        Status = MasterDataStatus.Inactive;
        AdvanceRevision(updatedAtUtc);
    }

    public void Reactivate(DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireActive(Status);
        Status = MasterDataStatus.Active;
        AdvanceRevision(updatedAtUtc);
    }

    private void AdvanceRevision(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = MasterDataValueRules.RequireRevisionTimestamp(
            UpdatedAtUtc,
            updatedAtUtc);
        Version = MasterDataValueRules.NextVersion(Version);
    }

    private void ApplyDetails(
        string name,
        int decimalPlaces,
        WeightProcessingMethod processingMethod,
        string? notes)
    {
        try
        {
            _ = Common.Measurements.WeightProcessingPolicy.Create(
                decimalPlaces,
                processingMethod);
        }
        catch (WeightProcessingException exception)
        {
            var code = exception.ErrorCode switch
            {
                WeightProcessingException.DecimalPlacesInvalid =>
                    "WEIGHT_POLICY_DECIMAL_PLACES_INVALID",
                _ => "WEIGHT_POLICY_METHOD_INVALID",
            };
            throw new MasterDataDomainException(
                code,
                exception.Message,
                exception.ErrorCode ==
                    WeightProcessingException.DecimalPlacesInvalid
                    ? "decimalPlaces"
                    : "processingMethod");
        }

        Name = MasterDataValueRules.RequiredText(
            name,
            MasterDataValueRules.MaximumNameLength,
            "name");
        DecimalPlaces = decimalPlaces;
        ProcessingMethod = processingMethod;
        Notes = MasterDataValueRules.OptionalText(
            notes,
            MasterDataValueRules.MaximumNotesLength,
            "notes",
            "WEIGHT_POLICY_INVALID");
    }
}
