using System.Text.Json;
using TraderPro.Domain.Common.Measurements;

namespace TraderPro.UnitTests;

public sealed class WeightProcessingGoldenVectorTests
{
    [Fact]
    public void Golden_vector_metadata_matches_the_supported_contract()
    {
        using var document = LoadGoldenVectors();
        var root = document.RootElement;

        Assert.Equal(
            "TraderPro.WeightProcessing",
            root.GetProperty("contract").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal(
            6,
            root.GetProperty("storageDecimalPlaces").GetInt32());
        Assert.True(root.GetProperty("validCases").GetArrayLength() >= 30);
        Assert.True(root.GetProperty("invalidCases").GetArrayLength() >= 14);
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void Valid_golden_vector_matches_the_domain_processor(
        string name,
        string rawWeightKg,
        int decimalPlaces,
        string method,
        string expectedProcessedWeightKg,
        string expectedDisplayWeightKg)
    {
        var result = WeightProcessor.Process(
            rawWeightKg,
            decimalPlaces,
            method);
        var context = $"Golden vector '{name}'";

        Assert.True(
            result.RawWeightKg == rawWeightKg,
            $"{context} did not preserve the raw weight.");
        Assert.True(
            result.ProcessedWeightKg == expectedProcessedWeightKg,
            $"{context} produced '{result.ProcessedWeightKg}' instead of " +
            $"'{expectedProcessedWeightKg}'.");
        Assert.True(
            result.DisplayWeightKg == expectedDisplayWeightKg,
            $"{context} displayed '{result.DisplayWeightKg}' instead of " +
            $"'{expectedDisplayWeightKg}'.");
        Assert.True(
            result.DecimalPlaces == decimalPlaces,
            $"{context} did not preserve decimal places.");
        Assert.True(
            result.Method.ToString() == method,
            $"{context} did not preserve the processing method.");
    }

    [Theory]
    [MemberData(nameof(InvalidCases))]
    public void Invalid_golden_vector_exposes_the_expected_error_code(
        string name,
        string rawWeightKg,
        int decimalPlaces,
        string method,
        string expectedErrorCode)
    {
        var exception = Assert.Throws<WeightProcessingException>(
            () => WeightProcessor.Process(
                rawWeightKg,
                decimalPlaces,
                method));

        Assert.True(
            exception.ErrorCode == expectedErrorCode,
            $"Golden vector '{name}' returned '{exception.ErrorCode}' " +
            $"instead of '{expectedErrorCode}'.");
    }

    public static IEnumerable<object[]> ValidCases()
    {
        using var document = LoadGoldenVectors();

        foreach (var vector in document
                     .RootElement
                     .GetProperty("validCases")
                     .EnumerateArray())
        {
            yield return
            [
                GetRequiredString(vector, "name"),
                GetRequiredString(vector, "rawWeightKg"),
                vector.GetProperty("decimalPlaces").GetInt32(),
                GetRequiredString(vector, "method"),
                GetRequiredString(vector, "expectedProcessedWeightKg"),
                GetRequiredString(vector, "expectedDisplayWeightKg"),
            ];
        }
    }

    public static IEnumerable<object[]> InvalidCases()
    {
        using var document = LoadGoldenVectors();

        foreach (var vector in document
                     .RootElement
                     .GetProperty("invalidCases")
                     .EnumerateArray())
        {
            yield return
            [
                GetRequiredString(vector, "name"),
                GetRequiredString(vector, "rawWeightKg"),
                vector.GetProperty("decimalPlaces").GetInt32(),
                GetRequiredString(vector, "method"),
                GetRequiredString(vector, "expectedErrorCode"),
            ];
        }
    }

    private static JsonDocument LoadGoldenVectors()
    {
        var repositoryRoot = FindRepositoryRoot();
        var vectorPath = Path.Combine(
            repositoryRoot,
            "contracts",
            "golden-vectors",
            "weight-processing.v1.json");

        return JsonDocument.Parse(File.ReadAllText(vectorPath));
    }

    private static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        return element.GetProperty(propertyName).GetString() ??
               throw new InvalidDataException(
                   $"Golden-vector property '{propertyName}' must be a string.");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var solutionPath = Path.Combine(
                directory.FullName,
                "TraderPro.sln");
            var vectorPath = Path.Combine(
                directory.FullName,
                "contracts",
                "golden-vectors",
                "weight-processing.v1.json");
            if (File.Exists(solutionPath) && File.Exists(vectorPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repository root and weight golden vectors.");
    }
}
