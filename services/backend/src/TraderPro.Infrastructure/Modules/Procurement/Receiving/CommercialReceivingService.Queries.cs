using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Receiving;

namespace TraderPro.Infrastructure.Modules.Procurement.Receiving;

internal sealed partial class CommercialReceivingService
{
    public async Task<CommercialReceivingListResult> ListAsync(
        CommercialReceivingListQuery query,
        CancellationToken cancellationToken)
    {
        RequireContext();
        if (query.Limit is < 1 or > 100) throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "Limit must be between 1 and 100.", ApplicationErrorCategory.Validation);
        CommercialReceivingStatus? status = null;
        if (query.Status is not null)
        {
            if (!Enum.TryParse(query.Status, false, out CommercialReceivingStatus parsed) || !Enum.IsDefined(parsed))
                throw Problem("RECEIVING_STATUS_INVALID", "The Receiving status filter is invalid.", ApplicationErrorCategory.Validation);
            status = parsed;
        }
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var cursor = Decode<ListCursor>(query.Cursor, "RECEIVING_SESSION_ID_INVALID");
        if (cursor is not null && (cursor.WorkspaceId != current.WorkspaceId || cursor.CompanyId != current.CompanyId || cursor.DeviceId != current.DeviceId || cursor.IsOwner != current.IsOwner || cursor.Status != query.Status || cursor.Search != search))
            throw Problem("RECEIVING_SESSION_ID_INVALID", "The Receiving list cursor is invalid.", ApplicationErrorCategory.Validation);

        var rows = from session in dbContext.CommercialReceivingSessions.AsNoTracking()
                   join ownership in dbContext.CommercialReceivingOwnerships.AsNoTracking() on session.Id equals ownership.ReceivingSessionId
                   where session.CompanyId == current.CompanyId && (current.IsOwner || ownership.EditorDeviceId == current.DeviceId)
                   select new
                   {
                       session,
                       ownership,
                       lastCloudUpdateUtc = session.UpdatedAtUtc >= ownership.UpdatedAtUtc
                           ? session.UpdatedAtUtc
                           : ownership.UpdatedAtUtc,
                   };
        if (status is not null) rows = rows.Where(row => row.session.Status == status);
        if (search is not null) rows = rows.Where(row => row.session.CloudReference.Contains(search) || (row.session.ExternalReference != null && row.session.ExternalReference.Contains(search)));
        if (cursor is not null) rows = rows.Where(row => row.lastCloudUpdateUtc < cursor.LastCloudUpdateUtc || (row.lastCloudUpdateUtc == cursor.LastCloudUpdateUtc && string.Compare(row.session.CloudReference, cursor.CloudReference) < 0));
        var page = await rows.OrderByDescending(row => row.lastCloudUpdateUtc).ThenByDescending(row => row.session.CloudReference).Take(query.Limit + 1).ToListAsync(cancellationToken);
        var hasMore = page.Count > query.Limit;
        var items = page.Take(query.Limit).Select(row => new CommercialReceivingListItem(
            row.session.Id, row.session.CloudReference, row.session.ExternalReference, row.session.Status.ToString(),
            row.session.SupplierCodeSnapshot, row.session.SupplierNameSnapshot, row.session.EntryCount,
            DecimalText(row.session.ProcessedTotalWeightKg), row.ownership.EditorDeviceId,
            row.ownership.OwnershipGeneration, row.ownership.LeaseExpiresAtUtc,
            LeaseHealth(row.session, row.ownership), LeaseAttention(row.session, row.ownership),
            row.lastCloudUpdateUtc, row.session.Version)).ToArray();
        var last = page.Take(query.Limit).LastOrDefault();
        return new CommercialReceivingListResult(items, hasMore && last is not null ? Encode(new ListCursor(1, current.WorkspaceId, current.CompanyId, current.DeviceId, current.IsOwner, query.Status, search, last.lastCloudUpdateUtc, last.session.CloudReference)) : null, hasMore);
    }

    public async Task<CommercialReceivingLiveView> GetLiveViewAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        RequireContext();
        var row = await (from session in dbContext.CommercialReceivingSessions.AsNoTracking()
                         join ownership in dbContext.CommercialReceivingOwnerships.AsNoTracking() on session.Id equals ownership.ReceivingSessionId
                         where session.CompanyId == current.CompanyId && session.Id == sessionId && (current.IsOwner || ownership.EditorDeviceId == current.DeviceId)
                         select new { session, ownership }).SingleOrDefaultAsync(cancellationToken)
            ?? throw Problem("RECEIVING_SESSION_NOT_FOUND", "The Receiving Session was not found.", ApplicationErrorCategory.NotFound);
        var entries = await dbContext.CommercialReceivingEntries.AsNoTracking().Where(entry => entry.ReceivingSessionId == sessionId)
            .OrderByDescending(entry => entry.LocalSequence).Take(5)
            .Select(entry => new CommercialReceivingRecentEntry(entry.Id, entry.LocalSequence, entry.ProductCodeSnapshot, entry.ProductNameSnapshot, entry.BagTypeCodeSnapshot, entry.BagCount, entry.RawWeightKg, entry.ProcessedWeightKg.ToString("0.000000"), entry.DisplayWeightKg, entry.CapturedAtDeviceUtc, entry.AcceptedAtServerUtc))
            .ToArrayAsync(cancellationToken);
        CommercialReceivingVehicleSnapshot? vehicle = row.session.ReceivingVehicleId is null ? null : new(row.session.ReceivingVehicleId.Value, row.session.ReceivingVehicleVersionSnapshot!.Value, row.session.VehicleCodeSnapshot!, row.session.VehicleRegistrationSnapshot!, row.session.VehicleDisplayNameSnapshot);
        return new CommercialReceivingLiveView(row.session.Id, row.session.CloudReference, row.session.ExternalReference, row.session.Status.ToString(), row.session.Version,
            new(row.session.SupplierId, row.session.SupplierVersionSnapshot, row.session.SupplierCodeSnapshot, row.session.SupplierNameSnapshot),
            new(row.session.DestinationLocationId, row.session.DestinationLocationVersionSnapshot, row.session.DestinationLocationCodeSnapshot, row.session.DestinationLocationNameSnapshot),
            new(row.session.WeightProcessingPolicyId, row.session.WeightPolicyVersionSnapshot, row.session.WeightDecimalPlacesSnapshot, row.session.WeightProcessingMethodSnapshot.ToString()),
            vehicle, row.session.EntryCount, DecimalText(row.session.ProcessedTotalWeightKg), row.ownership.EditorDeviceId,
            row.ownership.OwnershipGeneration, row.ownership.LeaseExpiresAtUtc, LeaseHealth(row.session, row.ownership), Later(row.session.UpdatedAtUtc, row.ownership.UpdatedAtUtc), row.session.SubmittedAtUtc, entries,
            LeaseAttention(row.session, row.ownership));
    }

    public async Task<CommercialEventCursorResult> ReadEventsAsync(CommercialEventCursorQuery query, CancellationToken cancellationToken)
    {
        RequireContext();
        if (query.Limit is < 1 or > 100) throw Problem("COMMERCIAL_SYNC_CURSOR_INVALID", "Limit must be between 1 and 100.", ApplicationErrorCategory.Validation);
        var cursor = Decode<EventCursor>(query.Cursor, "COMMERCIAL_SYNC_CURSOR_INVALID");
        if (cursor is not null && (cursor.WorkspaceId != current.WorkspaceId || cursor.CompanyId != current.CompanyId || cursor.DeviceId != current.DeviceId || cursor.Role != current.Role.ToString()))
            throw Problem("COMMERCIAL_SYNC_CURSOR_INVALID", "The commercial event cursor is invalid for this authority.", ApplicationErrorCategory.Validation);
        var after = cursor?.After ?? 0;
        var rows = await (from message in dbContext.OutboxMessages.AsNoTracking()
                          join audience in dbContext.CommercialOutboxAudiences.AsNoTracking()
                              on message.Id equals audience.OutboxMessageId
                          where message.EventStream == OutboxEventStream.CommercialMobileSync &&
                                audience.CompanyId == current.CompanyId && message.Sequence > after &&
                                ((current.IsOwner && audience.Audience == OutboxAudience.OwnerBroadcast) ||
                                 (audience.Audience == OutboxAudience.TargetDevice && audience.TargetDeviceId == current.DeviceId))
                          orderby message.Sequence
                          select new
                          {
                              message.Sequence,
                              message.Id,
                              message.EventType,
                              message.EventVersion,
                              message.AggregateId,
                              message.AggregateVersion,
                              audience.Audience,
                              audience.TargetDeviceId,
                              message.OccurredAtUtc,
                              message.CorrelationId,
                              message.PayloadJson,
                          })
            .Take(query.Limit + 1)
            .ToListAsync(cancellationToken);
        var hasMore = rows.Count > query.Limit;
        var page = rows.Take(query.Limit).ToArray();
        var events = page.Select(row =>
        {
            using var document = JsonDocument.Parse(row.PayloadJson);
            return new CommercialEventCursorItem(row.Sequence, row.Id, row.EventType, row.EventVersion, row.AggregateId, row.AggregateVersion, row.Audience.ToString(), row.TargetDeviceId, row.OccurredAtUtc, row.CorrelationId, document.RootElement.Clone());
        }).ToArray();
        var next = page.Length == 0 ? after : page[^1].Sequence;
        return new CommercialEventCursorResult(events, Encode(new EventCursor(1, current.WorkspaceId, current.CompanyId, current.DeviceId, current.Role.ToString(), next)), hasMore);
    }

    public async Task<CommercialMasterCursorResult> ReadMastersAsync(CommercialMasterCursorQuery query, CancellationToken cancellationToken)
    {
        RequireContext();
        if (query.Limit is < 1 or > 100) throw Problem("COMMERCIAL_MASTER_CURSOR_INVALID", "Limit must be between 1 and 100.", ApplicationErrorCategory.Validation);
        var cursor = Decode<MasterCursor>(query.Cursor, "COMMERCIAL_MASTER_CURSOR_INVALID");
        if (cursor is not null && (cursor.WorkspaceId != current.WorkspaceId || cursor.CompanyId != current.CompanyId))
            throw Problem("COMMERCIAL_MASTER_CURSOR_INVALID", "The commercial master cursor is invalid for this company.", ApplicationErrorCategory.Validation);
        var after = cursor?.After ?? 0;
        var highWater = cursor?.HighWater;
        if (highWater is null)
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken);
            var scope = $"TraderPro.CommercialMasterSync.CommitOrder.v1\n{current.WorkspaceId:D}\n{current.CompanyId:D}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))", cancellationToken);
            highWater = await dbContext.CommercialMasterChanges.Where(change => change.CompanyId == current.CompanyId).MaxAsync(change => (long?)change.Sequence, cancellationToken) ?? after;
            await transaction.CommitAsync(cancellationToken);
        }
        var rows = await dbContext.CommercialMasterChanges.AsNoTracking().Where(change => change.CompanyId == current.CompanyId && change.Sequence > after && change.Sequence <= highWater)
            .OrderBy(change => change.Sequence).Take(query.Limit + 1).ToListAsync(cancellationToken);
        var hasMore = rows.Count > query.Limit;
        var page = rows.Take(query.Limit).ToArray();
        var changes = page.Select(row =>
        {
            using var document = JsonDocument.Parse(row.PayloadJson);
            return new CommercialMasterCursorItem(row.Sequence, row.MasterType, row.MasterId, row.MasterVersion, row.Status, row.OccurredAtUtc, document.RootElement.Clone());
        }).ToArray();
        var nextAfter = page.Length == 0 ? after : page[^1].Sequence;
        var nextHighWater = !hasMore && nextAfter >= highWater ? null : highWater;
        return new CommercialMasterCursorResult(changes, Encode(new MasterCursor(1, current.WorkspaceId, current.CompanyId, nextAfter, nextHighWater)), highWater.Value, hasMore);
    }

    private string LeaseHealth(CommercialReceivingSession session, CommercialReceivingOwnership ownership)
    {
        if (session.Status is CommercialReceivingStatus.SubmittedForSettlementReview) return "Closed";
        if (ownership.LeaseExpiresAtUtc is null) return "AcquisitionRequired";
        if (ownership.LeaseExpiresAtUtc <= clock.UtcNow) return "Expired";
        if (ownership.LeaseExpiresAtUtc <= clock.UtcNow.AddMinutes(CommercialReceivingOptions.HeartbeatTargetMinutes)) return "Expiring";
        return "Healthy";
    }

    private string? LeaseAttention(
        CommercialReceivingSession session,
        CommercialReceivingOwnership ownership)
    {
        if (session.Status is CommercialReceivingStatus.SubmittedForSettlementReview)
        {
            return null;
        }

        if (ownership.LeaseExpiresAtUtc is null)
        {
            return "LeaseAcquisitionRequired";
        }

        return ownership.LeaseExpiresAtUtc <= clock.UtcNow
            ? "LeaseReacquisitionRequired"
            : null;
    }

    private static DateTimeOffset Later(DateTimeOffset left, DateTimeOffset right) =>
        left >= right ? left : right;

    private static string Encode<T>(T payload)
    {
        var value = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
    private static T? Decode<T>(string? cursor, string code) where T : class
    {
        if (cursor is null) return null;
        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var result = JsonSerializer.Deserialize<T>(Convert.FromBase64String(base64), JsonOptions);
            var version = result?.GetType().GetProperty("Version")?.GetValue(result);
            if (result is null || version is not int value || value != 1) throw new FormatException();
            return result;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw Problem(code, "The opaque cursor is invalid.", ApplicationErrorCategory.Validation);
        }
    }

    private sealed record ListCursor(int Version, Guid WorkspaceId, Guid CompanyId, Guid DeviceId, bool IsOwner, string? Status, string? Search, DateTimeOffset LastCloudUpdateUtc, string CloudReference);
    private sealed record EventCursor(int Version, Guid WorkspaceId, Guid CompanyId, Guid DeviceId, string Role, long After);
    private sealed record MasterCursor(int Version, Guid WorkspaceId, Guid CompanyId, long After, long? HighWater);
}
