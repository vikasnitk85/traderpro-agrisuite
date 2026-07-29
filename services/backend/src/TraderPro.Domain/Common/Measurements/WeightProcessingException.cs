namespace TraderPro.Domain.Common.Measurements;

public sealed class WeightProcessingException : Exception
{
    public const string RawRequired = "WEIGHT_RAW_REQUIRED";
    public const string RawInvalid = "WEIGHT_RAW_INVALID";
    public const string RawNegative = "WEIGHT_RAW_NEGATIVE";
    public const string RawIntegerDigitsExceeded =
        "WEIGHT_RAW_INTEGER_DIGITS_EXCEEDED";
    public const string RawFractionDigitsExceeded =
        "WEIGHT_RAW_FRACTION_DIGITS_EXCEEDED";
    public const string DecimalPlacesInvalid =
        "WEIGHT_DECIMAL_PLACES_INVALID";
    public const string ProcessingMethodInvalid =
        "WEIGHT_PROCESSING_METHOD_INVALID";

    public WeightProcessingException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
