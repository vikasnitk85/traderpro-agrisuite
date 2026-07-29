using TraderPro.Domain.Common;
using TraderPro.Domain.Common.Measurements;

namespace TraderPro.Domain.Procurement.Poc;

/// <summary>
/// Immutable cloud-accepted physical fact for the Task 6A proof of concept.
/// </summary>
public sealed class ReceivingEntryPoc : IWorkspaceScoped
{
    public const int MaximumProductReferenceLength = 200;
    public const int MaximumBagTypeReferenceLength = 100;
    public const int MaximumWeightSourceLength = 50;

    private ReceivingEntryPoc()
    {
    }

    private ReceivingEntryPoc(
        Guid workspaceId,
        Guid receivingSessionId,
        Guid operationId,
        long localSequence,
        string productReference,
        string bagTypeReference,
        int bagCount,
        WeightProcessingResult processed,
        string weightSource,
        DateTimeOffset capturedAtDeviceUtc,
        DateTimeOffset acceptedAtServerUtc)
    {
        Id = Uuid7.NewGuid();
        WorkspaceId = RequiredId(workspaceId, nameof(workspaceId));
        ReceivingSessionId = RequiredId(
            receivingSessionId,
            nameof(receivingSessionId));
        OperationId = RequiredId(operationId, nameof(operationId));
        if (localSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(localSequence));
        }

        if (bagCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bagCount));
        }

        LocalSequence = localSequence;
        ProductReference = Required(
            productReference,
            MaximumProductReferenceLength,
            nameof(productReference));
        BagTypeReference = Required(
            bagTypeReference,
            MaximumBagTypeReferenceLength,
            nameof(bagTypeReference));
        BagCount = bagCount;
        RawWeightKg = processed.RawWeightKg;
        ProcessedWeightKg = decimal.Parse(
            processed.ProcessedWeightKg,
            System.Globalization.CultureInfo.InvariantCulture);
        DisplayWeightKg = decimal.Parse(
            processed.DisplayWeightKg,
            System.Globalization.CultureInfo.InvariantCulture);
        DecimalPlaces = processed.DecimalPlaces;
        ProcessingMethod = processed.Method.ToString();
        WeightSource = Required(
            weightSource,
            MaximumWeightSourceLength,
            nameof(weightSource));
        CapturedAtDeviceUtc = RequireUtc(
            capturedAtDeviceUtc,
            nameof(capturedAtDeviceUtc));
        AcceptedAtServerUtc = RequireUtc(
            acceptedAtServerUtc,
            nameof(acceptedAtServerUtc));
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid ReceivingSessionId { get; private set; }

    public Guid OperationId { get; private set; }

    public long LocalSequence { get; private set; }

    public string ProductReference { get; private set; } = string.Empty;

    public string BagTypeReference { get; private set; } = string.Empty;

    public int BagCount { get; private set; }

    public string RawWeightKg { get; private set; } = string.Empty;

    public decimal ProcessedWeightKg { get; private set; }

    public decimal DisplayWeightKg { get; private set; }

    public int DecimalPlaces { get; private set; }

    public string ProcessingMethod { get; private set; } = string.Empty;

    public string WeightSource { get; private set; } = string.Empty;

    public DateTimeOffset CapturedAtDeviceUtc { get; private set; }

    public DateTimeOffset AcceptedAtServerUtc { get; private set; }

    public static ReceivingEntryPoc Create(
        Guid workspaceId,
        Guid receivingSessionId,
        Guid operationId,
        long localSequence,
        string productReference,
        string bagTypeReference,
        int bagCount,
        WeightProcessingResult processed,
        string weightSource,
        DateTimeOffset capturedAtDeviceUtc,
        DateTimeOffset acceptedAtServerUtc)
    {
        ArgumentNullException.ThrowIfNull(processed);
        return new ReceivingEntryPoc(
            workspaceId,
            receivingSessionId,
            operationId,
            localSequence,
            productReference,
            bagTypeReference,
            bagCount,
            processed,
            weightSource,
            capturedAtDeviceUtc,
            acceptedAtServerUtc);
    }

    private static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "A non-empty identifier is required.",
                parameterName);
        }

        return value;
    }

    private static string Required(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private static DateTimeOffset RequireUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "The timestamp must use a zero UTC offset.",
                parameterName);
        }

        return value;
    }
}
