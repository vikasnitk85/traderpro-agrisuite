using TraderPro.Domain.Common;

namespace TraderPro.Domain.Platform;

public sealed class Company :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    private Company()
    {
    }

    private Company(
        Guid workspaceId,
        string code,
        string legalName,
        string? tradeName,
        string? taxRegistrationNumber,
        DateTimeOffset createdAtUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = workspaceId;
        Code = PlatformEntityGuard.Required(code, 64, nameof(code));
        LegalName = PlatformEntityGuard.Required(
            legalName,
            200,
            nameof(legalName));
        TradeName = PlatformEntityGuard.Optional(
            tradeName,
            200,
            nameof(tradeName));
        TaxRegistrationNumber = PlatformEntityGuard.Optional(
            taxRegistrationNumber,
            64,
            nameof(taxRegistrationNumber));
        Status = CompanyStatus.Active;
        CreatedAtUtc = PlatformEntityGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string? TradeName { get; private set; }

    public string? TaxRegistrationNumber { get; private set; }

    public CompanyStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public long Version { get; private set; }

    public static Company Create(
        Guid workspaceId,
        string code,
        string legalName,
        string? tradeName,
        string? taxRegistrationNumber,
        DateTimeOffset createdAtUtc)
    {
        return new Company(
            workspaceId,
            code,
            legalName,
            tradeName,
            taxRegistrationNumber,
            createdAtUtc);
    }

    public void UpdateNames(string legalName, string? tradeName)
    {
        LegalName = PlatformEntityGuard.Required(
            legalName,
            200,
            nameof(legalName));
        TradeName = PlatformEntityGuard.Optional(
            tradeName,
            200,
            nameof(tradeName));
    }

    public void SetStatus(CompanyStatus status)
    {
        Status = PlatformEntityGuard.Defined(status, nameof(status));
    }
}
