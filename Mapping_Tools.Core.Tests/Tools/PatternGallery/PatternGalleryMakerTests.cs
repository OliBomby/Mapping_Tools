using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryMakerTests
{
    [TestMethod]
    public void FromObjects_WithValidObjectAndTimingLines_CreatesIndexedPatternAndMap()
    {
        // Arrange
        PatternGalleryMaker maker = new();
        List<HitObject> hitObjects =
        [
            new HitObject("64,96,1000,1,0,0:0:0:0:"),
            new HitObject("128,96,1250,1,0,0:0:0:0:")
        ];
        List<TimingPoint> timingPoints =
        [new TimingPoint(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false)];

        // Act
        PatternGalleryPattern pattern = maker.FromObjects(
            hitObjects,
            timingPoints,
            "Two circles",
            1.4,
            GameMode.Standard,
            out Beatmap patternBeatmap);

        // Assert
        pattern.Name.Should().Be("Two circles");
        pattern.FileName.Should().EndWith(".osu");
        pattern.ObjectCount.Should().Be(2);
        pattern.Duration.Should().Be(TimeSpan.FromMilliseconds(250));
        patternBeatmap.HitObjects.Should().HaveCount(2);
        patternBeatmap.Metadata["Version"].Value.Should().Be("Two circles");
    }

    [TestMethod]
    public void FromSelected_WithNoSelectedObjects_ThrowsInvalidOperationException()
    {
        // Arrange
        PatternGalleryMaker maker = new();
        Beatmap beatmap = new(
            [new HitObject("64,96,1000,1,0,0:0:0:0:")],
            [],
            globalSv: 1.4);

        // Act
        Action act = () => maker.FromSelected(beatmap, "Empty selection", [], out _);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No selected hit objects found.");
    }
}
