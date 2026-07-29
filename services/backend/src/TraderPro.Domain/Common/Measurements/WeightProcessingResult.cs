namespace TraderPro.Domain.Common.Measurements;

public sealed record WeightProcessingResult(
    string RawWeightKg,
    string ProcessedWeightKg,
    string DisplayWeightKg,
    int DecimalPlaces,
    WeightProcessingMethod Method);
