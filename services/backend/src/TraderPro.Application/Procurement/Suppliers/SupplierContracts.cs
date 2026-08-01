using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.MasterData;

namespace TraderPro.Application.Procurement.Suppliers;

public sealed record CreateSupplierCommand(
    string Code,
    string Name,
    string? LocalName,
    string SupplierType,
    string ProductScopeMode,
    string? ContactName,
    string? ContactNumber,
    string? Email,
    string? AddressLine,
    string? TaxRegistrationNumber,
    string? Notes,
    IReadOnlyList<Guid> InitialProductIds);

public sealed record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? LocalName,
    string SupplierType,
    string ProductScopeMode,
    string? ContactName,
    string? ContactNumber,
    string? Email,
    string? AddressLine,
    string? TaxRegistrationNumber,
    string? Notes,
    long ExpectedVersion);

public sealed record SupplierResult(
    Guid Id,
    string Code,
    string Name,
    string? LocalName,
    string SupplierType,
    string ProductScopeMode,
    string? ContactName,
    string? ContactNumber,
    string? Email,
    string? AddressLine,
    string? TaxRegistrationNumber,
    string? NormalizedTaxRegistrationNumber,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public sealed record SupplierListQuery(
    MasterListQuery Master,
    string? SupplierType,
    string? ProductScopeMode);

public sealed record AddSupplierProductScopeCommand(
    Guid SupplierId,
    Guid ProductId);

public sealed record SupplierProductScopeStatusCommand(
    Guid SupplierId,
    Guid Id,
    long ExpectedVersion);

public sealed record SupplierProductScopeResult(
    Guid Id,
    Guid SupplierId,
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

public interface ISupplierService
{
    Task<IdempotentCommandResult<SupplierResult>> CreateAsync(
        CreateSupplierCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<SupplierResult>> UpdateAsync(
        UpdateSupplierCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<SupplierResult>> SetStatusAsync(
        MasterStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<SupplierResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<MasterPage<SupplierResult>> ListAsync(
        SupplierListQuery query,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<SupplierProductScopeResult>> AddScopeAsync(
        AddSupplierProductScopeCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IdempotentCommandResult<SupplierProductScopeResult>> SetScopeStatusAsync(
        SupplierProductScopeStatusCommand command,
        bool activate,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<MasterPage<SupplierProductScopeResult>> ListScopesAsync(
        Guid supplierId,
        MasterListQuery query,
        CancellationToken cancellationToken);
}
