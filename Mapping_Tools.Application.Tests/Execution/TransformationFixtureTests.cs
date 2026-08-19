using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Execution;

[TestClass]
public sealed class TransformationFixtureTests
{
    [DataTestMethod]
    [DataRow("auto-fail-detector", "Auto-fail Detector")]
    [DataRow("combo-colour-studio", "Combo Colour Studio")]
    [DataRow("hitsound-copier", "Hitsound Copier")]
    [DataRow("hitsound-preview", "Hitsound Preview Helper")]
    [DataRow("hitsound-studio", "Hitsound Studio")]
    [DataRow("map-cleaner", "Map Cleaner")]
    [DataRow("mapset-merger", "Mapset Merger")]
    [DataRow("metadata-manager", "Metadata Manager")]
    [DataRow("pattern-gallery", "Pattern Gallery")]
    [DataRow("property-transformer", "Property Transformer")]
    [DataRow("rhythm-guide", "Rhythm Guide")]
    [DataRow("slider-completionator", "Slider Completionator")]
    [DataRow("slider-merger", "Slider Merger")]
    [DataRow("slider-picturator", "Slider Picturator")]
    [DataRow("sliderator", "Sliderator")]
    [DataRow("timing-copier", "Timing Copier")]
    [DataRow("timing-helper", "Timing Helper")]
    [DataRow("tumour-generator", "Tumour Generator 2")]
    public void LoadAcceptedBaseline_UsesInputsOptionsOutputAndReport(string fixtureName, string feature)
    {
        // Arrange
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Transformations");
        string recordPath = Path.Combine(fixtureRoot, $"{fixtureName}.json");
        string optionsPath = Path.Combine(fixtureRoot, $"{fixtureName}.options.json");
        string reportPath = Path.Combine(fixtureRoot, $"{fixtureName}-report.md");
        using JsonDocument record = JsonDocument.Parse(File.ReadAllText(recordPath));
        using JsonDocument options = JsonDocument.Parse(File.ReadAllText(optionsPath));
        string expectedOutputReference = record.RootElement.GetProperty("expectedOutput").GetString()
            ?? throw new InvalidDataException("A transformation fixture has no expected output.");
        string expectedOutputPath = ResolveFixturePath(fixtureRoot, expectedOutputReference);
        string[] referencedInputs = EnumerateRelativePaths(record.RootElement)
            .Concat(EnumerateRelativePaths(options.RootElement))
            .Distinct(StringComparer.Ordinal)
            .Select(path => ResolveFixturePath(fixtureRoot, path))
            .ToArray();

        // Act
        byte[][] inputBytes = referencedInputs.Select(File.ReadAllBytes).ToArray();
        string expectedOutput = File.ReadAllText(expectedOutputPath);
        string report = File.ReadAllText(reportPath);

        // Assert
        record.RootElement.GetProperty("feature").GetString().Should().Be(feature);
        record.RootElement.GetProperty("status").GetString().Should().Be("accepted");
        options.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        referencedInputs.Should().NotBeEmpty();
        inputBytes.Should().OnlyContain(bytes => bytes.Length > 0);
        expectedOutput.Should().NotBeNullOrWhiteSpace();
        report.Should().NotBeNullOrWhiteSpace();

        if (fixtureName == "mapset-merger")
        {
            using JsonDocument mapsetOutput = JsonDocument.Parse(expectedOutput);
            string[] expectedBeatmaps = mapsetOutput.RootElement
                .GetProperty("versionedBeatmaps")
                .EnumerateArray()
                .Select(item => item.GetProperty("path").GetString()
                    ?? throw new InvalidDataException("A mapset baseline has no beatmap path."))
                .Select(path => ResolveFixturePath(fixtureRoot, path))
                .ToArray();

            expectedBeatmaps.Should().HaveCount(2);
            expectedBeatmaps.Select(File.ReadAllText).Should().OnlyContain(content => content.Contains("osu file format"));
        }
    }

    private static string ResolveFixturePath(string fixtureRoot, string relativePath)
    {
        string path = Path.GetFullPath(Path.Combine(fixtureRoot, relativePath));
        File.Exists(path).Should().BeTrue($"Fixture path does not exist: {relativePath}");
        return path;
    }

    private static IEnumerable<string> EnumerateRelativePaths(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                string? value = element.GetString();
                if (value?.StartsWith("../", StringComparison.Ordinal) == true)
                {
                    yield return value;
                }

                yield break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    foreach (string path in EnumerateRelativePaths(item))
                    {
                        yield return path;
                    }
                }

                yield break;

            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    foreach (string path in EnumerateRelativePaths(property.Value))
                    {
                        yield return path;
                    }
                }

                yield break;

            default:
                yield break;
        }
    }
}
