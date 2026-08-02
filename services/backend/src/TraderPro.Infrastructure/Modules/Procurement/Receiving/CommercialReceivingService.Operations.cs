using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Catalog;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Operations;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Receiving;
using TraderPro.Domain.Procurement.Suppliers;
using TraderPro.Infrastructure.Modules.Shared;

namespace TraderPro.Infrastructure.Modules.Procurement.Receiving;

internal sealed partial class CommercialReceivingService
{
    private async Task<CommercialReceivingCloudState> StartAsync(
        CommercialMobileOperationCommand operation,
        StartCommercialReceivingSessionPayload payload,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var requestedMode = Enum.Parse<VehicleSelectionMode>(
            payload.VehicleSelectionMode,
            false);
        await AcquireReferenceSeriesLockAsync(cancellationToken);
        var reservation = await dbContext.CommercialReceivingReferenceReservations
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_reservations WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND operation_id = {operation.OperationId} AND session_id = {operation.SessionId} AND request_hash = {requestHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ??
            throw Problem(
                "RECEIVING_REFERENCE_CONFLICT",
                "The Start operation does not own a valid reference reservation.",
                ApplicationErrorCategory.Conflict);
        var referencePolicy = await dbContext.CommercialReceivingReferencePolicies
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_policies WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND id = {reservation.PolicyId} FOR SHARE")
            .SingleOrDefaultAsync(cancellationToken);
        var referenceCounter = await dbContext.CommercialReceivingReferenceCounters
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_counters WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND policy_id = {reservation.PolicyId} AND period_key = {reservation.PeriodKey} FOR SHARE")
            .SingleOrDefaultAsync(cancellationToken);
        if (referencePolicy is null ||
            referencePolicy.DocumentType !=
            CommercialReceivingReferencePolicy.DocumentTypeValue ||
            referenceCounter is null ||
            referenceCounter.NextNumber <= reservation.Sequence)
        {
            throw Problem(
                "RECEIVING_REFERENCE_CONFLICT",
                "The Start reference reservation no longer has valid policy ownership.",
                ApplicationErrorCategory.Conflict);
        }

        await LockSessionAsync(operation.SessionId, cancellationToken);
        if (await dbContext.CommercialReceivingSessions.AnyAsync(item => item.Id == operation.SessionId, cancellationToken))
        {
            throw Problem("RECEIVING_REFERENCE_CONFLICT", "The Receiving Session ID is already in use.", ApplicationErrorCategory.Conflict);
        }

        await PostgreSqlProcurementDefaultsLock.AcquireAsync(
            dbContext,
            current.WorkspaceId,
            current.CompanyId,
            cancellationToken);
        await LockStartMastersAsync(payload, cancellationToken);
        var settings = await dbContext.CompanyProcurementSettings.SingleOrDefaultAsync(
            item => item.CompanyId == current.CompanyId && item.Id == payload.CompanyProcurementSettingsId,
            cancellationToken) ?? throw Problem("RECEIVING_SETTINGS_NOT_CONFIGURED", "Procurement settings are not configured.", ApplicationErrorCategory.Conflict);
        if (settings.Version != payload.ProcurementSettingsVersion)
            throw Problem("RECEIVING_SETTINGS_VERSION_STALE", "The Procurement Settings version is stale.", ApplicationErrorCategory.Conflict);
        if (settings.DefaultBranchId != current.DefaultBranchId || settings.VehicleSelectionMode != requestedMode)
            throw Problem("RECEIVING_SETTINGS_VERSION_STALE", "The captured Procurement Settings facts do not match.", ApplicationErrorCategory.Conflict);

        var supplier = await dbContext.Suppliers.SingleOrDefaultAsync(
            item => item.CompanyId == current.CompanyId && item.Id == payload.SupplierId,
            cancellationToken) ?? throw Problem("RECEIVING_SUPPLIER_INACTIVE", "The captured Supplier is unavailable.", ApplicationErrorCategory.Conflict);
        RequireActiveVersion(supplier.Status, supplier.Version, payload.SupplierVersion, "RECEIVING_SUPPLIER_INACTIVE", "RECEIVING_SUPPLIER_VERSION_STALE", "Supplier");

        var destination = await dbContext.BusinessLocations.SingleOrDefaultAsync(
            item => item.CompanyId == current.CompanyId && item.BranchId == current.DefaultBranchId && item.Id == payload.DestinationLocationId,
            cancellationToken) ?? throw Problem("RECEIVING_DESTINATION_INACTIVE", "The captured destination is unavailable.", ApplicationErrorCategory.Conflict);
        RequireActiveVersion(destination.Status, destination.Version, payload.DestinationLocationVersion, "RECEIVING_DESTINATION_INACTIVE", "RECEIVING_MASTER_NEEDS_ATTENTION", "destination");
        if (settings.DefaultDestinationLocationId != destination.Id)
            throw Problem("RECEIVING_DEFAULT_DESTINATION_MISMATCH", "The destination is not the configured default.", ApplicationErrorCategory.Conflict);

        var weightPolicy = await dbContext.WeightProcessingPolicies.SingleOrDefaultAsync(
            item => item.CompanyId == current.CompanyId && item.Id == payload.WeightProcessingPolicyId,
            cancellationToken) ?? throw Problem("RECEIVING_WEIGHT_POLICY_INACTIVE", "The captured Weight Policy is unavailable.", ApplicationErrorCategory.Conflict);
        RequireActiveVersion(weightPolicy.Status, weightPolicy.Version, payload.WeightProcessingPolicyVersion, "RECEIVING_WEIGHT_POLICY_INACTIVE", "RECEIVING_MASTER_NEEDS_ATTENTION", "Weight Policy");
        if (settings.DefaultWeightProcessingPolicyId != weightPolicy.Id)
            throw Problem("RECEIVING_DEFAULT_POLICY_MISMATCH", "The Weight Policy is not the configured default.", ApplicationErrorCategory.Conflict);

        ReceivingVehicle? vehicle = null;
        if (settings.VehicleSelectionMode is VehicleSelectionMode.Disabled && payload.ReceivingVehicleId is not null)
            throw Problem("RECEIVING_MASTER_NEEDS_ATTENTION", "Vehicle selection is disabled by Procurement Settings.", ApplicationErrorCategory.Conflict);
        if (payload.ReceivingVehicleId is not null)
        {
            if (payload.ReceivingVehicleVersion is null or <= 0)
                throw Problem("RECEIVING_VEHICLE_INACTIVE", "A selected Vehicle requires its captured version.", ApplicationErrorCategory.Conflict);
            vehicle = await dbContext.ReceivingVehicles.SingleOrDefaultAsync(
                item => item.CompanyId == current.CompanyId && item.Id == payload.ReceivingVehicleId,
                cancellationToken) ?? throw Problem("RECEIVING_VEHICLE_INACTIVE", "The selected Vehicle is unavailable.", ApplicationErrorCategory.Conflict);
            RequireActiveVersion(vehicle.Status, vehicle.Version, payload.ReceivingVehicleVersion.Value, "RECEIVING_VEHICLE_INACTIVE", "RECEIVING_MASTER_NEEDS_ATTENTION", "Vehicle");
        }

        var now = clock.UtcNow;
        reservation.Consume(operation.SessionId, now);

        var session = DomainValue(() => CommercialReceivingSession.Start(new CommercialReceivingSessionStartFacts(
            operation.SessionId, current.WorkspaceId, current.CompanyId, current.DefaultBranchId,
            reservation.RenderedReference, reservation.Sequence, reservation.Id,
            reservation.PolicyVersion, payload.ExternalReference,
            supplier.Id, supplier.Version, supplier.Code, supplier.Name, supplier.ProductScopeMode,
            settings.Id, settings.Version, settings.VehicleSelectionMode,
            destination.Id, destination.Version, destination.Code, destination.Name,
            weightPolicy.Id, weightPolicy.Version, weightPolicy.DecimalPlaces, weightPolicy.ProcessingMethod,
            vehicle?.Id, vehicle?.Version, vehicle?.Code, vehicle?.RegistrationNumber, vehicle?.DisplayName,
            payload.StartedAtDeviceUtc, now)));
        var ownership = CommercialReceivingOwnership.Start(current.WorkspaceId, current.CompanyId, session.Id, current.DeviceId, now, options.LeaseDuration);
        dbContext.CommercialReceivingSessions.Add(session);
        dbContext.CommercialReceivingOwnerships.Add(ownership);

        AddAudit("Procurement.CommercialReceiving.SessionStarted", session.Id, null, JsonSerializer.Serialize(new
        {
            sessionId = session.Id,
            session.CloudReference,
            session.SupplierCodeSnapshot,
            destinationCode = session.DestinationLocationCodeSnapshot,
            session.Status,
            session.Version,
            editorDeviceId = ownership.EditorDeviceId,
            ownership.OwnershipGeneration,
        }, JsonOptions), now);
        AddReceivingEvents("CommercialReceivingSessionStarted", session, ownership, new
        {
            sessionId = session.Id,
            session.CloudReference,
            status = session.Status.ToString(),
            session.Version,
            supplierCode = session.SupplierCodeSnapshot,
            supplierName = session.SupplierNameSnapshot,
            destinationCode = session.DestinationLocationCodeSnapshot,
            destinationName = session.DestinationLocationNameSnapshot,
            vehicleDisplayName = session.VehicleDisplayNameSnapshot,
            editorDeviceId = ownership.EditorDeviceId,
            ownership.OwnershipGeneration,
            ownership.LeaseExpiresAtUtc,
            session.EntryCount,
            processedTotalWeightKg = DecimalText(session.ProcessedTotalWeightKg),
            updatedAtUtc = session.UpdatedAtUtc,
        }, now);
        return Cloud(session, ownership);
    }

    private async Task<CommercialReceivingCloudState> RecordEntryAsync(
        CommercialMobileOperationCommand operation,
        CancellationToken cancellationToken)
    {
        var payload = ReadPayload<RecordCommercialReceivingEntryPayload>(operation);
        RequirePayloadIdentity(operation, payload.OperationId, payload.SessionId, payload.LocalSequence);
        if (!IsUuid7(payload.EntryId))
            throw Problem("RECEIVING_SESSION_ID_INVALID", "Entry ID must be a canonical UUIDv7.", ApplicationErrorCategory.Validation);
        if (payload.CapturedAtDeviceUtc.Offset != TimeSpan.Zero || string.IsNullOrWhiteSpace(payload.WeightSource) ||
            payload.WeightSource != payload.WeightSource.Trim() || payload.WeightSource.Length > 32)
            throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "The Entry timestamp or weight source is invalid.", ApplicationErrorCategory.Validation);
        var loaded = await LoadSessionForUpdateAsync(operation.SessionId, cancellationToken);
        Domain(() => loaded.Session.RequireInProgress());
        Domain(() => loaded.Session.RequireSequence(operation.LocalSequence));
        Domain(() => loaded.Ownership.RequireLease(current.DeviceId, operation.OwnershipGeneration!.Value, operation.Lease!.LeaseId, clock.UtcNow));
        await LockEntryMastersAsync(payload, cancellationToken);

        var product = await dbContext.Products.SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId && item.Id == payload.ProductId, cancellationToken)
            ?? throw Problem("RECEIVING_PRODUCT_INACTIVE", "The captured Product is unavailable.", ApplicationErrorCategory.Conflict);
        if (product.Status is not MasterDataStatus.Active) throw Problem("RECEIVING_PRODUCT_INACTIVE", "The captured Product is inactive.", ApplicationErrorCategory.Conflict);
        if (!product.IsPurchasable) throw Problem("RECEIVING_PRODUCT_NOT_PURCHASABLE", "The captured Product is not purchasable.", ApplicationErrorCategory.Conflict);
        if (product.Version != payload.ProductVersion) throw Problem("RECEIVING_PRODUCT_VERSION_STALE", "The captured Product version is stale.", ApplicationErrorCategory.Conflict);

        SupplierProductScope? scope = null;
        if (loaded.Session.SupplierProductScopeModeSnapshot is SupplierProductScopeMode.Restricted)
        {
            if (payload.SupplierProductScopeId is null || payload.SupplierProductScopeVersion is null)
                throw Problem("RECEIVING_PRODUCT_SCOPE_REQUIRED", "The Restricted Supplier requires Product scope evidence.", ApplicationErrorCategory.Conflict);
            scope = await dbContext.SupplierProductScopes.SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId && item.Id == payload.SupplierProductScopeId && item.SupplierId == loaded.Session.SupplierId && item.ProductId == product.Id, cancellationToken);
            if (scope is null || scope.Status is not MasterDataStatus.Active)
                throw Problem("RECEIVING_PRODUCT_SCOPE_INVALID", "The captured Supplier Product scope is not active.", ApplicationErrorCategory.Conflict);
            if (scope.Version != payload.SupplierProductScopeVersion)
                throw Problem("RECEIVING_PRODUCT_SCOPE_INVALID", "The captured Supplier Product scope version is stale.", ApplicationErrorCategory.Conflict);
        }
        else if (payload.SupplierProductScopeId is not null || payload.SupplierProductScopeVersion is not null)
            throw Problem("RECEIVING_PRODUCT_SCOPE_INVALID", "An Unrestricted Supplier does not require Product scope evidence.", ApplicationErrorCategory.Conflict);

        var bag = await dbContext.BagTypes.SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId && item.Id == payload.BagTypeId, cancellationToken)
            ?? throw Problem("RECEIVING_BAG_TYPE_INACTIVE", "The captured Bag Type is unavailable.", ApplicationErrorCategory.Conflict);
        RequireActiveVersion(bag.Status, bag.Version, payload.BagTypeVersion, "RECEIVING_BAG_TYPE_INACTIVE", "RECEIVING_BAG_TYPE_VERSION_STALE", "Bag Type");

        ProductStandardBagWeight? standard = null;
        if (payload.ProductStandardBagWeightId is not null)
        {
            if (payload.ProductStandardBagWeightVersion is null or <= 0)
                throw Problem("RECEIVING_STANDARD_BAG_WEIGHT_VERSION_STALE", "The selected Standard Bag Weight requires its version.", ApplicationErrorCategory.Conflict);
            standard = await dbContext.ProductStandardBagWeights.SingleOrDefaultAsync(item => item.CompanyId == current.CompanyId && item.Id == payload.ProductStandardBagWeightId && item.ProductId == product.Id && item.BagTypeId == bag.Id, cancellationToken);
            if (standard is null || standard.Status is not MasterDataStatus.Active)
                throw Problem("RECEIVING_STANDARD_BAG_WEIGHT_INVALID", "The selected Standard Bag Weight is invalid.", ApplicationErrorCategory.Conflict);
            if (standard.Version != payload.ProductStandardBagWeightVersion)
                throw Problem("RECEIVING_STANDARD_BAG_WEIGHT_VERSION_STALE", "The selected Standard Bag Weight version is stale.", ApplicationErrorCategory.Conflict);
        }
        else if (payload.ProductStandardBagWeightVersion is not null)
            throw Problem("RECEIVING_STANDARD_BAG_WEIGHT_INVALID", "Standard Bag Weight identity and version must be supplied together.", ApplicationErrorCategory.Conflict);

        if (!Enum.TryParse<WeightProcessingMethod>(payload.ProcessingMethod, false, out var method) ||
            method != loaded.Session.WeightProcessingMethodSnapshot || payload.DecimalPlaces != loaded.Session.WeightDecimalPlacesSnapshot)
            throw Problem("RECEIVING_WEIGHT_PROCESSING_MISMATCH", "The Entry processing policy does not match the Session snapshot.", ApplicationErrorCategory.Validation);
        WeightProcessingResult processed;
        try { processed = WeightProcessor.Process(payload.RawWeightKg, payload.DecimalPlaces, payload.ProcessingMethod); }
        catch (WeightProcessingException exception) { throw Problem("RECEIVING_WEIGHT_PROCESSING_MISMATCH", "The Entry weight could not be processed exactly.", ApplicationErrorCategory.Validation, inner: exception); }
        if (!string.Equals(processed.ProcessedWeightKg, payload.ProcessedWeightKg, StringComparison.Ordinal) ||
            !string.Equals(processed.DisplayWeightKg, payload.DisplayWeightKg, StringComparison.Ordinal))
            throw Problem("RECEIVING_WEIGHT_PROCESSING_MISMATCH", "The submitted processed/display weight does not match the server result.", ApplicationErrorCategory.Validation);
        var processedDecimal = decimal.Parse(processed.ProcessedWeightKg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        if (processedDecimal <= 0m || payload.BagCount <= 0)
            throw Problem("RECEIVING_TOTAL_WEIGHT_REQUIRED", "Entry raw/processed weight and bag count must be positive.", ApplicationErrorCategory.Validation);

        var now = clock.UtcNow;
        var entry = DomainValue(() => CommercialReceivingEntry.Create(new CommercialReceivingEntryFacts(
            payload.EntryId, current.WorkspaceId, current.CompanyId, loaded.Session.Id, operation.OperationId, operation.LocalSequence,
            product.Id, product.Version, product.Code, product.Name, product.ProductType, product.ProcessingFamilyCode,
            scope?.Id, scope?.Version, loaded.Session.SupplierProductScopeModeSnapshot,
            scope is null ? "Unrestricted" : "RestrictedScopeValidated",
            bag.Id, bag.Version, bag.Code, bag.Name, bag.ConstructionClass, bag.StandardTareWeightKg, bag.IsReturnable,
            standard?.Id, standard?.Version, standard?.Label, standard?.StandardContentWeightKg,
            payload.BagCount, payload.RawWeightKg, processedDecimal, processed.DisplayWeightKg,
            payload.DecimalPlaces, method, payload.WeightSource, payload.CapturedAtDeviceUtc, now)));
        Domain(() => loaded.Session.AcceptEntry(entry, now));
        dbContext.CommercialReceivingEntries.Add(entry);
        AddAudit("Procurement.CommercialReceiving.EntryAccepted", loaded.Session.Id, null, JsonSerializer.Serialize(new
        {
            sessionId = loaded.Session.Id,
            entryId = entry.Id,
            entry.LocalSequence,
            entry.ProductCodeSnapshot,
            entry.BagTypeCodeSnapshot,
            entry.BagCount,
            entry.RawWeightKg,
            processedWeightKg = DecimalText(entry.ProcessedWeightKg),
            loaded.Session.EntryCount,
            processedTotalWeightKg = DecimalText(loaded.Session.ProcessedTotalWeightKg),
            loaded.Session.Version,
        }, JsonOptions), now);
        AddReceivingEvents("CommercialReceivingEntryAccepted", loaded.Session, loaded.Ownership, new
        {
            sessionId = loaded.Session.Id,
            entryId = entry.Id,
            entry.LocalSequence,
            productCode = entry.ProductCodeSnapshot,
            productName = entry.ProductNameSnapshot,
            bagTypeCode = entry.BagTypeCodeSnapshot,
            bagTypeName = entry.BagTypeNameSnapshot,
            entry.BagCount,
            entry.RawWeightKg,
            processedWeightKg = DecimalText(entry.ProcessedWeightKg),
            entry.DisplayWeightKg,
            entry.CapturedAtDeviceUtc,
            entry.AcceptedAtServerUtc,
            loaded.Session.EntryCount,
            processedTotalWeightKg = DecimalText(loaded.Session.ProcessedTotalWeightKg),
            loaded.Session.Version,
        }, now);
        return Cloud(loaded.Session, loaded.Ownership);
    }

    private async Task<CommercialReceivingCloudState> SubmitAsync(
        CommercialMobileOperationCommand operation,
        CancellationToken cancellationToken)
    {
        var payload = ReadPayload<SubmitCommercialReceivingSessionPayload>(operation);
        RequirePayloadIdentity(operation, payload.OperationId, payload.SessionId, payload.LocalSequence);
        if (payload.SubmittedAtDeviceUtc.Offset != TimeSpan.Zero)
            throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "The submitted Device timestamp must be UTC.", ApplicationErrorCategory.Validation);
        var loaded = await LoadSessionForUpdateAsync(operation.SessionId, cancellationToken);
        Domain(() => loaded.Ownership.RequireLease(current.DeviceId, operation.OwnershipGeneration!.Value, operation.Lease!.LeaseId, clock.UtcNow));
        var now = clock.UtcNow;
        Domain(() => loaded.Session.Submit(operation.LocalSequence, now));
        loaded.Ownership.Close(now);
        AddAudit("Procurement.CommercialReceiving.SessionSubmitted", loaded.Session.Id, null, JsonSerializer.Serialize(new
        {
            sessionId = loaded.Session.Id,
            loaded.Session.CloudReference,
            status = loaded.Session.Status.ToString(),
            loaded.Session.SubmittedAtUtc,
            loaded.Session.EntryCount,
            processedTotalWeightKg = DecimalText(loaded.Session.ProcessedTotalWeightKg),
            loaded.Session.Version,
        }, JsonOptions), now);
        AddReceivingEvents("CommercialReceivingSessionSubmitted", loaded.Session, loaded.Ownership, new
        {
            sessionId = loaded.Session.Id,
            loaded.Session.CloudReference,
            status = loaded.Session.Status.ToString(),
            loaded.Session.SubmittedAtUtc,
            loaded.Session.EntryCount,
            processedTotalWeightKg = DecimalText(loaded.Session.ProcessedTotalWeightKg),
            loaded.Session.Version,
            ownershipClosed = true,
        }, now);
        return Cloud(loaded.Session, loaded.Ownership);
    }

    private static void RequireActiveVersion(MasterDataStatus status, long version, long suppliedVersion, string inactiveCode, string staleCode, string kind)
    {
        if (status is not MasterDataStatus.Active) throw Problem(inactiveCode, $"The captured {kind} is inactive.", ApplicationErrorCategory.Conflict);
        if (version != suppliedVersion) throw Problem(staleCode, $"The captured {kind} version is stale.", ApplicationErrorCategory.Conflict);
    }

    private T DomainValue<T>(Func<T> action)
    {
        try { return action(); }
        catch (CommercialReceivingDomainException exception) { throw FromDomain(exception); }
    }

    private void AddAudit(
        string action,
        Guid aggregateId,
        string? reason,
        string? after,
        DateTimeOffset now,
        string aggregateType = "Procurement.CommercialReceivingSession") =>
        dbContext.AuditEvents.Add(AuditEvent.Create(current.WorkspaceId, current.CompanyId, current.DefaultBranchId, current.UserId, current.DeviceId, action, aggregateType, aggregateId, null, reason, null, after, current.CorrelationId, now));

    private void AddReceivingEvents(string eventType, CommercialReceivingSession session, CommercialReceivingOwnership ownership, object payload, DateTimeOffset now)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        AddReceivingEvent(eventType, session, json, OutboxAudience.OwnerBroadcast, null, now);
        AddReceivingEvent(eventType, session, json, OutboxAudience.TargetDevice, ownership.EditorDeviceId, now);
    }

    private void AddReceivingEvent(
        string eventType,
        CommercialReceivingSession session,
        string payloadJson,
        OutboxAudience audience,
        Guid? targetDeviceId,
        DateTimeOffset now)
    {
        var message = OutboxMessage.Create(
            current.WorkspaceId,
            eventType,
            1,
            "CommercialReceivingSession",
            session.Id,
            session.Version,
            payloadJson,
            current.CorrelationId,
            now,
            OutboxEventStream.CommercialMobileSync);
        dbContext.OutboxMessages.Add(message);
        dbContext.CommercialOutboxAudiences.Add(
            CommercialOutboxAudience.Create(
                message,
                current.CompanyId,
                audience,
                targetDeviceId));
    }

    private static CommercialReceivingCloudState Cloud(CommercialReceivingSession session, CommercialReceivingOwnership ownership) =>
        new(session.CloudReference, session.Status.ToString(), session.Version, ownership.OwnershipGeneration, ownership.LeaseId, ownership.LeaseExpiresAtUtc, session.EntryCount, DecimalText(session.ProcessedTotalWeightKg));
}
