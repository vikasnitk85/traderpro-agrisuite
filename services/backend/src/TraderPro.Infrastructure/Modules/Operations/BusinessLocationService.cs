using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Operations;
using TraderPro.Application.Platform.Identity;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Operations;

internal sealed class BusinessLocationService(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext context,
    IBusinessLocationDefaultUsageReader defaultUsage,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency) :
    IBusinessLocationService
{
    private const string AggregateType = "Operations.BusinessLocation";
    private const int EventVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public Task<IdempotentCommandResult<BusinessLocationResult>> CreateAsync(
        CreateBusinessLocationCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var normalizedCode =
            CommercialMasterDataInfrastructure.NormalizeCode(command.Code);
        var locationType =
            CommercialMasterDataInputRules.ParseLocationType(
                command.LocationType);
        var requestHash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.CreateLocation,
            context,
            ("code", normalizedCode),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("locationType", locationType),
            ("addressLine", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.AddressLine)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            CommercialMasterDataCommandTypes.CreateLocation,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            requestHash,
            context.CorrelationId,
            201,
            async () =>
            {
                if (await dbContext.BusinessLocations.AnyAsync(
                        item =>
                            item.CompanyId == context.CompanyId &&
                            item.NormalizedCode == normalizedCode,
                        cancellationToken))
                {
                    throw CodeExists();
                }

                var now = clock.UtcNow;
                var location = CommercialMasterDataInfrastructure.Domain(
                    () => BusinessLocation.Create(
                        context.WorkspaceId,
                        context.CompanyId,
                        context.DefaultBranchId,
                        normalizedCode,
                        command.Name,
                        command.LocalName,
                        locationType,
                        command.AddressLine,
                        command.Notes,
                        now));
                dbContext.BusinessLocations.Add(location);
                var result = ToResult(location);
                AddFacts(
                    location,
                    "Operations.BusinessLocation.Created",
                    "Operations.BusinessLocationCreated",
                    null,
                    result,
                    now,
                    location.Version);
                return result;
            },
            IsReplayable,
            null,
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    public Task<IdempotentCommandResult<BusinessLocationResult>> UpdateAsync(
        UpdateBusinessLocationCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var locationType =
            CommercialMasterDataInputRules.ParseLocationType(
                command.LocationType);
        var requestHash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.UpdateLocation,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion),
            ("name", CommercialMasterDataInputRules.CanonicalRequiredText(
                command.Name)),
            ("localName", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.LocalName)),
            ("locationType", locationType),
            ("addressLine", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.AddressLine)),
            ("notes", CommercialMasterDataInputRules.CanonicalOptionalText(
                command.Notes)));
        return ExecuteMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.UpdateLocation,
            idempotencyKey,
            requestHash,
            "Operations.BusinessLocation.Updated",
            "Operations.BusinessLocationUpdated",
            (location, now) => CommercialMasterDataInfrastructure.Domain(
                () => location.Update(
                    command.Name,
                    command.LocalName,
                    locationType,
                    command.AddressLine,
                    command.Notes,
                    now)),
            cancellationToken);
    }

    public async Task<IdempotentCommandResult<BusinessLocationResult>>
        DeactivateAsync(
            MasterStatusCommand command,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var requestHash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.DeactivateLocation,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return await ExecuteMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.DeactivateLocation,
            idempotencyKey,
            requestHash,
            "Operations.BusinessLocation.Deactivated",
            "Operations.BusinessLocationDeactivated",
            async (location, now) =>
            {
                if (await defaultUsage.IsCurrentDefaultAsync(
                        location.Id,
                        cancellationToken))
                {
                    throw DefaultInUse();
                }

                CommercialMasterDataInfrastructure.Domain(
                    () => location.Deactivate(now));
            },
            cancellationToken);
    }

    public Task<IdempotentCommandResult<BusinessLocationResult>> ReactivateAsync(
        MasterStatusCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(command);
        var requestHash = CommercialMasterDataRequestHash.Compute(
            CommercialMasterDataCommandTypes.ReactivateLocation,
            context,
            ("id", command.Id),
            ("expectedVersion", command.ExpectedVersion));
        return ExecuteMutationAsync(
            command.Id,
            command.ExpectedVersion,
            CommercialMasterDataCommandTypes.ReactivateLocation,
            idempotencyKey,
            requestHash,
            "Operations.BusinessLocation.Reactivated",
            "Operations.BusinessLocationReactivated",
            (location, now) => CommercialMasterDataInfrastructure.Domain(
                () => location.Reactivate(now)),
            cancellationToken);
    }

    public async Task<BusinessLocationResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var entity = await dbContext.BusinessLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                item.Id == id &&
                item.CompanyId == context.CompanyId &&
                item.BranchId == context.DefaultBranchId,
                cancellationToken);
        return entity is null ? throw NotFound() : ToResult(entity);
    }

    public async Task<MasterPage<BusinessLocationResult>> ListAsync(
        MasterListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        ArgumentNullException.ThrowIfNull(query);
        var cursorScope = new CommercialMasterCursorScope(
            CommercialMasterKinds.BusinessLocation,
            context.WorkspaceId,
            context.CompanyId,
            context.DefaultBranchId,
            query.Status,
            query.Search);
        var cursor = CommercialMasterDataInfrastructure.DecodeCursor(
            query.Cursor,
            cursorScope);
        var rows = dbContext.BusinessLocations
            .AsNoTracking()
            .Where(item =>
                item.CompanyId == context.CompanyId &&
                item.BranchId == context.DefaultBranchId);
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
        return new MasterPage<BusinessLocationResult>(
            selected.Select(item => ToResult(item)).ToArray(),
            hasMore
                ? CommercialMasterDataInfrastructure.EncodeCursor(
                    cursorScope,
                    selected[^1].NormalizedCode,
                    selected[^1].Id)
                : null,
            hasMore);
    }

    private Task<IdempotentCommandResult<BusinessLocationResult>>
        ExecuteMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string auditAction,
            string eventType,
            Action<BusinessLocation, DateTimeOffset> mutate,
            CancellationToken cancellationToken)
    {
        return ExecuteMutationAsync(
            id,
            expectedVersion,
            commandType,
            idempotencyKey,
            requestHash,
            auditAction,
            eventType,
            (location, now) =>
            {
                mutate(location, now);
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    private Task<IdempotentCommandResult<BusinessLocationResult>>
        ExecuteMutationAsync(
            Guid id,
            long expectedVersion,
            string commandType,
            string idempotencyKey,
            string requestHash,
            string auditAction,
            string eventType,
            Func<BusinessLocation, DateTimeOffset, Task> mutate,
            CancellationToken cancellationToken)
    {
        CommercialMasterDataInputRules.RequireExpectedVersion(expectedVersion);
        return idempotency.ExecuteAsync(
            context.WorkspaceId,
            commandType,
            CommercialMasterDataInputRules.RequireIdempotencyKey(
                idempotencyKey),
            requestHash,
            context.CorrelationId,
            200,
            async () =>
            {
                var location = await FindTrackedAsync(id, cancellationToken);
                if (location.Version != expectedVersion)
                {
                    throw CommercialMasterDataInfrastructure.VersionConflict(
                        expectedVersion,
                        location.Version);
                }

                var before = ToResult(location);
                var now = clock.UtcNow;
                await mutate(location, now);
                var after = ToResult(location);
                AddFacts(
                    location,
                    auditAction,
                    eventType,
                    before,
                    after,
                    now,
                    location.Version);
                return after;
            },
            IsReplayable,
            () => ReadConcurrencyProblemAsync(
                id,
                expectedVersion,
                cancellationToken),
            MapPersistenceProblem,
            OutboxCommitOrdering.IndependentInternal,
            cancellationToken);
    }

    private async Task<BusinessLocation> FindTrackedAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.BusinessLocations.SingleOrDefaultAsync(
                item =>
                    item.Id == id &&
                    item.CompanyId == context.CompanyId &&
                    item.BranchId == context.DefaultBranchId,
                cancellationToken) ??
            throw NotFound();
    }

    private async Task<ApplicationProblemException> ReadConcurrencyProblemAsync(
        Guid id,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion = await dbContext.BusinessLocations
            .AsNoTracking()
            .Where(item =>
                item.Id == id &&
                item.CompanyId == context.CompanyId &&
                item.BranchId == context.DefaultBranchId)
            .Select(item => (long?)item.Version)
            .SingleOrDefaultAsync(cancellationToken);
        return currentVersion is null
            ? NotFound()
            : CommercialMasterDataInfrastructure.VersionConflict(
                expectedVersion,
                currentVersion.Value);
    }

    private void AddFacts(
        BusinessLocation location,
        string action,
        string eventType,
        BusinessLocationResult? before,
        BusinessLocationResult after,
        DateTimeOffset occurredAtUtc,
        long version)
    {
        var statusPayload = JsonSerializer.Serialize(
            new
            {
                locationId = location.Id,
                location.BranchId,
                status = location.Status.ToString(),
                version,
            },
            JsonOptions);
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                context.WorkspaceId,
                context.CompanyId,
                context.DefaultBranchId,
                context.UserId,
                context.DeviceId,
                action,
                AggregateType,
                location.Id,
                null,
                null,
                before is null
                    ? null
                    : JsonSerializer.Serialize(before, JsonOptions),
                JsonSerializer.Serialize(after, JsonOptions),
                context.CorrelationId,
                occurredAtUtc));
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                context.WorkspaceId,
                eventType,
                EventVersion,
                AggregateType,
                location.Id,
                version,
                statusPayload,
                context.CorrelationId,
                occurredAtUtc,
                OutboxEventStream.Internal));
    }

    private void RequireContext()
    {
        if (!context.IsBound)
        {
            throw new ApplicationProblemException(
                "AUTHENTICATED_CONTEXT_INVALID",
                "The authenticated TraderPro context is invalid.",
                ApplicationErrorCategory.Authentication);
        }
    }

    private static BusinessLocationResult ToResult(
        BusinessLocation location)
    {
        return new BusinessLocationResult(
            location.Id,
            location.BranchId,
            location.Code,
            location.Name,
            location.LocalName,
            location.LocationType.ToString(),
            location.AddressLine,
            location.Notes,
            location.Status.ToString(),
            location.CreatedAtUtc,
            location.UpdatedAtUtc,
            location.Version);
    }

    private static bool IsReplayable(BusinessLocationResult result)
    {
        return result.Id != Guid.Empty &&
            result.BranchId != Guid.Empty &&
            result.Code.Length is >= 2 and <= 32 &&
            result.Version > 0;
    }

    private static ApplicationProblemException NotFound()
    {
        return new ApplicationProblemException(
            "BUSINESS_LOCATION_NOT_FOUND",
            "The business location was not found.",
            ApplicationErrorCategory.NotFound);
    }

    private static ApplicationProblemException CodeExists()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "BUSINESS_LOCATION_CODE_EXISTS",
            "A business location with this code already exists.");
    }

    private static ApplicationProblemException DefaultInUse()
    {
        return CommercialMasterDataInfrastructure.Conflict(
            "PROCUREMENT_DEFAULT_LOCATION_IN_USE",
            "The current default procurement destination must be changed before this location can be deactivated.");
    }

    private static ApplicationProblemException? MapPersistenceProblem(
        Exception exception)
    {
        var postgres =
            CommercialMasterDataInfrastructure.PostgreSql(exception);
        return postgres?.ConstraintName switch
        {
            "ux_business_locations_workspace_company_code" => CodeExists(),
            "ck_procurement_default_location_active" => DefaultInUse(),
            _ => null,
        };
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
