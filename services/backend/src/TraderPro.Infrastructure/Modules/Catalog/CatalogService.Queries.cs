using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Catalog;
using TraderPro.Application.Common.MasterData;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Infrastructure.Modules.Shared;

namespace TraderPro.Infrastructure.Modules.Catalog;

internal sealed partial class CatalogService
{
    public async Task<ProductGroupResult> GetGroupAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var group = await dbContext.ProductGroups
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken);
        return group is null ? throw ProductGroupNotFound() : ToResult(group);
    }

    public async Task<MasterPage<ProductGroupResult>> ListGroupsAsync(
        MasterListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var scope = new CommercialMasterCursorScope(
            CommercialMasterKinds.ProductGroup,
            context.WorkspaceId,
            context.CompanyId,
            null,
            query.Status,
            query.Search);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            scope);
        var rows = dbContext.ProductGroups
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            query.Status,
            item => item.Status);
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Code, pattern, "\\") ||
                EF.Functions.ILike(item.Name, pattern, "\\") ||
                (item.LocalName != null &&
                    EF.Functions.ILike(item.LocalName, pattern, "\\")));
        }

        if (cursor is not null)
        {
            rows = rows.Where(item =>
                string.Compare(item.NormalizedCode, cursor.Value.Code) > 0 ||
                (item.NormalizedCode == cursor.Value.Code &&
                    item.Id.CompareTo(cursor.Value.Id) > 0));
        }

        var page = await rows
            .OrderBy(item => item.NormalizedCode)
            .ThenBy(item => item.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > query.Limit;
        var selected = page.Take(query.Limit).ToArray();
        return new MasterPage<ProductGroupResult>(
            selected.Select(ToResult).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    scope,
                    selected[^1].NormalizedCode,
                    selected[^1].Id)
                : null,
            hasMore);
    }

    public async Task<ProductResult> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.CompanyId == context.CompanyId,
                cancellationToken);
        return product is null ? throw ProductNotFound() : ToResult(product);
    }

    public async Task<MasterPage<ProductResult>> ListProductsAsync(
        ProductListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var master = query.Master;
        ProductType? productType = query.ProductType is null
            ? null
            : CommercialMasterDataInputRules.ParseProductType(
                query.ProductType);
        var filterScope =
            $"group={query.ProductGroupId?.ToString("D") ?? "*"};type={productType?.ToString() ?? "*"};purchasable={query.IsPurchasable?.ToString().ToLowerInvariant() ?? "*"};family={query.NormalizedProcessingFamilyCode ?? "*"}";
        var scope = new CommercialMasterCursorScope(
            CommercialMasterKinds.Product,
            context.WorkspaceId,
            context.CompanyId,
            null,
            master.Status,
            master.Search,
            filterScope);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            master.Cursor,
            scope);
        var rows = dbContext.Products
            .AsNoTracking()
            .Where(item => item.CompanyId == context.CompanyId);
        rows = CommercialMasterDataInfrastructure.ApplyStatus(
            rows,
            master.Status,
            item => item.Status);
        if (query.ProductGroupId is not null)
        {
            rows = rows.Where(item =>
                item.ProductGroupId == query.ProductGroupId.Value);
        }

        if (productType is not null)
        {
            rows = rows.Where(item =>
                item.ProductType == productType.Value);
        }

        if (query.IsPurchasable is not null)
        {
            rows = rows.Where(item =>
                item.IsPurchasable == query.IsPurchasable.Value);
        }

        if (query.NormalizedProcessingFamilyCode is not null)
        {
            rows = rows.Where(item =>
                item.NormalizedProcessingFamilyCode ==
                    query.NormalizedProcessingFamilyCode);
        }

        if (master.Search is not null)
        {
            var pattern = $"%{EscapeLike(master.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.Code, pattern, "\\") ||
                EF.Functions.ILike(item.Name, pattern, "\\") ||
                (item.LocalName != null &&
                    EF.Functions.ILike(item.LocalName, pattern, "\\")));
        }

        if (cursor is not null)
        {
            rows = rows.Where(item =>
                string.Compare(item.NormalizedCode, cursor.Value.Code) > 0 ||
                (item.NormalizedCode == cursor.Value.Code &&
                    item.Id.CompareTo(cursor.Value.Id) > 0));
        }

        var page = await rows
            .OrderBy(item => item.NormalizedCode)
            .ThenBy(item => item.Id)
            .Take(master.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > master.Limit;
        var selected = page.Take(master.Limit).ToArray();
        return new MasterPage<ProductResult>(
            selected.Select(ToResult).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    scope,
                    selected[^1].NormalizedCode,
                    selected[^1].Id)
                : null,
            hasMore);
    }

    public async Task<ProductStandardBagWeightResult> GetBagStandardAsync(
        Guid productId,
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var row = await (
                from standard in dbContext.ProductStandardBagWeights
                    .AsNoTracking()
                join bagType in dbContext.BagTypes.AsNoTracking()
                    on standard.BagTypeId equals bagType.Id
                where standard.Id == id &&
                    standard.ProductId == productId &&
                    standard.CompanyId == context.CompanyId
                select new { Standard = standard, BagTypeCode = bagType.Code })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null
            ? throw BagStandardNotFound()
            : ToResult(row.Standard, row.BagTypeCode);
    }

    public async Task<MasterPage<ProductStandardBagWeightResult>>
        ListBagStandardsAsync(
            Guid productId,
            MasterListQuery query,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var filterScope = $"productId={productId:D}";
        var scope = new CommercialMasterCursorScope(
            CommercialMasterKinds.ProductStandardBagWeight,
            context.WorkspaceId,
            context.CompanyId,
            null,
            query.Status,
            query.Search,
            filterScope);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            scope);
        if (!await dbContext.Products.AsNoTracking().AnyAsync(
                item =>
                    item.Id == productId &&
                    item.CompanyId == context.CompanyId,
                cancellationToken))
        {
            throw ProductNotFound();
        }

        var rows =
            from standard in dbContext.ProductStandardBagWeights.AsNoTracking()
            join bagType in dbContext.BagTypes.AsNoTracking()
                on standard.BagTypeId equals bagType.Id
            where standard.CompanyId == context.CompanyId &&
                standard.ProductId == productId
            select new { Standard = standard, BagType = bagType };
        rows = query.Status switch
        {
            MasterStatusFilter.Active => rows.Where(item =>
                item.Standard.Status == MasterDataStatus.Active),
            MasterStatusFilter.Inactive => rows.Where(item =>
                item.Standard.Status == MasterDataStatus.Inactive),
            _ => rows,
        };
        if (query.Search is not null)
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            rows = rows.Where(item =>
                EF.Functions.ILike(item.BagType.Code, pattern, "\\") ||
                EF.Functions.ILike(item.BagType.Name, pattern, "\\") ||
                (item.Standard.Label != null &&
                    EF.Functions.ILike(
                        item.Standard.Label,
                        pattern,
                        "\\")));
        }

        if (cursor is not null)
        {
            rows = rows.Where(item =>
                string.Compare(
                    item.BagType.NormalizedCode,
                    cursor.Value.Code) > 0 ||
                (item.BagType.NormalizedCode == cursor.Value.Code &&
                    item.Standard.Id.CompareTo(cursor.Value.Id) > 0));
        }

        var page = await rows
            .OrderBy(item => item.BagType.NormalizedCode)
            .ThenBy(item => item.Standard.Id)
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > query.Limit;
        var selected = page.Take(query.Limit).ToArray();
        return new MasterPage<ProductStandardBagWeightResult>(
            selected.Select(item => ToResult(
                item.Standard,
                item.BagType.Code)).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    scope,
                    selected[^1].BagType.NormalizedCode,
                    selected[^1].Standard.Id)
                : null,
            hasMore);
    }

    private static ProductGroupResult ToResult(ProductGroup group)
    {
        return new ProductGroupResult(
            group.Id,
            group.Code,
            group.Name,
            group.LocalName,
            group.Description,
            group.Status.ToString(),
            group.CreatedAtUtc,
            group.UpdatedAtUtc,
            group.Version);
    }

    private static ProductResult ToResult(Product product)
    {
        return new ProductResult(
            product.Id,
            product.ProductGroupId,
            product.Code,
            product.Name,
            product.LocalName,
            product.ProductType.ToString(),
            product.IsPurchasable,
            product.ProcessingFamilyCode,
            product.NormalizedProcessingFamilyCode,
            product.Description,
            product.Notes,
            product.Status.ToString(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc,
            product.Version);
    }

    private static ProductStandardBagWeightResult ToResult(
        ProductStandardBagWeight standard,
        string bagTypeCode)
    {
        return new ProductStandardBagWeightResult(
            standard.Id,
            standard.ProductId,
            standard.BagTypeId,
            bagTypeCode,
            standard.Label,
            standard.StandardContentWeightKg.ToString(
                "0.000000",
                CultureInfo.InvariantCulture),
            standard.IsDefault,
            standard.Status.ToString(),
            standard.CreatedAtUtc,
            standard.UpdatedAtUtc,
            standard.Version);
    }

    private static bool IsReplayable(ProductGroupResult result)
    {
        return result.Id != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(ProductResult result)
    {
        return result.Id != Guid.Empty &&
            result.ProductGroupId != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static bool IsReplayable(
        ProductStandardBagWeightResult result)
    {
        return result.Id != Guid.Empty &&
            result.ProductId != Guid.Empty &&
            result.BagTypeId != Guid.Empty &&
            result.Version > 0;
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
