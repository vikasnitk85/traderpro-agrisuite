namespace TraderPro.Domain.Common.Measurements;

public sealed record WeightProcessingPolicy
{
    private WeightProcessingPolicy(
        int decimalPlaces,
        WeightProcessingMethod method)
    {
        DecimalPlaces = decimalPlaces;
        Method = method;
    }

    public int DecimalPlaces { get; }

    public WeightProcessingMethod Method { get; }

    public static WeightProcessingPolicy Create(
        int decimalPlaces,
        WeightProcessingMethod method)
    {
        ValidateDecimalPlaces(decimalPlaces);

        if (!Enum.IsDefined(method))
        {
            throw new WeightProcessingException(
                WeightProcessingException.ProcessingMethodInvalid,
                "The weight-processing method must be Standard, Floor, or Ceiling.");
        }

        return new WeightProcessingPolicy(decimalPlaces, method);
    }

    public static WeightProcessingPolicy Parse(
        int decimalPlaces,
        string? method)
    {
        ValidateDecimalPlaces(decimalPlaces);

        var parsedMethod = method switch
        {
            nameof(WeightProcessingMethod.Standard) =>
                WeightProcessingMethod.Standard,
            nameof(WeightProcessingMethod.Floor) =>
                WeightProcessingMethod.Floor,
            nameof(WeightProcessingMethod.Ceiling) =>
                WeightProcessingMethod.Ceiling,
            _ => throw new WeightProcessingException(
                WeightProcessingException.ProcessingMethodInvalid,
                "The weight-processing method must be exactly Standard, Floor, or Ceiling."),
        };

        return new WeightProcessingPolicy(decimalPlaces, parsedMethod);
    }

    private static void ValidateDecimalPlaces(int decimalPlaces)
    {
        if (decimalPlaces is < 1 or > 3)
        {
            throw new WeightProcessingException(
                WeightProcessingException.DecimalPlacesInvalid,
                "Weight decimal places must be exactly 1, 2, or 3.");
        }
    }
}
