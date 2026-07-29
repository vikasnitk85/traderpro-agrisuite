using System.Globalization;
using System.Numerics;

namespace TraderPro.Domain.Common.Measurements;

public static class WeightProcessor
{
    private const int StorageDecimalPlaces = 6;
    private const int MaximumIntegerDigits = 14;
    private const int MaximumFractionDigits = 6;
    private static readonly BigInteger StorageScale =
        BigInteger.Pow(10, StorageDecimalPlaces);

    public static WeightProcessingResult Process(
        string? rawWeightKg,
        int decimalPlaces,
        string? method)
    {
        return Process(
            rawWeightKg,
            WeightProcessingPolicy.Parse(decimalPlaces, method));
    }

    public static WeightProcessingResult Process(
        string? rawWeightKg,
        WeightProcessingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var rawScaledWeight = ParseRawWeight(rawWeightKg);
        var quantum = BigInteger.Pow(
            10,
            StorageDecimalPlaces - policy.DecimalPlaces);
        var units = BigInteger.DivRem(
            rawScaledWeight,
            quantum,
            out var remainder);

        units = policy.Method switch
        {
            WeightProcessingMethod.Standard
                when remainder * 2 >= quantum => units + 1,
            WeightProcessingMethod.Ceiling
                when !remainder.IsZero => units + 1,
            WeightProcessingMethod.Standard or
                WeightProcessingMethod.Floor or
                WeightProcessingMethod.Ceiling => units,
            _ => throw new WeightProcessingException(
                WeightProcessingException.ProcessingMethodInvalid,
                "The weight-processing method must be Standard, Floor, or Ceiling."),
        };

        var processedScaledWeight = units * quantum;

        return new WeightProcessingResult(
            rawWeightKg!,
            FormatWeight(processedScaledWeight, StorageDecimalPlaces),
            FormatWeight(processedScaledWeight, policy.DecimalPlaces),
            policy.DecimalPlaces,
            policy.Method);
    }

    private static BigInteger ParseRawWeight(string? rawWeightKg)
    {
        if (string.IsNullOrWhiteSpace(rawWeightKg))
        {
            throw new WeightProcessingException(
                WeightProcessingException.RawRequired,
                "Raw weight is required.");
        }

        if (rawWeightKg[0] == '-')
        {
            throw new WeightProcessingException(
                WeightProcessingException.RawNegative,
                "Raw weight cannot be negative.");
        }

        var decimalSeparatorIndex = rawWeightKg.IndexOf('.');
        if (decimalSeparatorIndex == 0 ||
            decimalSeparatorIndex == rawWeightKg.Length - 1 ||
            decimalSeparatorIndex != rawWeightKg.LastIndexOf('.'))
        {
            throw InvalidRawWeight();
        }

        for (var index = 0; index < rawWeightKg.Length; index++)
        {
            if (index == decimalSeparatorIndex)
            {
                continue;
            }

            if (rawWeightKg[index] is < '0' or > '9')
            {
                throw InvalidRawWeight();
            }
        }

        var integerDigits = decimalSeparatorIndex < 0
            ? rawWeightKg.Length
            : decimalSeparatorIndex;
        var fractionDigits = decimalSeparatorIndex < 0
            ? 0
            : rawWeightKg.Length - decimalSeparatorIndex - 1;

        if (integerDigits > MaximumIntegerDigits)
        {
            throw new WeightProcessingException(
                WeightProcessingException.RawIntegerDigitsExceeded,
                $"Raw weight cannot have more than {MaximumIntegerDigits} integer digits.");
        }

        if (fractionDigits > MaximumFractionDigits)
        {
            throw new WeightProcessingException(
                WeightProcessingException.RawFractionDigitsExceeded,
                $"Raw weight cannot have more than {MaximumFractionDigits} fractional digits.");
        }

        var integerPart = BigInteger.Parse(
            rawWeightKg.AsSpan(0, integerDigits),
            NumberStyles.None,
            CultureInfo.InvariantCulture);
        var fractionalPart = fractionDigits == 0
            ? BigInteger.Zero
            : BigInteger.Parse(
                rawWeightKg.AsSpan(
                    decimalSeparatorIndex + 1,
                    fractionDigits),
                NumberStyles.None,
                CultureInfo.InvariantCulture) *
              BigInteger.Pow(
                  10,
                  StorageDecimalPlaces - fractionDigits);

        return integerPart * StorageScale + fractionalPart;
    }

    private static string FormatWeight(
        BigInteger scaledWeight,
        int decimalPlaces)
    {
        var integerPart = BigInteger.DivRem(
            scaledWeight,
            StorageScale,
            out var sixDigitFraction);
        var displayDivisor = BigInteger.Pow(
            10,
            StorageDecimalPlaces - decimalPlaces);
        var displayedFraction = sixDigitFraction / displayDivisor;

        return string.Concat(
            integerPart.ToString(CultureInfo.InvariantCulture),
            ".",
            displayedFraction.ToString(
                $"D{decimalPlaces}",
                CultureInfo.InvariantCulture));
    }

    private static WeightProcessingException InvalidRawWeight()
    {
        return new WeightProcessingException(
            WeightProcessingException.RawInvalid,
            "Raw weight must use unsigned decimal notation with a dot separator.");
    }
}
