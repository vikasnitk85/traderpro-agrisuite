using System.Globalization;
using System.Text.RegularExpressions;
using TraderPro.Domain.Common;

namespace TraderPro.Domain.Procurement.Receiving;

public enum CommercialReceivingReferenceResetPolicy : short
{
    Never = 1,
    CalendarYear = 2,
    Monthly = 3,
}

public sealed partial class CommercialReceivingReferencePolicy :
    IWorkspaceScoped,
    IVersionedEntity,
    ICreatedAtUtc,
    IUpdatedAtUtc
{
    public const string DocumentTypeValue = "CommercialReceiving";
    public const string DefaultFormatTemplate = "RCV-{SEQ:000000}";
    public const int MaximumFormatLength = 100;

    private CommercialReceivingReferencePolicy()
    {
    }

    private CommercialReceivingReferencePolicy(
        Guid workspaceId,
        Guid companyId,
        string formatTemplate,
        CommercialReceivingReferenceResetPolicy resetPolicy,
        long startingNumber,
        DateTimeOffset now)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        CompanyId = RequiredId(companyId, nameof(companyId));
        DocumentType = DocumentTypeValue;
        FormatTemplate = ValidateTemplate(formatTemplate, resetPolicy);
        ResetPolicy = Defined(resetPolicy);
        StartingNumber = Positive(startingNumber, nameof(startingNumber));
        CreatedAtUtc = Utc(now, nameof(now));
        UpdatedAtUtc = CreatedAtUtc;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string FormatTemplate { get; private set; } = string.Empty;
    public CommercialReceivingReferenceResetPolicy ResetPolicy { get; private set; }
    public long StartingNumber { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static CommercialReceivingReferencePolicy CreateDefault(
        Guid workspaceId,
        Guid companyId,
        DateTimeOffset now) =>
        Create(
            workspaceId,
            companyId,
            DefaultFormatTemplate,
            CommercialReceivingReferenceResetPolicy.Never,
            1,
            now);

    public static CommercialReceivingReferencePolicy Create(
        Guid workspaceId,
        Guid companyId,
        string formatTemplate,
        CommercialReceivingReferenceResetPolicy resetPolicy,
        long startingNumber,
        DateTimeOffset now) =>
        new(
            workspaceId,
            companyId,
            formatTemplate,
            resetPolicy,
            startingNumber,
            now);

    public void Update(
        string formatTemplate,
        CommercialReceivingReferenceResetPolicy resetPolicy,
        long startingNumber,
        bool seriesHasIssued,
        DateTimeOffset now)
    {
        if (seriesHasIssued && startingNumber != StartingNumber)
        {
            throw new CommercialReceivingDomainException(
                "RECEIVING_REFERENCE_SERIES_STARTED",
                "The starting number cannot change after the series has issued a reference.");
        }

        FormatTemplate = ValidateTemplate(formatTemplate, resetPolicy);
        ResetPolicy = Defined(resetPolicy);
        StartingNumber = Positive(startingNumber, nameof(startingNumber));
        UpdatedAtUtc = Utc(now, nameof(now));
        Version = checked(Version + 1);
    }

    public string PeriodKey(DateTimeOffset serverNow)
    {
        var now = Utc(serverNow, nameof(serverNow));
        return ResetPolicy switch
        {
            CommercialReceivingReferenceResetPolicy.Never => "ALL",
            CommercialReceivingReferenceResetPolicy.CalendarYear =>
                now.Year.ToString("D4", CultureInfo.InvariantCulture),
            CommercialReceivingReferenceResetPolicy.Monthly =>
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{now.Year:D4}-{now.Month:D2}"),
            _ => throw InvalidPolicy(),
        };
    }

    public string Render(long sequence, DateTimeOffset serverNow)
    {
        Positive(sequence, nameof(sequence));
        var now = Utc(serverNow, nameof(serverNow));
        var match = SequenceToken().Match(FormatTemplate);
        var width = match.Groups[1].Value.Length;
        var rendered = SequenceToken().Replace(
            FormatTemplate,
            sequence.ToString($"D{width}", CultureInfo.InvariantCulture));
        return rendered
            .Replace("{YYYY}", now.Year.ToString("D4", CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{MM}", now.Month.ToString("D2", CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    public static string ValidateTemplate(
        string? value,
        CommercialReceivingReferenceResetPolicy resetPolicy)
    {
        Defined(resetPolicy);
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaximumFormatLength ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            throw InvalidPolicy();
        }

        var matches = SequenceToken().Matches(value);
        if (matches.Count != 1)
        {
            throw InvalidPolicy();
        }

        var withoutTokens = SequenceToken().Replace(value, string.Empty)
            .Replace("{YYYY}", string.Empty, StringComparison.Ordinal)
            .Replace("{MM}", string.Empty, StringComparison.Ordinal);
        if (withoutTokens.Contains('{', StringComparison.Ordinal) ||
            withoutTokens.Contains('}', StringComparison.Ordinal) ||
            withoutTokens.Any(char.IsControl))
        {
            throw InvalidPolicy();
        }

        if (resetPolicy is CommercialReceivingReferenceResetPolicy.CalendarYear &&
            !value.Contains("{YYYY}", StringComparison.Ordinal))
        {
            throw InvalidPolicy();
        }

        if (resetPolicy is CommercialReceivingReferenceResetPolicy.Monthly &&
            (!value.Contains("{YYYY}", StringComparison.Ordinal) ||
             !value.Contains("{MM}", StringComparison.Ordinal)))
        {
            throw InvalidPolicy();
        }

        return value;
    }

    private static Guid RequiredId(Guid value, string name) =>
        value != Guid.Empty
            ? value
            : throw new ArgumentException("A non-empty identifier is required.", name);

    private static long Positive(long value, string name) =>
        value > 0
            ? value
            : throw new CommercialReceivingDomainException(
                "RECEIVING_REFERENCE_POLICY_INVALID",
                $"{name} must be positive.");

    private static DateTimeOffset Utc(DateTimeOffset value, string name) =>
        value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException("The timestamp must be UTC.", name);

    private static CommercialReceivingReferenceResetPolicy Defined(
        CommercialReceivingReferenceResetPolicy value) =>
        Enum.IsDefined(value) ? value : throw InvalidPolicy();

    private static CommercialReceivingDomainException InvalidPolicy() =>
        new(
            "RECEIVING_REFERENCE_POLICY_INVALID",
            "The commercial Receiving reference policy is invalid.");

    [GeneratedRegex("\\{SEQ:(0{1,12})\\}", RegexOptions.CultureInvariant)]
    private static partial Regex SequenceToken();
}

public sealed class CommercialReceivingReferenceCounter :
    IWorkspaceScoped,
    IVersionedEntity
{
    private CommercialReceivingReferenceCounter()
    {
    }

    private CommercialReceivingReferenceCounter(
        Guid workspaceId,
        Guid companyId,
        Guid policyId,
        string periodKey,
        long startingNumber)
    {
        WorkspaceId = workspaceId;
        CompanyId = companyId;
        PolicyId = policyId;
        PeriodKey = periodKey;
        NextNumber = startingNumber;
        Version = 1;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PolicyId { get; private set; }
    public string PeriodKey { get; private set; } = string.Empty;
    public long NextNumber { get; private set; }
    public long Version { get; private set; }

    public static CommercialReceivingReferenceCounter Create(
        Guid workspaceId,
        Guid companyId,
        Guid policyId,
        string periodKey,
        long startingNumber) =>
        new(workspaceId, companyId, policyId, periodKey, startingNumber);

    public long Allocate()
    {
        var allocated = NextNumber;
        NextNumber = checked(NextNumber + 1);
        Version = checked(Version + 1);
        return allocated;
    }
}

