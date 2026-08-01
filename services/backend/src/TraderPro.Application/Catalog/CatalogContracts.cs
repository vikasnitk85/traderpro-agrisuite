using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.MasterData;

namespace TraderPro.Application.Catalog;

public sealed record CreateProductGroupCommand(
    string Code,
    string Name,
    string? LocalName,
    string? Description);

public sealed record UpdateProductGroupCommand(
    Guid Id,
    string Name,
    string? LocalName,
    string? Description,
    long ExpectedVersion);

public sealed record ProductGroupResult(
    Guid Id,
    string Code,
    string Name,
    string? LocalName,
    string? Description,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record CreateProductCommand(
    Guid ProductGroupId,
    string Code,
    string Name,
    string? LocalName,
    string ProductType,
    bool IsPurchasable,
    string? ProcessingFamilyCode,
    string? Description,
    string? Notes);

public sealed record UpdateProductCommand(
    Guid Id,
    Guid ProductGroupId,
    string Name,
    string? LocalName,
    string ProductType,
    bool IsPurchasable,
    string? ProcessingFamilyCode,
    string? Description,
    string? Notes,
    long ExpectedVersion);

public sealed record ProductResult(
    Guid Id,
    Guid ProductGroupId,
    string Code,
    string Name,
    string? LocalName,
    string ProductType,
    bool IsPurchasable,
    string? ProcessingFamilyCode,
    string? NormalizedProcessingFamilyCode,
    string? Description,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record ProductListQuery(
    MasterListQuery Master,
    Guid? ProductGroupId,
    string? ProductType,
    bool? IsPurchasable,
    string? NormalizedProcessingFamilyCode);

public sealed record CreateProductStandardBagWeightCommand(
    Guid ProductId,
    Guid BagTypeId,
    string? Label,
    decimal StandardContentWeightKg,
    bool IsDefault);

public sealed record UpdateProductStandardBagWeightCommand(
    Guid ProductId,
    Guid Id,
    string? Label,
    decimal StandardContentWeightKg,
    long ExpectedVersion);

public sealed record ProductStandardBagWeightStatusCommand(
    Guid ProductId,
    Guid Id,
    long ExpectedVersion);

public sealed record ProductStandardBagWeightResult(
    Guid Id,
    Guid ProductId,
    Guid BagTypeId,
    string BagTypeCode,
    string? Label,
    string StandardContentWeightKg,
    bool IsDefault,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public interface ICatalogService
{
    Task<IdempotentCommandResult<ProductGroupResult>> CreateGroupAsync(
        CreateProductGroupCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductGroupResult>> UpdateGroupAsync(
        UpdateProductGroupCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductGroupResult>> SetGroupStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ProductGroupResult> GetGroupAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<ProductGroupResult>> ListGroupsAsync(
        MasterListQuery query,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductResult>> CreateProductAsync(
        CreateProductCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductResult>> UpdateProductAsync(
        UpdateProductCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductResult>> SetProductStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ProductResult> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<ProductResult>> ListProductsAsync(
        ProductListQuery query,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        CreateBagStandardAsync(
            CreateProductStandardBagWeightCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        UpdateBagStandardAsync(
            UpdateProductStandardBagWeightCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        SetBagStandardStatusAsync(
            ProductStandardBagWeightStatusCommand command,
            bool activate,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<IdempotentCommandResult<ProductStandardBagWeightResult>>
        SetDefaultBagStandardAsync(
            ProductStandardBagWeightStatusCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken);

    Task<ProductStandardBagWeightResult> GetBagStandardAsync(
        Guid productId,
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<ProductStandardBagWeightResult>> ListBagStandardsAsync(
        Guid productId,
        MasterListQuery query,
        CancellationToken cancellationToken);
}

public interface IBagTypeProductStandardUsageReader
{
    Task<bool> AcquireLockAndIsInUseAsync(
        Guid bagTypeId,
        CancellationToken cancellationToken);
}
