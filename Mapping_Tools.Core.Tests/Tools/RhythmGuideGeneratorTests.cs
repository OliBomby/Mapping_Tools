using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class RhythmGuideGeneratorTests
{
    [TestMethod]
    public void Append_WithAcceptedWaveZeroOptions_ReproducesLegacySemanticCounts()
    {
        // Arrange
        var source = Load("standard-feature-rich.osu");
        var target = Load("ComplicatedTestMap.osu");
        RhythmGuideOptions options = new()
        {
            SelectionMode = RhythmGuideSelectionMode.HitsoundEvents,
            NcEverything = true,
        };

        // Act
        RhythmGuideGenerator.Append(target, [source], options);

        // Assert
        target.HitObjects.Should().HaveCount(1344);
        target.HitObjects.Count(hitObject => hitObject.IsCircle).Should().Be(1336);
        target.HitObjects.Count(hitObject => hitObject.NewCombo).Should().Be(1334);
        target.BeatmapTiming.TimingPoints.Should().HaveCount(9);
    }

    [TestMethod]
    public void CreateNewMap_WithSource_RetainsRedlinesAndAppliesOutputMetadata()
    {
        // Arrange
        var source = Load("standard-feature-rich.osu");
        RhythmGuideOptions options = new()
        {
            OutputName = "Guide",
            SelectionMode = RhythmGuideSelectionMode.HitsoundEvents,
        };

        // Act
        var result = RhythmGuideGenerator.CreateNewMap([source], options);

        // Assert
        result.Metadata["Version"].Value.Should().Be("Guide");
        result.HitObjects.Should().NotBeEmpty();
        result.BeatmapTiming.TimingPoints.Should().OnlyContain(point => point.Uninherited);
    }

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(File.ReadAllLines(Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            fileName)).ToList());
    }
}
