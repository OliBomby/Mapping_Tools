using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.Sliderator;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Application.Tests.Tools.Sliderator;

[TestClass]
public sealed class SlideratorFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task RunAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("sliderator", fixtureName);
        SlideratorServiceOptions project = fixture.ReadProject<SlideratorServiceOptions>();
        HitObject sourceSlider = ReadLegacySlider(fixture);
        ApplySlideratorTransientState(project, sourceSlider);
        SlideratorService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.RunAsync(
            fixture.TargetPath,
            project,
            sourceSlider,
            false,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Sliderator");
        fixture.AssertTextOutput(actual);
    }

    private static HitObject ReadLegacySlider(FixtureContext fixture)
    {
        string projectPath = Path.Combine(fixture.FixtureRoot, "project.json");
        var document = JObject.Parse(File.ReadAllText(projectPath));
        string sliderJson = document["LoadedHitObjects"]?.SingleOrDefault()?.ToString()
                            ?? throw new InvalidDataException(
                                "The legacy Sliderator fixture contained no source slider.");
        return new LegacyProjectJsonSerializer().Deserialize<HitObject>(sliderJson);
    }

    private static void ApplySlideratorTransientState(
        SlideratorServiceOptions project,
        HitObject sourceSlider)
    {
        double temporalLength = sourceSlider.TemporalLength;
        double beatsPerMinute = sourceSlider.UnInheritedTimingPoint?.GetBpm() ?? 180;
        project.BeatsPerMinute = beatsPerMinute > 0 ? beatsPerMinute : 180;
        project.GraphBeats = project.BeatsPerMinute * temporalLength / 60000;
        project.PixelLength = sourceSlider.PixelLength;
        project.NewVelocity = SlideratorEngine.GetMaximumVelocity(project);
    }
}
