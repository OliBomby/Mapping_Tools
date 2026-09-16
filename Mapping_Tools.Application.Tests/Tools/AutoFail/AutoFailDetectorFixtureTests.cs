using System.Text.Json;
using System.Text.Json.Nodes;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.AutoFail;

[TestClass]
public sealed class AutoFailDetectorFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task AnalyzeAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("auto-fail-detector", fixtureName);
        AutoFailServiceOptions options = new(
            fixture.TargetPath,
            fixture.NumberProperty("ApproachRateOverride"),
            fixture.NumberProperty("OverallDifficultyOverride"),
            fixture.IntProperty("PhysicsUpdateLeniency"));
        AutoFailService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        var positive = await service.AnalyzeAsync(options, CancellationToken.None);
        var negative = await service.AnalyzeAsync(
            options with { Path = fixture.RequiredPath("NegativeControl") },
            CancellationToken.None);
        string actualOutput = JsonSerializer.Serialize(new
        {
            autoFailDetected = positive.Analysis.HasAutoFail,
            unloadingObjects = positive.Analysis.UnloadingObjects.Count,
            potentialUnloadingObjects = positive.Analysis.PotentialUnloadingObjects.Count,
            message = AutoFailMessage(positive.Analysis),
            negativeControlMessage = AutoFailMessage(negative.Analysis),
        });

        // Assert
        fixture.AssertAccepted("Auto-fail Detector");
        AssertJsonOutput(fixture, new FixtureExecutionResult(JsonOutput: actualOutput));
    }

    private static string AutoFailMessage(AutoFailAnalysis analysis)
    {
        return analysis.HasAutoFail
            ? $"{analysis.UnloadingObjects.Count} unloading objects detected and "
              + $"{analysis.PotentialUnloadingObjects.Count} potential unloading objects detected!"
            : "No auto-fail detected.";
    }

    private static void AssertJsonOutput(FixtureContext fixture, FixtureExecutionResult actual)
    {
        actual.WasExecuted.Should().BeTrue();
        actual.JsonOutput.Should().NotBeNull();
        JsonNode.DeepEquals(
                JsonNode.Parse(File.ReadAllText(fixture.ExpectedOutputPath)),
                JsonNode.Parse(actual.JsonOutput!))
            .Should().BeTrue();
    }
}
