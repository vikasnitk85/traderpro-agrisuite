using TraderPro.Application.Common.Errors;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Application.Common.MasterData;

public static class CommercialMasterDataCommandTypes
{
    public const string CreateLocation =
        "Operations.BusinessLocation.Create";
    public const string UpdateLocation =
        "Operations.BusinessLocation.Update";
    public const string DeactivateLocation =
        "Operations.BusinessLocation.Deactivate";
    public const string ReactivateLocation =
        "Operations.BusinessLocation.Reactivate";

    public const string CreateVehicle =
        "Procurement.ReceivingVehicle.Create";
    public const string UpdateVehicle =
        "Procurement.ReceivingVehicle.Update";
    public const string DeactivateVehicle =
        "Procurement.ReceivingVehicle.Deactivate";
    public const string ReactivateVehicle =
        "Procurement.ReceivingVehicle.Reactivate";

    public const string CreateBagType =
        "Procurement.BagType.Create";
    public const string UpdateBagType =
        "Procurement.BagType.Update";
    public const string DeactivateBagType =
        "Procurement.BagType.Deactivate";
    public const string ReactivateBagType =
        "Procurement.BagType.Reactivate";

    public const string CreateWeightPolicy =
        "Procurement.WeightProcessingPolicy.Create";
    public const string UpdateWeightPolicy =
        "Procurement.WeightProcessingPolicy.Update";
    public const string DeactivateWeightPolicy =
        "Procurement.WeightProcessingPolicy.Deactivate";
    public const string ReactivateWeightPolicy =
        "Procurement.WeightProcessingPolicy.Reactivate";

    public const string ConfigureCompanySettings =
        "Procurement.CompanySettings.Configure";
    public const string UpdateCompanySettings =
        "Procurement.CompanySettings.Update";

    public const string CreateSupplier = "Procurement.Supplier.Create";
    public const string UpdateSupplier = "Procurement.Supplier.Update";
    public const string DeactivateSupplier =
        "Procurement.Supplier.Deactivate";
    public const string ReactivateSupplier =
        "Procurement.Supplier.Reactivate";
    public const string AddSupplierProductScope =
        "Procurement.SupplierProductScope.Add";
    public const string DeactivateSupplierProductScope =
        "Procurement.SupplierProductScope.Deactivate";
    public const string ReactivateSupplierProductScope =
        "Procurement.SupplierProductScope.Reactivate";

    public const string CreateProductGroup = "Catalog.ProductGroup.Create";
    public const string UpdateProductGroup = "Catalog.ProductGroup.Update";
    public const string DeactivateProductGroup =
        "Catalog.ProductGroup.Deactivate";
    public const string ReactivateProductGroup =
        "Catalog.ProductGroup.Reactivate";
    public const string CreateProduct = "Catalog.Product.Create";
    public const string UpdateProduct = "Catalog.Product.Update";
    public const string DeactivateProduct = "Catalog.Product.Deactivate";
    public const string ReactivateProduct = "Catalog.Product.Reactivate";
    public const string CreateProductStandardBagWeight =
        "Catalog.ProductStandardBagWeight.Create";
    public const string UpdateProductStandardBagWeight =
        "Catalog.ProductStandardBagWeight.Update";
    public const string DeactivateProductStandardBagWeight =
        "Catalog.ProductStandardBagWeight.Deactivate";
    public const string ReactivateProductStandardBagWeight =
        "Catalog.ProductStandardBagWeight.Reactivate";
    public const string SetDefaultProductStandardBagWeight =
        "Catalog.ProductStandardBagWeight.SetDefault";
}

public enum MasterStatusFilter
{
    Active,
    Inactive,
    All,
}

public sealed record MasterListQuery(
    MasterStatusFilter Status,
    string? Search,
    string? Cursor,
    int Limit);

public sealed record MasterPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);

public sealed record CreateBusinessLocationCommand(
    string Code,
    string Name,
    string? LocalName,
    string LocationType,
    string? AddressLine,
    string? Notes);

public sealed record UpdateBusinessLocationCommand(
    Guid Id,
    string Name,
    string? LocalName,
    string LocationType,
    string? AddressLine,
    string? Notes,
    long ExpectedVersion);

public sealed record BusinessLocationResult(
    Guid Id,
    Guid BranchId,
    string Code,
    string Name,
    string? LocalName,
    string LocationType,
    string? AddressLine,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record CreateReceivingVehicleCommand(
    string Code,
    string RegistrationNumber,
    string? DisplayName,
    string VehicleType,
    string? OwnerName,
    string? ContactNumber,
    string? Notes);

public sealed record UpdateReceivingVehicleCommand(
    Guid Id,
    string RegistrationNumber,
    string? DisplayName,
    string VehicleType,
    string? OwnerName,
    string? ContactNumber,
    string? Notes,
    long ExpectedVersion);

public sealed record ReceivingVehicleResult(
    Guid Id,
    string Code,
    string RegistrationNumber,
    string? DisplayName,
    string VehicleType,
    string? OwnerName,
    string? ContactNumber,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record CreateBagTypeCommand(
    string Code,
    string Name,
    string? LocalName,
    string ConstructionClass,
    decimal StandardTareWeightKg,
    bool IsReturnable,
    string? Notes);

public sealed record UpdateBagTypeCommand(
    Guid Id,
    string Name,
    string? LocalName,
    string ConstructionClass,
    decimal StandardTareWeightKg,
    bool IsReturnable,
    string? Notes,
    long ExpectedVersion);

public sealed record BagTypeResult(
    Guid Id,
    string Code,
    string Name,
    string? LocalName,
    string ConstructionClass,
    string StandardTareWeightKg,
    bool IsReturnable,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record CreateWeightProcessingPolicyCommand(
    string Code,
    string Name,
    int DecimalPlaces,
    string ProcessingMethod,
    string? Notes);

public sealed record UpdateWeightProcessingPolicyCommand(
    Guid Id,
    string Name,
    int DecimalPlaces,
    string ProcessingMethod,
    string? Notes,
    long ExpectedVersion);

public sealed record WeightProcessingPolicyResult(
    Guid Id,
    string Code,
    string Name,
    int DecimalPlaces,
    string ProcessingMethod,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record MasterStatusCommand(Guid Id, long ExpectedVersion);

public sealed record ConfigureCompanyProcurementSettingsCommand(
    Guid DefaultDestinationLocationId,
    Guid DefaultWeightProcessingPolicyId,
    string VehicleSelectionMode,
    long? ExpectedVersion);

public sealed record CompanyProcurementSettingsResult(
    Guid Id,
    Guid DefaultBranchId,
    Guid DefaultDestinationLocationId,
    Guid DefaultWeightProcessingPolicyId,
    string VehicleSelectionMode,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public static class MasterDataProblem
{
    public static ApplicationProblemException FromDomain(
        MasterDataDomainException exception)
    {
        return new ApplicationProblemException(
            exception.Code,
            exception.Message,
            exception.Code is
                "MASTER_STATUS_INVALID" or
                "SUPPLIER_PRODUCT_SCOPE_REQUIRED" or
                "PRODUCT_GROUP_IN_USE" or
                "PRODUCT_SUPPLIER_SCOPE_IN_USE" or
                "PRODUCT_STANDARD_BAG_WEIGHT_IN_USE" or
                "PRODUCT_STANDARD_BAG_WEIGHT_DEFAULT_CONFLICT"
                ? ApplicationErrorCategory.Conflict
                : ApplicationErrorCategory.Validation,
            fieldErrors: exception.Field is null
                ? []
                :
                [
                    new ApplicationFieldError(
                        exception.Field,
                        exception.Code,
                        exception.Message),
                ],
            innerException: exception);
    }
}
