using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TraderPro.Application.Common.Commands;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.Tenancy;
using TraderPro.Application.Common.Time;
using TraderPro.Application.Procurement.Poc;
using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;
using TraderPro.Domain.Platform;
using TraderPro.Domain.Procurement.Poc;
using TraderPro.Infrastructure.Modules.Shared;
using TraderPro.Infrastructure.Persistence;
using TraderPro.Infrastructure.Tenancy;

namespace TraderPro.Infrastructure.Modules.Procurement.Poc;

internal sealed class ReceivingPocService(
    TraderProDbContext dbContext,
    ICurrentWorkspaceAccessor currentWorkspace,
    ICurrentDeviceAccessor currentDevice,
    CurrentWorkspaceAccessor mutableWorkspace,
    IClock clock,
    ProcurementPocOptions options,
    PostgreSqlIdempotentCommandExecutor idempotency) : IReceivingPocService
{
    private const string AggregateType = "ReceivingSessionPoc";
    private const int EventVersion = 1;
    private const string BootstrapCode = "TRADERPRO-PROCUREMENT-POC-LOCAL";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async Task<MobileSyncOperationsResult> ProcessOperationsAsync(
        MobileSyncOperationsCommand command,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var operations = command.Operations;
        if (operations is null || operations.Count is < 1 or > 50)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "A sync batch must contain between 1 and 50 operations.",
                ApplicationErrorCategory.Validation);
        }

        RequireContext();
        var normalized = operations.Select(ValidateCorrelation).ToArray();
        var blockedAggregates = new HashSet<Guid>();
        var results = new List<MobileSyncOperationResult>(normalized.Length);

        foreach (var correlated in normalized)
        {
            if (blockedAggregates.Contains(correlated.AggregateId))
            {
                results.Add(
                    Failure(
                        correlated,
                        "NeedsAttention",
                        "SYNC_OPERATION_BATCH_INVALID",
                        "A previous operation for this aggregate failed."));
                continue;
            }

            try
            {
                var operation = ValidateOperation(correlated);
                ValidatePayloadHash(operation);
                var result = operation.OperationType switch
                {
                    ProcurementPocOperationTypes.StartReceivingSession =>
                        await StartAsync(
                            operation,
                            correlationId,
                            cancellationToken),
                    ProcurementPocOperationTypes.RecordReceivingEntry =>
                        await RecordEntryAsync(
                            operation,
                            correlationId,
                            cancellationToken),
                    ProcurementPocOperationTypes.SubmitReceivingSession =>
                        await SubmitAsync(
                            operation,
                            correlationId,
                            cancellationToken),
                    _ => throw Problem(
                        "SYNC_OPERATION_TYPE_UNSUPPORTED",
                        "The sync operation type is not supported by this POC.",
                        ApplicationErrorCategory.Validation),
                };
                results.Add(
                    new MobileSyncOperationResult(
                        operation.OperationId,
                        operation.AggregateId,
                        operation.LocalSequence,
                        result.IdempotencyStatus ==
                            IdempotencyExecutionStatus.PreviouslyProcessed
                                ? "PreviouslyProcessed"
                                : "Accepted",
                        result.Result.Version,
                        result.Result.CloudReference,
                        result.Result.LeaseId,
                        result.Result.LeaseExpiresAtUtc));
            }
            catch (ApplicationProblemException exception)
            {
                blockedAggregates.Add(correlated.AggregateId);
                var resultStatus = exception.Category ==
                    ApplicationErrorCategory.Validation
                        ? "Rejected"
                        : "NeedsAttention";
                results.Add(
                    Failure(
                        correlated,
                        resultStatus,
                        exception.Code,
                        exception.Message));
            }
        }

        return new MobileSyncOperationsResult(results);
    }

    public Task<IdempotentCommandResult<ReceivingPocCommandResult>>
        HeartbeatAsync(
            ReceivingPocHeartbeatCommand command,
            string idempotencyKey,
            string correlationId,
            CancellationToken cancellationToken)
    {
        var (workspaceId, deviceId) = RequireContext();
        RequireIdempotencyKey(idempotencyKey);
        if (command.SessionId == Guid.Empty || command.LeaseId == Guid.Empty)
        {
            throw Problem(
                "RECEIVING_POC_LEASE_INVALID",
                "A valid session and lease UUID are required.",
                ApplicationErrorCategory.Validation);
        }

        var requestHash = ProcurementPocRequestHash.ForHeartbeat(
            deviceId,
            command.SessionId,
            command.LeaseId);
        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.Heartbeat,
            idempotencyKey,
            requestHash,
            correlationId,
            200,
            async () =>
            {
                var session = await RequireSessionAsync(
                    command.SessionId,
                    cancellationToken);
                var now = clock.UtcNow;
                ApplyDomain(
                    () => session.RenewLease(
                        deviceId,
                        command.LeaseId,
                        now,
                        now.AddMinutes(options.LeaseMinutes)));
                return ToResult(session, checked(session.Version + 1));
            },
            IsReplayable,
            () => CurrentVersionProblemAsync(
                command.SessionId,
                null,
                cancellationToken),
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ReceivingPocCommandResult>>
        ApproveAsync(
            ReceivingPocVersionedCommand command,
            string idempotencyKey,
            string correlationId,
            CancellationToken cancellationToken)
    {
        var (workspaceId, deviceId) = RequireContext();
        RequireIdempotencyKey(idempotencyKey);
        RequirePositiveExpectedVersion(command.ExpectedVersion);
        var requestHash = ProcurementPocRequestHash.ForApprove(
            deviceId,
            command.SessionId,
            command.ExpectedVersion);

        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.Approve,
            idempotencyKey,
            requestHash,
            correlationId,
            200,
            async () =>
            {
                var session = await RequireSessionAsync(
                    command.SessionId,
                    cancellationToken);
                EnsureExpectedVersion(session, command.ExpectedVersion);
                var now = clock.UtcNow;
                ApplyDomain(() => session.Approve(deviceId, now));
                var version = checked(session.Version + 1);
                var result = ToResult(session, version);
                AddAudit(
                    workspaceId,
                    deviceId,
                    "Procurement.Poc.ReceivingSession.Approved",
                    session.Id,
                    result,
                    correlationId,
                    now);
                AddEvent(
                    workspaceId,
                    "ReceivingSessionPocApproved",
                    session.Id,
                    version,
                    new
                    {
                        sessionId = session.Id,
                        status = session.Status.ToString(),
                        approvedByDeviceId = deviceId,
                        version,
                    },
                    correlationId,
                    now);
                return result;
            },
            IsReplayable,
            () => CurrentVersionProblemAsync(
                command.SessionId,
                command.ExpectedVersion,
                cancellationToken),
            cancellationToken);
    }

    public Task<IdempotentCommandResult<ReceivingPocCommandResult>>
        FinalizeAsync(
            ReceivingPocVersionedCommand command,
            string idempotencyKey,
            string correlationId,
            CancellationToken cancellationToken)
    {
        var (workspaceId, deviceId) = RequireContext();
        RequireIdempotencyKey(idempotencyKey);
        RequirePositiveExpectedVersion(command.ExpectedVersion);
        var requestHash = ProcurementPocRequestHash.ForFinalize(
            deviceId,
            command.SessionId,
            command.ExpectedVersion);

        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.Finalize,
            idempotencyKey,
            requestHash,
            correlationId,
            200,
            async () =>
            {
                var session = await RequireSessionAsync(
                    command.SessionId,
                    cancellationToken);
                if (session.Status == ReceivingPocStatus.Finalized)
                {
                    throw Problem(
                        "RECEIVING_POC_ALREADY_FINALIZED",
                        "The POC session is already finalized.",
                        ApplicationErrorCategory.Conflict);
                }

                EnsureExpectedVersion(session, command.ExpectedVersion);
                if (session.ApprovedByDeviceId is null)
                {
                    throw Problem(
                        "RECEIVING_POC_STATUS_INVALID",
                        "The POC session has no recorded approval.",
                        ApplicationErrorCategory.Conflict);
                }

                var now = clock.UtcNow;
                ApplyDomain(() => session.Finalize(deviceId, now));
                var finalization = ReceivingFinalizationPoc.Create(
                    workspaceId,
                    session.Id,
                    session.EntryCount,
                    session.ProcessedTotalWeightKg,
                    session.ApprovedByDeviceId.Value,
                    deviceId,
                    now);
                dbContext.ReceivingFinalizationPocs.Add(finalization);
                var version = checked(session.Version + 1);
                var result = ToResult(session, version, finalization.Id);
                AddAudit(
                    workspaceId,
                    deviceId,
                    "Procurement.Poc.ReceivingSession.Finalized",
                    session.Id,
                    result,
                    correlationId,
                    now);
                AddEvent(
                    workspaceId,
                    "ReceivingSessionPocFinalized",
                    session.Id,
                    version,
                    new
                    {
                        sessionId = session.Id,
                        finalizationId = finalization.Id,
                        status = session.Status.ToString(),
                        finalEntryCount = finalization.FinalEntryCount,
                        finalProcessedTotalWeightKg =
                            Weight(finalization.FinalProcessedTotalWeightKg),
                        version,
                    },
                    correlationId,
                    now);
                return result;
            },
            IsReplayable,
            () => FinalizationConcurrencyProblemAsync(
                command.SessionId,
                command.ExpectedVersion,
                cancellationToken),
            cancellationToken);
    }

    public async Task<ReceivingPocLiveView?> FindLiveViewAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        RequireContext();
        var session = await dbContext.ReceivingSessionPocs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == sessionId,
                cancellationToken);
        if (session is null)
        {
            return null;
        }

        var entries = await dbContext.ReceivingEntryPocs
            .AsNoTracking()
            .Where(entry => entry.ReceivingSessionId == sessionId)
            .OrderByDescending(entry => entry.LocalSequence)
            .Take(5)
            .Select(entry => new ReceivingPocEntryView(
                entry.Id,
                entry.LocalSequence,
                entry.ProductReference,
                entry.BagTypeReference,
                entry.BagCount,
                entry.RawWeightKg,
                Weight(entry.ProcessedWeightKg),
                DisplayWeight(entry.DisplayWeightKg, entry.DecimalPlaces),
                entry.DecimalPlaces,
                entry.ProcessingMethod,
                entry.WeightSource,
                entry.CapturedAtDeviceUtc,
                entry.AcceptedAtServerUtc))
            .ToListAsync(cancellationToken);

        return new ReceivingPocLiveView(
            session.Id,
            Reference(session.CloudReferenceSequence),
            session.Status.ToString(),
            session.EditorDeviceId,
            session.LeaseExpiresAtUtc,
            session.EntryCount,
            Weight(session.ProcessedTotalWeightKg),
            session.Version,
            entries,
            session.UpdatedAtUtc);
    }

    public async Task<ReceivingPocListResult> ListAsync(
        string? status,
        long? after,
        int limit,
        CancellationToken cancellationToken)
    {
        RequireContext();
        if (after < 0 || limit is < 1 or > 100)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "The list cursor must be non-negative and limit must be 1 to 100.",
                ApplicationErrorCategory.Validation);
        }

        ReceivingPocStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ReceivingPocStatus>(
                    status,
                    ignoreCase: false,
                    out var value) ||
                !Enum.IsDefined(value))
            {
                throw Problem(
                    "RECEIVING_POC_STATUS_INVALID",
                    "The requested POC status is invalid.",
                    ApplicationErrorCategory.Validation);
            }

            parsedStatus = value;
        }

        var query = dbContext.ReceivingSessionPocs.AsNoTracking();
        if (parsedStatus is not null)
        {
            query = query.Where(
                candidate => candidate.Status == parsedStatus.Value);
        }

        if (after is not null)
        {
            query = query.Where(
                candidate => candidate.CloudReferenceSequence > after.Value);
        }

        var rows = await query
            .OrderBy(candidate => candidate.CloudReferenceSequence)
            .Take(limit + 1)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.CloudReferenceSequence,
                candidate.Status,
                candidate.EditorDeviceId,
                candidate.LeaseExpiresAtUtc,
                candidate.EntryCount,
                candidate.ProcessedTotalWeightKg,
                candidate.Version,
                candidate.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToArray();
        var items = page.Select(candidate => new ReceivingPocListItem(
            candidate.Id,
            Reference(candidate.CloudReferenceSequence),
            candidate.Status.ToString(),
            candidate.EditorDeviceId,
            candidate.LeaseExpiresAtUtc,
            candidate.EntryCount,
            Weight(candidate.ProcessedTotalWeightKg),
            candidate.Version,
            candidate.UpdatedAtUtc)).ToArray();

        return new ReceivingPocListResult(
            items,
            hasMore && page.Length > 0
                ? page[^1].CloudReferenceSequence
                : null,
            hasMore);
    }

    public async Task<ProcurementPocBootstrapResult> BootstrapAsync(
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({BootstrapCode}, 0))",
            cancellationToken);

        var workspace = await dbContext.Workspaces
            .SingleOrDefaultAsync(
                candidate => candidate.Code == BootstrapCode,
                cancellationToken);
        if (workspace is null)
        {
            workspace = Workspace.Create(
                BootstrapCode,
                "TraderPro Procurement POC Workspace",
                clock.UtcNow);
            dbContext.Workspaces.Add(workspace);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        mutableWorkspace.SetWorkspace(workspace.Id);
        var company = await dbContext.Companies.SingleOrDefaultAsync(
            candidate => candidate.Code == "POC-COMPANY",
            cancellationToken);
        if (company is null)
        {
            company = Company.Create(
                workspace.Id,
                "POC-COMPANY",
                "TraderPro Procurement POC Company",
                "TraderPro POC",
                null,
                clock.UtcNow);
            dbContext.Companies.Add(company);
        }

        var branch = await dbContext.Branches.SingleOrDefaultAsync(
            candidate => candidate.Code == "POC-BRANCH",
            cancellationToken);
        if (branch is null)
        {
            branch = Branch.Create(
                workspace.Id,
                company.Id,
                "POC-BRANCH",
                "POC Default Branch",
                true,
                clock.UtcNow);
            dbContext.Branches.Add(branch);
        }

        var operatorDevice = await dbContext.Devices.SingleOrDefaultAsync(
            candidate => candidate.InstallationId == "poc-mobile-a",
            cancellationToken);
        if (operatorDevice is null)
        {
            operatorDevice = Device.Create(
                workspace.Id,
                "poc-mobile-a",
                "POC Mobile A Operator",
                "Development",
                clock.UtcNow);
            dbContext.Devices.Add(operatorDevice);
        }

        var ownerDevice = await dbContext.Devices.SingleOrDefaultAsync(
            candidate => candidate.InstallationId == "poc-mobile-b",
            cancellationToken);
        if (ownerDevice is null)
        {
            ownerDevice = Device.Create(
                workspace.Id,
                "poc-mobile-b",
                "POC Mobile B Owner",
                "Development",
                clock.UtcNow);
            dbContext.Devices.Add(ownerDevice);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProcurementPocBootstrapResult(
            workspace.Id,
            company.Id,
            branch.Id,
            operatorDevice.Id,
            ownerDevice.Id,
            BootstrapCode);
    }

    private Task<IdempotentCommandResult<ReceivingPocCommandResult>> StartAsync(
        ValidatedOperation operation,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (operation.LeaseId is not null)
        {
            throw Problem(
                "RECEIVING_POC_LEASE_UNEXPECTED",
                "StartReceivingSession must not include lease metadata.",
                ApplicationErrorCategory.Validation);
        }

        if (operation.LocalSequence != 1)
        {
            throw SequenceProblem(operation.LocalSequence, 1);
        }

        if (operation.AggregateId.Version != 7)
        {
            throw Problem(
                "RECEIVING_POC_SESSION_ID_INVALID",
                "The client-supplied session ID must be UUIDv7.",
                ApplicationErrorCategory.Validation);
        }

        var payload = ReadPayload<StartReceivingSessionPocPayload>(
            operation.PayloadJson);
        ValidatePayloadIdentity(
            payload.OperationId,
            payload.LocalSessionId,
            null,
            operation);
        if (payload.CreatedAtDeviceUtc is not null &&
            payload.CreatedAtDeviceUtc.Value.Offset != TimeSpan.Zero)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "createdAtDeviceUtc must be UTC when supplied.",
                ApplicationErrorCategory.Validation);
        }

        var (workspaceId, deviceId) = RequireContext();
        var requestHash = ProcurementPocRequestHash.ForStart(
            deviceId,
            operation.OperationType,
            operation.AggregateId,
            operation.LocalSequence,
            operation.ExpectedCloudVersion,
            payload.TemporaryReference,
            payload.CreatedAtDeviceUtc);
        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.MobileSyncOperation,
            operation.OperationId.ToString("D"),
            requestHash,
            correlationId,
            201,
            async () =>
            {
                if (await dbContext.ReceivingSessionPocs.AnyAsync(
                        candidate => candidate.Id == operation.AggregateId,
                        cancellationToken))
                {
                    throw Problem(
                        "RECEIVING_POC_SEQUENCE_CONFLICT",
                        "The POC session already exists for another operation.",
                        ApplicationErrorCategory.Conflict);
                }

                var now = clock.UtcNow;
                var session = ReceivingSessionPoc.Start(
                    operation.AggregateId,
                    workspaceId,
                    deviceId,
                    Uuid7.NewGuid(),
                    now.AddMinutes(options.LeaseMinutes),
                    now);
                dbContext.ReceivingSessionPocs.Add(session);

                // The cloud reference is an identity value generated by
                // PostgreSQL. This first flush stays inside the same command
                // transaction; any later failure rolls it back.
                await dbContext.SaveChangesAsync(cancellationToken);
                var result = ToResult(session, session.Version);
                AddAudit(
                    workspaceId,
                    deviceId,
                    "Procurement.Poc.ReceivingSession.Started",
                    session.Id,
                    result,
                    correlationId,
                    now);
                AddEvent(
                    workspaceId,
                    "ReceivingSessionPocStarted",
                    session.Id,
                    session.Version,
                    new
                    {
                        sessionId = session.Id,
                        cloudReference = result.CloudReference,
                        status = session.Status.ToString(),
                        editorDeviceId = deviceId,
                        leaseExpiresAtUtc = session.LeaseExpiresAtUtc,
                        version = session.Version,
                    },
                    correlationId,
                    now);
                return result;
            },
            IsReplayable,
            null,
            cancellationToken);
    }

    private Task<IdempotentCommandResult<ReceivingPocCommandResult>>
        RecordEntryAsync(
            ValidatedOperation operation,
            string correlationId,
            CancellationToken cancellationToken)
    {
        var payload = ReadPayload<RecordReceivingEntryPocPayload>(
            operation.PayloadJson);
        ValidatePayloadIdentity(
            payload.OperationId,
            payload.LocalSessionId,
            payload.CloudSessionId,
            operation);
        if (payload.LocalSequence is not null &&
            payload.LocalSequence != operation.LocalSequence)
        {
            throw InvalidPayloadIdentity();
        }

        var leaseId = operation.LeaseId ??
            throw Problem(
                "RECEIVING_POC_LEASE_REQUIRED",
                "RecordReceivingEntry requires lease metadata.",
                ApplicationErrorCategory.Validation);
        var productReference = RequireText(
            payload.ProductReference,
            ReceivingEntryPoc.MaximumProductReferenceLength,
            "productReference");
        var bagTypeReference = RequireText(
            payload.BagTypeReference,
            ReceivingEntryPoc.MaximumBagTypeReferenceLength,
            "bagTypeReference");
        if (payload.BagCount <= 0 ||
            payload.CapturedAtDeviceUtc.Offset != TimeSpan.Zero)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "Bag count must be positive and the capture time must be UTC.",
                ApplicationErrorCategory.Validation);
        }

        if (payload.WeightSource is not ("ManualSpike" or "TestScale"))
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "weightSource must be ManualSpike or TestScale.",
                ApplicationErrorCategory.Validation);
        }

        WeightProcessingResult processed;
        try
        {
            processed = WeightProcessor.Process(
                payload.RawWeightKg,
                payload.DecimalPlaces,
                payload.ProcessingMethod);
        }
        catch (WeightProcessingException exception)
        {
            throw Problem(
                "RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH",
                "The supplied weight does not match the captured processing policy.",
                ApplicationErrorCategory.Validation,
                exception);
        }

        if (!string.Equals(
                processed.ProcessedWeightKg,
                payload.ProcessedWeightKg,
                StringComparison.Ordinal) ||
            !string.Equals(
                processed.DisplayWeightKg,
                payload.DisplayWeightKg,
                StringComparison.Ordinal))
        {
            throw Problem(
                "RECEIVING_POC_WEIGHT_PROCESSING_MISMATCH",
                "The submitted processed/display weight does not match the server result.",
                ApplicationErrorCategory.Validation);
        }

        var (workspaceId, deviceId) = RequireContext();
        var requestHash = ProcurementPocRequestHash.ForRecordEntry(
            deviceId,
            operation.OperationType,
            operation.AggregateId,
            operation.LocalSequence,
            operation.ExpectedCloudVersion,
            leaseId,
            productReference,
            bagTypeReference,
            payload.BagCount,
            processed.RawWeightKg,
            processed.ProcessedWeightKg,
            processed.DisplayWeightKg,
            processed.DecimalPlaces,
            processed.Method.ToString(),
            payload.WeightSource,
            payload.CapturedAtDeviceUtc);
        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.MobileSyncOperation,
            operation.OperationId.ToString("D"),
            requestHash,
            correlationId,
            200,
            async () =>
            {
                var session = await RequireSessionAsync(
                    operation.AggregateId,
                    cancellationToken);
                EnsureExpectedVersionIfSupplied(
                    session,
                    operation.ExpectedCloudVersion);
                var now = clock.UtcNow;
                ApplyDomain(
                    () => session.RecordEntry(
                        deviceId,
                        leaseId,
                        operation.LocalSequence,
                        decimal.Parse(
                            processed.ProcessedWeightKg,
                            CultureInfo.InvariantCulture),
                        now));
                var entry = ReceivingEntryPoc.Create(
                    workspaceId,
                    session.Id,
                    operation.OperationId,
                    operation.LocalSequence,
                    productReference,
                    bagTypeReference,
                    payload.BagCount,
                    processed,
                    payload.WeightSource,
                    payload.CapturedAtDeviceUtc,
                    now);
                dbContext.ReceivingEntryPocs.Add(entry);
                var version = checked(session.Version + 1);
                var result = ToResult(session, version);
                AddAudit(
                    workspaceId,
                    deviceId,
                    "Procurement.Poc.ReceivingEntry.Accepted",
                    session.Id,
                    new
                    {
                        entry.Id,
                        entry.LocalSequence,
                        processed.ProcessedWeightKg,
                        session.EntryCount,
                        processedTotalWeightKg =
                            Weight(session.ProcessedTotalWeightKg),
                        version,
                    },
                    correlationId,
                    now);
                AddEvent(
                    workspaceId,
                    "ReceivingEntryPocAccepted",
                    session.Id,
                    version,
                    new
                    {
                        sessionId = session.Id,
                        entryId = entry.Id,
                        entry.LocalSequence,
                        entry.ProductReference,
                        entry.BagTypeReference,
                        entry.BagCount,
                        processedWeightKg = processed.ProcessedWeightKg,
                        entryCount = session.EntryCount,
                        processedTotalWeightKg =
                            Weight(session.ProcessedTotalWeightKg),
                        version,
                    },
                    correlationId,
                    now);
                return result;
            },
            IsReplayable,
            () => CurrentVersionProblemAsync(
                operation.AggregateId,
                operation.ExpectedCloudVersion,
                cancellationToken),
            cancellationToken);
    }

    private Task<IdempotentCommandResult<ReceivingPocCommandResult>> SubmitAsync(
        ValidatedOperation operation,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var payload = ReadPayload<SubmitReceivingSessionPocPayload>(
            operation.PayloadJson);
        ValidatePayloadIdentity(
            payload.OperationId,
            payload.LocalSessionId,
            null,
            operation);
        var leaseId = operation.LeaseId ??
            throw Problem(
                "RECEIVING_POC_LEASE_REQUIRED",
                "SubmitReceivingSession requires lease metadata.",
                ApplicationErrorCategory.Validation);
        var (workspaceId, deviceId) = RequireContext();
        var requestHash = ProcurementPocRequestHash.ForSubmit(
            deviceId,
            operation.OperationType,
            operation.AggregateId,
            operation.LocalSequence,
            operation.ExpectedCloudVersion,
            leaseId);
        return idempotency.ExecuteAsync(
            workspaceId,
            ProcurementPocCommandTypes.MobileSyncOperation,
            operation.OperationId.ToString("D"),
            requestHash,
            correlationId,
            200,
            async () =>
            {
                var session = await RequireSessionAsync(
                    operation.AggregateId,
                    cancellationToken);
                EnsureExpectedVersionIfSupplied(
                    session,
                    operation.ExpectedCloudVersion);
                var now = clock.UtcNow;
                ApplyDomain(
                    () => session.Submit(
                        deviceId,
                        leaseId,
                        operation.LocalSequence,
                        now));
                var version = checked(session.Version + 1);
                var result = ToResult(session, version);
                AddAudit(
                    workspaceId,
                    deviceId,
                    "Procurement.Poc.ReceivingSession.Submitted",
                    session.Id,
                    result,
                    correlationId,
                    now);
                AddEvent(
                    workspaceId,
                    "ReceivingSessionPocSubmitted",
                    session.Id,
                    version,
                    new
                    {
                        sessionId = session.Id,
                        status = session.Status.ToString(),
                        entryCount = session.EntryCount,
                        processedTotalWeightKg =
                            Weight(session.ProcessedTotalWeightKg),
                        version,
                    },
                    correlationId,
                    now);
                return result;
            },
            IsReplayable,
            () => CurrentVersionProblemAsync(
                operation.AggregateId,
                operation.ExpectedCloudVersion,
                cancellationToken),
            cancellationToken);
    }

    private async Task<ReceivingSessionPoc> RequireSessionAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.ReceivingSessionPocs.SingleOrDefaultAsync(
                candidate => candidate.Id == id,
                cancellationToken) ??
            throw Problem(
                "RECEIVING_POC_SESSION_NOT_FOUND",
                "The POC Receiving Session was not found.",
                ApplicationErrorCategory.NotFound);
    }

    private (Guid WorkspaceId, Guid DeviceId) RequireContext()
    {
        var workspaceId = currentWorkspace.WorkspaceId;
        var deviceId = currentDevice.DeviceId;
        if (workspaceId is null || deviceId is null)
        {
            throw Problem(
                "DEVICE_CONTEXT_REQUIRED",
                "A temporary workspace and device context is required.",
                ApplicationErrorCategory.Validation);
        }

        return (workspaceId.Value, deviceId.Value);
    }

    private static CorrelatedOperation ValidateCorrelation(
        MobileSyncOperationCommand? operation)
    {
        if (operation is null ||
            !Guid.TryParseExact(
                operation.OperationId,
                "D",
                out var operationId) ||
            operationId == Guid.Empty ||
            !Guid.TryParseExact(
                operation.AggregateId,
                "D",
                out var aggregateId) ||
            aggregateId == Guid.Empty ||
            operation.LocalSequence <= 0)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "Every operation requires canonical operation and aggregate UUIDs plus a positive sequence.",
                ApplicationErrorCategory.Validation);
        }

        return new CorrelatedOperation(
            operationId,
            aggregateId,
            operation.LocalSequence,
            operation.OperationType,
            operation.ExpectedCloudVersion,
            operation.PayloadJson,
            operation.PayloadHash,
            operation.LeaseId);
    }

    private static ValidatedOperation ValidateOperation(
        CorrelatedOperation operation)
    {
        if (string.IsNullOrWhiteSpace(operation.OperationType) ||
            operation.PayloadJson is null ||
            operation.PayloadHash is null)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "The operation type, payloadJson and payloadHash are required.",
                ApplicationErrorCategory.Validation);
        }

        return new ValidatedOperation(
            operation.OperationId,
            operation.OperationType,
            operation.AggregateId,
            operation.LocalSequence,
            operation.ExpectedCloudVersion,
            operation.PayloadJson,
            operation.PayloadHash,
            ParseOptionalCanonicalGuid(operation.LeaseId));
    }

    private static void ValidatePayloadHash(ValidatedOperation operation)
    {
        var actual = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(operation.PayloadJson)));
        if (operation.PayloadHash.Length != 64 ||
            !string.Equals(
                actual,
                operation.PayloadHash,
                StringComparison.Ordinal))
        {
            throw Problem(
                "SYNC_OPERATION_PAYLOAD_HASH_INVALID",
                "payloadHash does not match the exact UTF-8 payloadJson bytes.",
                ApplicationErrorCategory.Validation);
        }
    }

    private static T ReadPayload<T>(string payloadJson)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, JsonOptions) ??
                throw InvalidPayload();
        }
        catch (JsonException exception)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                "The operation payload is not valid typed JSON.",
                ApplicationErrorCategory.Validation,
                exception);
        }
    }

    private static void ValidatePayloadIdentity(
        string? operationId,
        string? sessionId,
        string? cloudSessionId,
        ValidatedOperation operation)
    {
        if (operationId is not null &&
            (!Guid.TryParseExact(operationId, "D", out var parsedOperation) ||
             parsedOperation != operation.OperationId))
        {
            throw InvalidPayloadIdentity();
        }

        if (sessionId is not null &&
            (!Guid.TryParseExact(sessionId, "D", out var parsedSession) ||
             parsedSession != operation.AggregateId))
        {
            throw InvalidPayloadIdentity();
        }

        if (cloudSessionId is not null &&
            (!Guid.TryParseExact(
                 cloudSessionId,
                 "D",
                 out var parsedCloudSession) ||
             parsedCloudSession == Guid.Empty ||
             !string.Equals(
                 cloudSessionId,
                 parsedCloudSession.ToString("D"),
                 StringComparison.Ordinal) ||
             parsedCloudSession != operation.AggregateId))
        {
            throw InvalidPayloadIdentity();
        }
    }

    private static ApplicationProblemException InvalidPayloadIdentity()
    {
        return Problem(
            "SYNC_OPERATION_BATCH_INVALID",
            "Payload operation/session identity must match the sync envelope.",
            ApplicationErrorCategory.Validation);
    }

    private static ApplicationProblemException InvalidPayload()
    {
        return Problem(
            "SYNC_OPERATION_BATCH_INVALID",
            "The operation payload is required.",
            ApplicationErrorCategory.Validation);
    }

    private static Guid? ParseOptionalCanonicalGuid(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!Guid.TryParseExact(value, "D", out var parsed) ||
            parsed == Guid.Empty ||
            !string.Equals(
                value,
                parsed.ToString("D"),
                StringComparison.Ordinal))
        {
            throw Problem(
                "RECEIVING_POC_LEASE_INVALID",
                "leaseId metadata must be one canonical non-empty UUID.",
                ApplicationErrorCategory.Validation);
        }

        return parsed;
    }

    private static string RequireText(
        string? value,
        int maximumLength,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Trim().Length > maximumLength)
        {
            throw Problem(
                "SYNC_OPERATION_BATCH_INVALID",
                $"{field} must contain 1 to {maximumLength} characters.",
                ApplicationErrorCategory.Validation);
        }

        return value.Trim();
    }

    private static void RequireIdempotencyKey(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw Problem(
                "IDEMPOTENCY_KEY_REQUIRED",
                "An Idempotency-Key header is required.",
                ApplicationErrorCategory.Validation);
        }

        if (value.Length > 200 ||
            value.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) ||
                  character is '.' or '_' or ':' or '-')))
        {
            throw Problem(
                "IDEMPOTENCY_KEY_INVALID",
                "The idempotency key contains unsupported characters.",
                ApplicationErrorCategory.Validation);
        }
    }

    private static void RequirePositiveExpectedVersion(long version)
    {
        if (version <= 0)
        {
            throw Problem(
                "RECEIVING_POC_VERSION_CONFLICT",
                "X-Expected-Version must be a positive integer.",
                ApplicationErrorCategory.Validation);
        }
    }

    private static void EnsureExpectedVersion(
        ReceivingSessionPoc session,
        long expectedVersion)
    {
        if (session.Version != expectedVersion)
        {
            throw VersionConflict(expectedVersion, session.Version);
        }
    }

    private static void EnsureExpectedVersionIfSupplied(
        ReceivingSessionPoc session,
        long? expectedVersion)
    {
        if (expectedVersion is not null &&
            session.Version != expectedVersion.Value)
        {
            throw VersionConflict(expectedVersion.Value, session.Version);
        }
    }

    private async Task<ApplicationProblemException> CurrentVersionProblemAsync(
        Guid sessionId,
        long? expectedVersion,
        CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var current = await dbContext.ReceivingSessionPocs
            .AsNoTracking()
            .Where(candidate => candidate.Id == sessionId)
            .Select(candidate => (long?)candidate.Version)
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            return Problem(
                "RECEIVING_POC_SESSION_NOT_FOUND",
                "The POC Receiving Session was not found.",
                ApplicationErrorCategory.NotFound);
        }

        return VersionConflict(expectedVersion, current.Value);
    }

    private async Task<ApplicationProblemException>
        FinalizationConcurrencyProblemAsync(
            Guid sessionId,
            long expectedVersion,
            CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var current = await dbContext.ReceivingSessionPocs
            .AsNoTracking()
            .Where(candidate => candidate.Id == sessionId)
            .Select(candidate => new
            {
                candidate.Status,
                candidate.Version,
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (current?.Status == ReceivingPocStatus.Finalized)
        {
            return Problem(
                "RECEIVING_POC_ALREADY_FINALIZED",
                "The POC session is already finalized.",
                ApplicationErrorCategory.Conflict);
        }

        return current is null
            ? Problem(
                "RECEIVING_POC_SESSION_NOT_FOUND",
                "The POC Receiving Session was not found.",
                ApplicationErrorCategory.NotFound)
            : VersionConflict(expectedVersion, current.Version);
    }

    private static ApplicationProblemException VersionConflict(
        long? expectedVersion,
        long currentVersion)
    {
        return new ApplicationProblemException(
            "RECEIVING_POC_VERSION_CONFLICT",
            "The POC Receiving Session changed before this operation was applied.",
            ApplicationErrorCategory.Conflict,
            details: new Dictionary<string, object?>
            {
                ["expectedVersion"] = expectedVersion,
                ["currentVersion"] = currentVersion,
            });
    }

    private static ApplicationProblemException SequenceProblem(
        long supplied,
        long expected)
    {
        return supplied < expected
            ? Problem(
                "RECEIVING_POC_SEQUENCE_CONFLICT",
                "The local sequence was already consumed.",
                ApplicationErrorCategory.Conflict)
            : Problem(
                "RECEIVING_POC_SEQUENCE_GAP",
                "The next ordered local operation is missing.",
                ApplicationErrorCategory.Conflict);
    }

    private static void ApplyDomain(Action action)
    {
        try
        {
            action();
        }
        catch (ReceivingPocDomainException exception)
        {
            throw Problem(
                exception.Code,
                exception.Message,
                exception.Code is "RECEIVING_POC_ENTRY_REQUIRED"
                    ? ApplicationErrorCategory.Validation
                    : ApplicationErrorCategory.Conflict,
                exception);
        }
    }

    private void AddAudit(
        Guid workspaceId,
        Guid deviceId,
        string action,
        Guid aggregateId,
        object snapshot,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.AuditEvents.Add(
            AuditEvent.Create(
                workspaceId,
                null,
                null,
                null,
                deviceId,
                action,
                AggregateType,
                aggregateId,
                null,
                null,
                null,
                JsonSerializer.Serialize(snapshot, JsonOptions),
                correlationId,
                occurredAtUtc));
    }

    private void AddEvent(
        Guid workspaceId,
        string eventType,
        Guid aggregateId,
        long aggregateVersion,
        object payload,
        string correlationId,
        DateTimeOffset occurredAtUtc)
    {
        dbContext.OutboxMessages.Add(
            OutboxMessage.Create(
                workspaceId,
                eventType,
                EventVersion,
                AggregateType,
                aggregateId,
                aggregateVersion,
                JsonSerializer.Serialize(payload, JsonOptions),
                correlationId,
                occurredAtUtc,
                OutboxEventStream.MobileSync));
    }

    private static ReceivingPocCommandResult ToResult(
        ReceivingSessionPoc session,
        long version,
        Guid? finalizationId = null)
    {
        return new ReceivingPocCommandResult(
            session.Id,
            Reference(session.CloudReferenceSequence),
            session.Status.ToString(),
            session.EditorDeviceId,
            session.LeaseId,
            session.LeaseExpiresAtUtc,
            session.EntryCount,
            Weight(session.ProcessedTotalWeightKg),
            version,
            finalizationId);
    }

    private static bool IsReplayable(ReceivingPocCommandResult result)
    {
        return result.SessionId != Guid.Empty &&
            result.Version > 0 &&
            result.CloudReference.StartsWith(
                "RS-POC-",
                StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(result.Status);
    }

    private static string Reference(long sequence)
    {
        return $"RS-POC-{sequence:000000}";
    }

    private static string Weight(decimal value)
    {
        return value.ToString("0.000000", CultureInfo.InvariantCulture);
    }

    private static string DisplayWeight(decimal value, int decimalPlaces)
    {
        return value.ToString(
            $"0.{new string('0', decimalPlaces)}",
            CultureInfo.InvariantCulture);
    }

    private static MobileSyncOperationResult Failure(
        CorrelatedOperation operation,
        string resultStatus,
        string code,
        string message)
    {
        return new MobileSyncOperationResult(
            operation.OperationId,
            operation.AggregateId,
            operation.LocalSequence,
            resultStatus,
            ErrorCode: code,
            Message: message);
    }

    private static ApplicationProblemException Problem(
        string code,
        string message,
        ApplicationErrorCategory category,
        Exception? innerException = null)
    {
        return new ApplicationProblemException(
            code,
            message,
            category,
            innerException: innerException);
    }

    private sealed record ValidatedOperation(
        Guid OperationId,
        string OperationType,
        Guid AggregateId,
        long LocalSequence,
        long? ExpectedCloudVersion,
        string PayloadJson,
        string PayloadHash,
        Guid? LeaseId);

    private sealed record CorrelatedOperation(
        Guid OperationId,
        Guid AggregateId,
        long LocalSequence,
        string? OperationType,
        long? ExpectedCloudVersion,
        string? PayloadJson,
        string? PayloadHash,
        string? LeaseId);
}
