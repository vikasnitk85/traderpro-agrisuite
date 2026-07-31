using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TraderPro.Application.Common.Errors;
using TraderPro.Application.Common.MasterData;
using TraderPro.Domain.Common.MasterData;

namespace TraderPro.Infrastructure.Modules.Shared;

internal static class CommercialMasterKinds
{
    public const string BusinessLocation = "Operations.BusinessLocation";
    public const string ReceivingVehicle = "Procurement.ReceivingVehicle";
    public const string BagType = "Procurement.BagType";
    public const string WeightProcessingPolicy =
        "Procurement.WeightProcessingPolicy";
}

internal sealed record CommercialMasterCursorScope(
    string MasterKind,
    Guid WorkspaceId,
    Guid CompanyId,
    Guid? BranchId,
    MasterStatusFilter Status,
    string? Search);

internal readonly record struct CommercialMasterCursorPosition(
    string Code,
    Guid Id);

internal static class CommercialMasterDataInfrastructure
{
    private const int CursorVersion = 1;
    private static readonly JsonSerializerOptions CursorJsonOptions =
        new(JsonSerializerDefaults.Web);

    public static string NormalizeCode(string code)
    {
        try
        {
            return MasterDataValueRules.NormalizeCode(code);
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }
    }

    public static T Domain<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }
    }

    public static void Domain(Action action)
    {
        try
        {
            action();
        }
        catch (MasterDataDomainException exception)
        {
            throw MasterDataProblem.FromDomain(exception);
        }
    }

    public static IQueryable<T> ApplyStatus<T>(
        IQueryable<T> query,
        MasterStatusFilter filter,
        System.Linq.Expressions.Expression<Func<T, MasterDataStatus>> status)
    {
        return filter switch
        {
            MasterStatusFilter.Active =>
                query.Where(Equal(status, MasterDataStatus.Active)),
            MasterStatusFilter.Inactive =>
                query.Where(Equal(status, MasterDataStatus.Inactive)),
            _ => query,
        };
    }

    public static CommercialMasterCursorPosition? DecodeCursor(
        string? cursor,
        CommercialMasterCursorScope scope)
    {
        if (cursor is null)
        {
            return null;
        }

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(
                base64.Length + ((4 - base64.Length % 4) % 4),
                '=');
            var payload = JsonSerializer.Deserialize<MasterCursorPayload>(
                Convert.FromBase64String(base64),
                CursorJsonOptions);
            if (payload is null ||
                payload.Version != CursorVersion ||
                !string.Equals(
                    payload.MasterKind,
                    scope.MasterKind,
                    StringComparison.Ordinal) ||
                payload.WorkspaceId != scope.WorkspaceId ||
                payload.CompanyId != scope.CompanyId ||
                payload.BranchId != scope.BranchId ||
                !string.Equals(
                    payload.Status,
                    scope.Status.ToString(),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    payload.Search,
                    scope.Search,
                    StringComparison.Ordinal) ||
                payload.Id == Guid.Empty ||
                string.IsNullOrEmpty(payload.Code))
            {
                throw new FormatException();
            }

            if (!string.Equals(
                    payload.Code,
                    MasterDataValueRules.NormalizeCode(payload.Code),
                    StringComparison.Ordinal))
            {
                throw new FormatException();
            }

            return new CommercialMasterCursorPosition(
                payload.Code,
                payload.Id);
        }
        catch (Exception exception)
            when (exception is FormatException or JsonException or
                MasterDataDomainException)
        {
            throw new ApplicationProblemException(
                "MASTER_CURSOR_INVALID",
                "The list cursor is invalid.",
                ApplicationErrorCategory.Validation);
        }
    }

    public static string EncodeCursor(
        CommercialMasterCursorScope scope,
        string code,
        Guid id)
    {
        var base64 = Convert.ToBase64String(
            JsonSerializer.SerializeToUtf8Bytes(
                new MasterCursorPayload(
                    CursorVersion,
                    scope.MasterKind,
                    scope.WorkspaceId,
                    scope.CompanyId,
                    scope.BranchId,
                    scope.Status.ToString(),
                    scope.Search,
                    code,
                    id),
                CursorJsonOptions));
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static ApplicationProblemException VersionConflict(
        long expectedVersion,
        long currentVersion)
    {
        return new ApplicationProblemException(
            "MASTER_VERSION_CONFLICT",
            "The master record changed before this command was applied.",
            ApplicationErrorCategory.Conflict,
            details: new Dictionary<string, object?>
            {
                ["expectedVersion"] = expectedVersion,
                ["currentVersion"] = currentVersion,
            });
    }

    public static ApplicationProblemException Conflict(
        string code,
        string message)
    {
        return new ApplicationProblemException(
            code,
            message,
            ApplicationErrorCategory.Conflict);
    }

    public static PostgresException? PostgreSql(Exception exception)
    {
        return exception switch
        {
            PostgresException postgres => postgres,
            DbUpdateException { InnerException: PostgresException postgres } =>
                postgres,
            _ => null,
        };
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> Equal<T>(
        System.Linq.Expressions.Expression<Func<T, MasterDataStatus>> selector,
        MasterDataStatus status)
    {
        var body = System.Linq.Expressions.Expression.Equal(
            selector.Body,
            System.Linq.Expressions.Expression.Constant(status));
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
            body,
            selector.Parameters);
    }

    private sealed record MasterCursorPayload(
        int Version,
        string MasterKind,
        Guid WorkspaceId,
        Guid CompanyId,
        Guid? BranchId,
        string Status,
        string? Search,
        string Code,
        Guid Id);
}
