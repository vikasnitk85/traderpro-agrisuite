using TraderPro.Domain.Common.Measurements;

namespace TraderPro.UnitTests;

public sealed class WeightProcessorTests
{
    [Fact]
    public void Raw_text_is_preserved_exactly()
    {
        var result = WeightProcessor.Process(
            "00050.2300",
            2,
            "Ceiling");

        Assert.Equal("00050.2300", result.RawWeightKg);
        Assert.Equal("50.230000", result.ProcessedWeightKg);
    }

    [Fact]
    public void Standard_uses_half_up_at_an_exact_midpoint()
    {
        var result = WeightProcessor.Process("50.25", 1, "Standard");

        Assert.Equal("50.300000", result.ProcessedWeightKg);
    }

    [Fact]
    public void Ceiling_does_not_increase_an_exact_value()
    {
        var result = WeightProcessor.Process("50.2300", 2, "Ceiling");

        Assert.Equal("50.230000", result.ProcessedWeightKg);
    }

    [Fact]
    public void Standard_can_carry_into_the_integer_part()
    {
        var result = WeightProcessor.Process("99.999", 2, "Standard");

        Assert.Equal("100.000000", result.ProcessedWeightKg);
        Assert.Equal("100.00", result.DisplayWeightKg);
    }

    [Fact]
    public void Ceiling_processes_the_smallest_supported_positive_value()
    {
        var result = WeightProcessor.Process("0.000001", 3, "Ceiling");

        Assert.Equal("0.001000", result.ProcessedWeightKg);
    }

    [Fact]
    public void Maximum_supported_input_is_processed_without_overflow()
    {
        var result = WeightProcessor.Process(
            "99999999999999.999999",
            3,
            "Ceiling");

        Assert.Equal(
            "100000000000000.000000",
            result.ProcessedWeightKg);
    }

    [Fact]
    public void Invalid_decimal_syntax_exposes_a_stable_error_code()
    {
        var exception = Assert.Throws<WeightProcessingException>(
            () => WeightProcessor.Process("50,2", 2, "Standard"));

        Assert.Equal(
            WeightProcessingException.RawInvalid,
            exception.ErrorCode);
    }
}
