using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Platform.Identity;
using TraderPro.Application.Procurement.Receiving;
using TraderPro.Domain.Common.MasterData;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.MasterData;
using TraderPro.Domain.Procurement.Receiving;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;

namespace TraderPro.Infrastructure.Modules.Procurement.Receiving;

internal sealed partial class CommercialReceivingService(
    TraderProDbContext dbContext,
    IAuthenticatedTraderProContext current,
    IClock clock,
    PostgreSqlIdempotentCommandExecutor idempotency,
    CommercialReceivingOptions options) : ICommercialReceivingService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<CommercialMobileOperationsResult> ProcessOperationsAsync(
        CommercialMobileOperationsCommand command,
        CancellationToken cancellationToken)
    {
        RequireOperatorContext();
        if (command.Operations is null || command.Operations.Count is < 1 or > 50)
        {
            throw Problem(
                "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                "A commercial mobile batch must contain between 1 and 50 operations.",
                ApplicationErrorCategory.Validation);
        }

        foreach (var operation in command.Operations)
        {
            if (!IsUuid7(operation.OperationId) || !IsUuid7(operation.SessionId) ||
                operation.LocalSequence <= 0 || string.IsNullOrWhiteSpace(operation.OperationType))
            {
                throw Problem(
                    "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                    "Every operation must have canonical UUIDv7 identities and a positive local sequence.",
                    ApplicationErrorCategory.Validation);
            }
        }

        var blocked = new HashSet<Guid>();
        var results = new List<CommercialMobileOperationResult>(command.Operations.Count);
        foreach (var operation in command.Operations)
        {
            if (blocked.Contains(operation.SessionId))
            {
                results.Add(Failed(
                    operation,
                    CommercialMobileOperationStatus.NeedsAttention,
                    "RECEIVING_OPERATION_WAITING_FOR_PRIOR_SEQUENCE",
                    "A prior operation blocked this Receiving Session in the batch.",
                    retryable: true,
                    requiresAction: true));
                continue;
            }

            try
            {
                results.Add(await ProcessOperationAsync(operation, cancellationToken));
            }
            catch (ApplicationProblemException exception)
            {
                var status = IsNeedsAttention(exception.Code) || exception.Retryable
                    ? CommercialMobileOperationStatus.NeedsAttention
                    : CommercialMobileOperationStatus.Rejected;
                results.Add(Failed(
                    operation,
                    status,
                    exception.Code,
                    exception.Message,
                    exception.Retryable,
                    status is CommercialMobileOperationStatus.NeedsAttention));
                blocked.Add(operation.SessionId);
            }
        }

        return new CommercialMobileOperationsResult(results);
    }

    private async Task<CommercialMobileOperationResult> ProcessOperationAsync(
        CommercialMobileOperationCommand operation,
        CancellationToken cancellationToken)
    {
        ValidateEnvelope(operation);
        ValidateImmutablePayloadShape(operation.PayloadJson);
        var actualPayloadHash = CommercialReceivingRequestHash.PayloadHash(operation.PayloadJson);
        if (!string.Equals(actualPayloadHash, operation.PayloadHash, StringComparison.Ordinal) ||
            operation.PayloadHash.Length != 64 ||
            operation.PayloadHash.Any(character => char.IsAsciiLetterUpper(character)))
        {
            throw Problem(
                "COMMERCIAL_MOBILE_OPERATION_PAYLOAD_HASH_INVALID",
                "The payload hash does not match the exact UTF-8 payload JSON.",
                ApplicationErrorCategory.Validation);
        }

        var requestHash = CommercialReceivingRequestHash.ForMobileOperation(
            operation.OperationType,
            operation.OperationId,
            operation.SessionId,
            operation.LocalSequence,
            operation.OwnershipGeneration,
            current.WorkspaceId,
            current.CompanyId,
            current.UserId,
            current.DeviceId,
            operation.PayloadHash,
            operation.PayloadJson);

        await ClaimOperationAsync(operation, requestHash, cancellationToken);

        try
        {
            StartCommercialReceivingSessionPayload? startPayload = null;
            if (operation.OperationType == CommercialReceivingOperationTypes.Start)
            {
                startPayload = ReadPayload<StartCommercialReceivingSessionPayload>(operation);
                ValidateStartPayload(operation, startPayload);
                await EnsureReferenceReservationAsync(operation, requestHash, cancellationToken);
            }

            var result = await idempotency.ExecuteAsync(
                current.WorkspaceId,
                CommercialReceivingOperationTypes.SharedCommandScope,
                operation.OperationId.ToString("D"),
                requestHash,
                current.CorrelationId,
                200,
                async () =>
                {
                    await AcquireOperationClaimLockAsync(
                        operation.OperationId,
                        cancellationToken);
                    var claim = await LoadOperationClaimForUpdateAsync(
                        operation.OperationId,
                        cancellationToken);
                    EnsureClaimIdentity(claim, operation, requestHash);
                    if (claim.State is CommercialReceivingOperationClaimState.Rejected)
                    {
                        throw Problem(
                            claim.AttentionCode ?? "COMMERCIAL_MOBILE_OPERATION_REJECTED",
                            claim.AttentionMessage ?? "The operation was permanently rejected.",
                            ApplicationErrorCategory.Conflict,
                            claim.Retryable);
                    }

                    if (claim.State is CommercialReceivingOperationClaimState.Completed)
                    {
                        throw Problem(
                            "IDEMPOTENCY_IN_PROGRESS",
                            "The completed operation claim is awaiting replay reconciliation.",
                            ApplicationErrorCategory.Conflict,
                            retryable: true);
                    }

                    if (claim.State is CommercialReceivingOperationClaimState.NeedsAttention)
                    {
                        claim.BeginRetry(clock.UtcNow);
                    }

                    var cloud = operation.OperationType switch
                    {
                        CommercialReceivingOperationTypes.Start =>
                            await StartAsync(
                                operation,
                                startPayload!,
                                requestHash,
                                cancellationToken),
                        CommercialReceivingOperationTypes.RecordEntry =>
                            await RecordEntryAsync(operation, cancellationToken),
                        CommercialReceivingOperationTypes.Submit =>
                            await SubmitAsync(operation, cancellationToken),
                        _ => throw Problem(
                            "COMMERCIAL_MOBILE_OPERATION_TYPE_UNSUPPORTED",
                            "The commercial mobile operation type is unsupported.",
                            ApplicationErrorCategory.Validation),
                    };
                    claim.Complete(clock.UtcNow);
                    return cloud;
                },
                IsReplayableCloudState,
                concurrencyProblem: null,
                persistenceProblem: MapPersistence,
                OutboxCommitOrdering.CommercialMobileSync,
                current.CompanyId,
                cancellationToken);
            await CompleteClaimIfNeededAsync(
                operation.OperationId,
                requestHash,
                cancellationToken);

            return new CommercialMobileOperationResult(
                operation.OperationId,
                operation.SessionId,
                operation.LocalSequence,
                result.IdempotencyStatus is IdempotencyExecutionStatus.PreviouslyProcessed
                    ? CommercialMobileOperationStatus.PreviouslyProcessed
                    : CommercialMobileOperationStatus.Accepted,
                result.Result,
                null);
        }
        catch (ApplicationProblemException exception)
        {
            await RecordClaimFailureAsync(operation.OperationId, requestHash, exception, cancellationToken);
            throw;
        }
    }

    private void ValidateEnvelope(CommercialMobileOperationCommand operation)
    {
        if (operation.ExpectedCloudVersion is not null)
        {
            throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "Ordered mobile operations must not include an expected cloud version.", ApplicationErrorCategory.Validation);
        }

        switch (operation.OperationType)
        {
            case CommercialReceivingOperationTypes.Start:
                if (operation.LocalSequence != 1 || operation.OwnershipGeneration is not null || operation.Lease is not null)
                {
                    throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "Start must be local sequence 1 without ownership generation or lease metadata.", ApplicationErrorCategory.Validation);
                }
                break;
            case CommercialReceivingOperationTypes.RecordEntry:
            case CommercialReceivingOperationTypes.Submit:
                if (operation.OwnershipGeneration is null or <= 0 || operation.Lease is null || operation.Lease.LeaseId == Guid.Empty)
                {
                    throw Problem("RECEIVING_LEASE_REQUIRED", "Record and Submit require ownership generation and current lease metadata.", ApplicationErrorCategory.Conflict);
                }
                break;
            default:
                throw Problem("COMMERCIAL_MOBILE_OPERATION_TYPE_UNSUPPORTED", "The commercial mobile operation type is unsupported.", ApplicationErrorCategory.Validation);
        }
    }

    private static T ReadPayload<T>(CommercialMobileOperationCommand operation)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(operation.PayloadJson, JsonOptions) ??
                throw new JsonException();
        }
        catch (JsonException exception)
        {
            throw Problem(
                "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                "The operation payload JSON is invalid.",
                ApplicationErrorCategory.Validation,
                inner: exception);
        }
    }

    private static void ValidateImmutablePayloadShape(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind is not JsonValueKind.Object)
            {
                throw new JsonException();
            }

            ValidateImmutablePayloadElement(document.RootElement);
        }
        catch (JsonException)
        {
            // Malformed or non-object JSON cannot contain a successfully
            // parsed forbidden capability. Typed deserialization after the
            // durable claim returns the stable validation failure and seals
            // the claim as Rejected without allocating a reference.
        }
    }

    private static void ValidateImmutablePayloadElement(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("lease", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("leaseId", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("leaseExpiresAtUtc", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("expectedCloudVersion", StringComparison.OrdinalIgnoreCase))
                {
                    throw Problem(
                        "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                        "Lease and expected-cloud-version metadata must not be stored in immutable payload JSON.",
                        ApplicationErrorCategory.Validation);
                }

                ValidateImmutablePayloadElement(property.Value);
            }

            return;
        }

        if (element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ValidateImmutablePayloadElement(item);
            }
        }
    }

    private static void RequirePayloadIdentity(
        CommercialMobileOperationCommand operation,
        Guid operationId,
        Guid sessionId,
        long localSequence)
    {
        if (operationId != operation.OperationId || sessionId != operation.SessionId || localSequence != operation.LocalSequence)
        {
            throw Problem("COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID", "Envelope and payload identities must match exactly.", ApplicationErrorCategory.Validation);
        }
    }

    private static void ValidateStartPayload(
        CommercialMobileOperationCommand operation,
        StartCommercialReceivingSessionPayload payload)
    {
        RequirePayloadIdentity(
            operation,
            payload.OperationId,
            payload.SessionId,
            payload.LocalSequence);
        if (!IsUuid7(payload.OperationId) ||
            !IsUuid7(payload.SessionId) ||
            !IsUuid7(payload.SupplierId) ||
            !IsUuid7(payload.CompanyProcurementSettingsId) ||
            !IsUuid7(payload.DestinationLocationId) ||
            !IsUuid7(payload.WeightProcessingPolicyId) ||
            (payload.ReceivingVehicleId is not null &&
             !IsUuid7(payload.ReceivingVehicleId.Value)) ||
            payload.SupplierVersion <= 0 ||
            payload.ProcurementSettingsVersion <= 0 ||
            payload.DestinationLocationVersion <= 0 ||
            payload.WeightProcessingPolicyVersion <= 0 ||
            (payload.ReceivingVehicleId is null) !=
            (payload.ReceivingVehicleVersion is null) ||
            payload.ReceivingVehicleVersion is <= 0 ||
            payload.StartedAtDeviceUtc == default ||
            payload.StartedAtDeviceUtc.Offset != TimeSpan.Zero ||
            !Enum.TryParse<VehicleSelectionMode>(
                payload.VehicleSelectionMode,
                false,
                out var requestedMode) ||
            !Enum.IsDefined(requestedMode) ||
            (requestedMode is VehicleSelectionMode.Disabled &&
             payload.ReceivingVehicleId is not null) ||
            (payload.ExternalReference is not null &&
             (string.IsNullOrWhiteSpace(payload.ExternalReference) ||
              payload.ExternalReference != payload.ExternalReference.Trim() ||
              payload.ExternalReference.Length >
              CommercialReceivingSession.MaximumExternalReferenceLength)))
        {
            throw Problem(
                "COMMERCIAL_MOBILE_OPERATION_BATCH_INVALID",
                "The immutable Start payload is structurally invalid.",
                ApplicationErrorCategory.Validation);
        }
    }

    private async Task LockSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var scope = $"TraderPro.CommercialReceiving.Session.v1\n{current.WorkspaceId:D}\n{current.CompanyId:D}\n{sessionId:D}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))",
            cancellationToken);
    }

    private async Task<(CommercialReceivingSession Session, CommercialReceivingOwnership Ownership)>
        LoadSessionForUpdateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        await LockSessionAsync(sessionId, cancellationToken);
        var session = await dbContext.CommercialReceivingSessions
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_sessions WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND id = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ?? throw Problem("RECEIVING_SESSION_NOT_FOUND", "The Receiving Session was not found.", ApplicationErrorCategory.NotFound);
        var ownership = await dbContext.CommercialReceivingOwnerships
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_ownerships WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND receiving_session_id = {sessionId} FOR UPDATE")
            .SingleAsync(cancellationToken);
        return (session, ownership);
    }

    private async Task EnsureReferenceReservationAsync(
        CommercialMobileOperationCommand operation,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireOperationClaimLockAsync(operation.OperationId, cancellationToken);
        var claim = await LoadOperationClaimForUpdateAsync(
            operation.OperationId,
            cancellationToken);
        EnsureClaimIdentity(claim, operation, requestHash);
        await AcquireReferenceSeriesLockAsync(cancellationToken);

        var existingByOperation = await dbContext
            .CommercialReceivingReferenceReservations
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_reservations WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND operation_id = {operation.OperationId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        var existingBySession = await dbContext
            .CommercialReceivingReferenceReservations
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_reservations WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND session_id = {operation.SessionId} FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
        if (existingByOperation is not null)
        {
            if (existingByOperation.SessionId != operation.SessionId ||
                !string.Equals(
                    existingByOperation.RequestHash,
                    requestHash,
                    StringComparison.Ordinal))
            {
                throw Problem("IDEMPOTENCY_PAYLOAD_CONFLICT", "The Start operation already owns another reference reservation.", ApplicationErrorCategory.Conflict);
            }

            if (existingBySession?.Id != existingByOperation.Id)
            {
                throw Problem(
                    "RECEIVING_REFERENCE_CONFLICT",
                    "The Receiving Session is already bound to another reference reservation.",
                    ApplicationErrorCategory.Conflict);
            }

            await transaction.CommitAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return;
        }

        if (existingBySession is not null)
        {
            throw Problem(
                "RECEIVING_REFERENCE_CONFLICT",
                "The Receiving Session is already bound to another Start operation.",
                ApplicationErrorCategory.Conflict);
        }

        var now = clock.UtcNow;
        var policy = await dbContext.CommercialReceivingReferencePolicies
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_policies WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND document_type = {CommercialReceivingReferencePolicy.DocumentTypeValue} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            policy = CommercialReceivingReferencePolicy.CreateDefault(current.WorkspaceId, current.CompanyId, now);
            dbContext.CommercialReceivingReferencePolicies.Add(policy);
        }

        var periodKey = policy.PeriodKey(now);
        var counter = await dbContext.CommercialReceivingReferenceCounters
            .FromSqlInterpolated($"SELECT * FROM procurement.commercial_receiving_reference_counters WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND policy_id = {policy.Id} AND period_key = {periodKey} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (counter is null)
        {
            counter = CommercialReceivingReferenceCounter.Create(
                current.WorkspaceId, current.CompanyId, policy.Id, periodKey, policy.StartingNumber);
            dbContext.CommercialReceivingReferenceCounters.Add(counter);
        }

        var sequence = counter.Allocate();
        dbContext.CommercialReceivingReferenceReservations.Add(
            CommercialReceivingReferenceReservation.Create(
                current.WorkspaceId, current.CompanyId, policy.Id, periodKey,
                operation.OperationId, operation.SessionId, requestHash, policy.Version,
                sequence, policy.Render(sequence, now), now));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (MapPersistence(exception) is { } mapped)
        {
            throw mapped;
        }
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private Task AcquireReferenceSeriesLockAsync(
        CancellationToken cancellationToken)
    {
        var scope =
            $"TraderPro.CommercialReceiving.Reference.v1\n{current.WorkspaceId:D}\n{current.CompanyId:D}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))",
            cancellationToken);
    }

    private Task LockStartMastersAsync(
        StartCommercialReceivingSessionPayload payload,
        CancellationToken cancellationToken) =>
        LockMasterRowsAsync(
            new[]
            {
                $"SELECT 1 FROM procurement.suppliers WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.SupplierId:D}' FOR SHARE",
                $"SELECT 1 FROM procurement.company_procurement_settings WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.CompanyProcurementSettingsId:D}' FOR SHARE",
                $"SELECT 1 FROM operations.business_locations WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.DestinationLocationId:D}' FOR SHARE",
                $"SELECT 1 FROM procurement.weight_processing_policies WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.WeightProcessingPolicyId:D}' FOR SHARE",
                payload.ReceivingVehicleId is null ? null : $"SELECT 1 FROM procurement.receiving_vehicles WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.ReceivingVehicleId.Value:D}' FOR SHARE",
            },
            cancellationToken);

    private Task LockEntryMastersAsync(
        RecordCommercialReceivingEntryPayload payload,
        CancellationToken cancellationToken) =>
        LockMasterRowsAsync(
            new[]
            {
                $"SELECT 1 FROM catalog.products WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.ProductId:D}' FOR SHARE",
                payload.SupplierProductScopeId is null ? null : $"SELECT 1 FROM procurement.supplier_product_scopes WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.SupplierProductScopeId.Value:D}' FOR SHARE",
                $"SELECT 1 FROM procurement.bag_types WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.BagTypeId:D}' FOR SHARE",
                payload.ProductStandardBagWeightId is null ? null : $"SELECT 1 FROM catalog.product_standard_bag_weights WHERE workspace_id = '{current.WorkspaceId:D}' AND company_id = '{current.CompanyId:D}' AND id = '{payload.ProductStandardBagWeightId.Value:D}' FOR SHARE",
            },
            cancellationToken);

    private async Task LockMasterRowsAsync(
        IEnumerable<string?> statements,
        CancellationToken cancellationToken)
    {
        foreach (var statement in statements.Where(value => value is not null))
        {
            await dbContext.Database.ExecuteSqlRawAsync(statement!, cancellationToken);
        }
    }

    private static ApplicationProblemException FromDomain(CommercialReceivingDomainException exception) =>
        Problem(
            exception.Code,
            exception.Message,
            exception.Code is "RECEIVING_SESSION_NOT_FOUND" ? ApplicationErrorCategory.NotFound : ApplicationErrorCategory.Conflict,
            inner: exception);

    private static void Domain(Action action)
    {
        try { action(); }
        catch (CommercialReceivingDomainException exception) { throw FromDomain(exception); }
    }

    private void RequireContext()
    {
        if (!current.IsBound || !current.IsOperatorOrOwner)
        {
            throw Problem("AUTHORIZATION_DENIED", "The request is not authorized.", ApplicationErrorCategory.Authorization);
        }
    }

    private void RequireOperatorContext()
    {
        if (!current.IsBound || !current.IsOperator)
        {
            throw Problem("AUTHORIZATION_DENIED", "The request is not authorized.", ApplicationErrorCategory.Authorization);
        }
    }

    private async Task ClaimOperationAsync(
        CommercialMobileOperationCommand operation,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireOperationClaimLockAsync(operation.OperationId, cancellationToken);
        var existing = await LoadOperationClaimForUpdateOrDefaultAsync(
            operation.OperationId,
            cancellationToken);
        if (existing is null)
        {
            if (operation.OperationType == CommercialReceivingOperationTypes.Start)
            {
                var reservedSession = await dbContext.CommercialReceivingReferenceReservations
                    .SingleOrDefaultAsync(
                        item => item.CompanyId == current.CompanyId &&
                            item.SessionId == operation.SessionId,
                        cancellationToken);
                if (reservedSession is not null && reservedSession.OperationId != operation.OperationId)
                {
                    if (reservedSession.RequestHash == new string('0', 64))
                    {
                        throw Problem(
                            "COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE",
                            "The legacy Start operation identity cannot be reconstructed safely and cannot be replayed.",
                            ApplicationErrorCategory.Conflict);
                    }

                    throw Problem(
                        "RECEIVING_REFERENCE_CONFLICT",
                        "The Receiving Session is already bound to another Start operation.",
                        ApplicationErrorCategory.Conflict);
                }
            }

            dbContext.CommercialReceivingOperationClaims.Add(
                CommercialReceivingOperationClaim.Create(
                    current.WorkspaceId, current.CompanyId, operation.OperationId,
                    operation.OperationType, operation.SessionId, current.DeviceId,
                    operation.OwnershipGeneration, requestHash, clock.UtcNow));
        }
        else
        {
            if (existing.State is CommercialReceivingOperationClaimState.Rejected &&
                existing.AttentionCode == "COMMERCIAL_MOBILE_OPERATION_LEGACY_NON_REPLAYABLE")
            {
                var knownOperationType = existing.OperationType ==
                    "LegacyNonReplayableCommercialReceivingSession"
                        ? CommercialReceivingOperationTypes.Start
                        : existing.OperationType;
                var hasKnownHash = existing.RequestHash != new string('0', 64);
                if (knownOperationType != operation.OperationType ||
                    existing.SessionId != operation.SessionId ||
                    existing.DeviceId != current.DeviceId ||
                    existing.OwnershipGeneration != operation.OwnershipGeneration ||
                    (hasKnownHash && !string.Equals(
                        existing.RequestHash,
                        requestHash,
                        StringComparison.Ordinal)))
                {
                    throw Problem("IDEMPOTENCY_PAYLOAD_CONFLICT", "The Operation ID is already bound to another immutable request.", ApplicationErrorCategory.Conflict);
                }

                throw Problem(
                    existing.AttentionCode,
                    existing.AttentionMessage ?? "The legacy operation cannot be replayed safely.",
                    ApplicationErrorCategory.Conflict);
            }

            if (!ClaimIdentityMatches(existing, operation, requestHash))
            {
                throw Problem("IDEMPOTENCY_PAYLOAD_CONFLICT", "The Operation ID is already bound to another immutable request.", ApplicationErrorCategory.Conflict);
            }

            if (existing.State is CommercialReceivingOperationClaimState.Rejected)
            {
                throw Problem(
                    existing.AttentionCode ?? "COMMERCIAL_MOBILE_OPERATION_REJECTED",
                    existing.AttentionMessage ?? "The operation was permanently rejected.",
                    ApplicationErrorCategory.Conflict,
                    existing.Retryable);
            }

            existing.BeginRetry(clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private async Task RecordClaimFailureAsync(
        Guid operationId,
        string requestHash,
        ApplicationProblemException exception,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireOperationClaimLockAsync(operationId, cancellationToken);
        var claim = await LoadOperationClaimForUpdateOrDefaultAsync(operationId, cancellationToken);
        if (claim is not null && string.Equals(claim.RequestHash, requestHash, StringComparison.Ordinal))
        {
            if (IsNeedsAttention(exception.Code) || exception.Retryable)
            {
                claim.NeedsAttention(exception.Code, exception.Message, exception.Retryable, clock.UtcNow);
            }
            else
            {
                claim.Reject(exception.Code, exception.Message, exception.Retryable, clock.UtcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private async Task CompleteClaimIfNeededAsync(
        Guid operationId,
        string requestHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await AcquireOperationClaimLockAsync(operationId, cancellationToken);
        var claim = await LoadOperationClaimForUpdateAsync(operationId, cancellationToken);
        if (!string.Equals(claim.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw Problem("IDEMPOTENCY_PAYLOAD_CONFLICT", "The Operation ID is already bound to another immutable request.", ApplicationErrorCategory.Conflict);
        }

        if (claim.State is not CommercialReceivingOperationClaimState.Completed)
        {
            claim.Complete(clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
    }

    private Task AcquireOperationClaimLockAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var scope = $"{CommercialReceivingOperationClaim.CommandScope}\n{operationId:D}";
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({scope}, 0))",
            cancellationToken);
    }

    private Task<CommercialReceivingOperationClaim?> LoadOperationClaimForUpdateOrDefaultAsync(
        Guid operationId,
        CancellationToken cancellationToken) =>
        dbContext.CommercialReceivingOperationClaims
            .FromSqlInterpolated($"SELECT * FROM sync.commercial_receiving_operation_claims WHERE workspace_id = {current.WorkspaceId} AND company_id = {current.CompanyId} AND command_scope = {CommercialReceivingOperationClaim.CommandScope} AND operation_id = {operationId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<CommercialReceivingOperationClaim> LoadOperationClaimForUpdateAsync(
        Guid operationId,
        CancellationToken cancellationToken) =>
        await LoadOperationClaimForUpdateOrDefaultAsync(operationId, cancellationToken) ??
        throw Problem(
            "COMMERCIAL_MOBILE_OPERATION_CLAIM_MISSING",
            "The commercial mobile operation claim is missing.",
            ApplicationErrorCategory.Conflict);

    private bool ClaimIdentityMatches(
        CommercialReceivingOperationClaim claim,
        CommercialMobileOperationCommand operation,
        string requestHash) =>
        string.Equals(claim.RequestHash, requestHash, StringComparison.Ordinal) &&
        claim.OperationType == operation.OperationType &&
        claim.SessionId == operation.SessionId &&
        claim.DeviceId == current.DeviceId &&
        claim.OwnershipGeneration == operation.OwnershipGeneration;

    private void EnsureClaimIdentity(
        CommercialReceivingOperationClaim claim,
        CommercialMobileOperationCommand operation,
        string requestHash)
    {
        if (!ClaimIdentityMatches(claim, operation, requestHash))
        {
            throw Problem(
                "IDEMPOTENCY_PAYLOAD_CONFLICT",
                "The Operation ID is already bound to another immutable request.",
                ApplicationErrorCategory.Conflict);
        }
    }

    private static bool IsUuid7(Guid value) => value != Guid.Empty && value.Version == 7;

    private static bool IsNeedsAttention(string code) => code is
        "RECEIVING_SEQUENCE_GAP" or
        "RECEIVING_SETTINGS_NOT_CONFIGURED" or
        "RECEIVING_SETTINGS_VERSION_STALE" or
        "RECEIVING_DEFAULT_DESTINATION_MISMATCH" or
        "RECEIVING_DEFAULT_POLICY_MISMATCH" or
        "RECEIVING_SUPPLIER_INACTIVE" or
        "RECEIVING_SUPPLIER_VERSION_STALE" or
        "RECEIVING_PRODUCT_INACTIVE" or
        "RECEIVING_PRODUCT_NOT_PURCHASABLE" or
        "RECEIVING_PRODUCT_VERSION_STALE" or
        "RECEIVING_PRODUCT_SCOPE_REQUIRED" or
        "RECEIVING_PRODUCT_SCOPE_INVALID" or
        "RECEIVING_BAG_TYPE_INACTIVE" or
        "RECEIVING_BAG_TYPE_VERSION_STALE" or
        "RECEIVING_STANDARD_BAG_WEIGHT_INVALID" or
        "RECEIVING_STANDARD_BAG_WEIGHT_VERSION_STALE" or
        "RECEIVING_DESTINATION_INACTIVE" or
        "RECEIVING_WEIGHT_POLICY_INACTIVE" or
        "RECEIVING_VEHICLE_INACTIVE" or
        "RECEIVING_MASTER_NEEDS_ATTENTION" or
        "RECEIVING_OWNERSHIP_DEVICE_MISMATCH" or
        "RECEIVING_OWNERSHIP_GENERATION_STALE" or
        "RECEIVING_LEASE_REQUIRED" or
        "RECEIVING_LEASE_INVALID" or
        "RECEIVING_LEASE_REACQUISITION_REQUIRED";

    private static CommercialMobileOperationResult Failed(
        CommercialMobileOperationCommand operation,
        CommercialMobileOperationStatus status,
        string code,
        string message,
        bool retryable = false,
        bool requiresAction = false) =>
        new(operation.OperationId, operation.SessionId, operation.LocalSequence, status, null, new(code, message, retryable, requiresAction));

    private static bool IsReplayableCloudState(CommercialReceivingCloudState state) =>
        !string.IsNullOrWhiteSpace(state.CloudReference) && state.SessionVersion > 0 && state.OwnershipGeneration > 0;

    private static ApplicationProblemException? MapPersistence(Exception exception)
    {
        var postgres = CommercialMasterDataInfrastructure.PostgreSql(exception);
        if (postgres?.SqlState == "23505")
        {
            return Problem("RECEIVING_REFERENCE_CONFLICT", "A Receiving identity or reference conflicts with an existing fact.", ApplicationErrorCategory.Conflict);
        }
        if (postgres?.SqlState == "55000" &&
            postgres.MessageText ==
                "Commercial Receiving editor must be an active credentialed Device.")
        {
            return Problem(
                "RECEIVING_OWNERSHIP_TARGET_INVALID",
                "The target Device is not active and credentialed.",
                ApplicationErrorCategory.Conflict);
        }
        return null;
    }

    private static ApplicationProblemException Problem(
        string code,
        string message,
        ApplicationErrorCategory category,
        bool retryable = false,
        Exception? inner = null) =>
        new(code, message, category, retryable, innerException: inner);

    private static string DecimalText(decimal value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);
}
