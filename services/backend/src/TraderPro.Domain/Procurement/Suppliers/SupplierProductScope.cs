using TraderPro.Domain.Common;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Domain.Procurement.Suppliers;

public sealed class SupplierProductScope :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private SupplierProductScope()
    {
    }

    private SupplierProductScope(
        Guid workspaceId,
        Guid companyId,
        Guid supplierId,
        Guid productId,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = MasterDataValueRules.RequiredId(
            workspaceId,
            nameof(workspaceId));
        CompanyId = MasterDataValueRules.RequiredId(
            companyId,
            nameof(companyId));
        SupplierId = MasterDataValueRules.RequiredId(
            supplierId,
            nameof(supplierId));
        ProductId = MasterDataValueRules.RequiredId(
            productId,
            nameof(productId));
        Status = MasterDataStatus.Active;
        CreatedAtUtc = MasterDataValueRules.RequireUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid SupplierId { get; private set; }

    public Guid ProductId { get; private set; }

    public MasterDataStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static SupplierProductScope Create(
        Guid workspaceId,
        Guid companyId,
        Guid supplierId,
        Guid productId,
        DateTimeOffset createdAtUtc)
    {
        return new SupplierProductScope(
            workspaceId,
            companyId,
            supplierId,
            productId,
            createdAtUtc);
    }

    public void Deactivate(
        bool supplierIsRestricted,
        int activeScopeCount,
        DateTimeOffset updatedAtUtc)
    {
        MasterDataValueRules.RequireInactive(Status);
        if (supplierIsRestricted && activeScopeCount <= 1)
        {
            throw new MasterDataDomainException(
                "SUPPLIER_PRODUCT_SCOPE_REQUIRED",
                "A Restricted supplier must retain at least one active product scope.",
                "status");
        }

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
}
