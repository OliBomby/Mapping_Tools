using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
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
            new("64,96,1000,1,0,0:0:0:0:"),
            new("128,96,1250,1,0,0:0:0:0:"),
        ];
        List<TimingPoint> timingPoints =
            [new(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false)];

        // Act
        var pattern = maker.FromObjects(
            hitObjects,
            timingPoints,
            "Two circles",
            1.4,
            GameMode.Standard,
            out var patternBeatmap);

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

    [TestMethod]
    public void FromSelected_WithOneObject_KeepsRequiredTimingAndRemovesStoryboardFromCopy()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false);
        HitObject first = new("64,96,1000,1,0,0:0:0:0:");
        HitObject selected = new("128,96,2000,1,0,0:0:0:0:");
        HitObject last = new("192,96,3000,1,0,0:0:0:0:");
        Beatmap source = new([first, selected, last], [redline], redline);
        source.StoryboardLayerForeground.Add(new Sprite { FilePath = "image.png" });
        PatternGalleryMaker maker = new();

        // Act
        var pattern = maker.FromSelected(source, "Selected", [selected], out Beatmap extracted);

        // Assert
        pattern.ObjectCount.Should().Be(1);
        extracted.HitObjects.Select(item => item.Time).Should().Equal(2000);
        extracted.BeatmapTiming.Redlines.Should().ContainSingle().Which.Offset.Should().Be(0);
        extracted.StoryboardLayerForeground.Should().BeEmpty();
        source.HitObjects.Should().HaveCount(3);
        source.StoryboardLayerForeground.Should().ContainSingle();
    }

    [TestMethod]
    public void FromBeatmapFiltered_WithInclusiveBounds_ExtractsOnlyObjectsInRange()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 70, true, false, false);
        Beatmap source = new(
            [
                new HitObject("64,96,500,1,0,0:0:0:0:"),
                new HitObject("64,96,1000,1,0,0:0:0:0:"),
                new HitObject("64,96,2000,1,0,0:0:0:0:"),
                new HitObject("64,96,2500,1,0,0:0:0:0:"),
            ],
            [redline],
            redline);
        PatternGalleryMaker maker = new();

        // Act
        var pattern = maker.FromBeatmapFiltered(source, "Window", null, 1000, 2000, out Beatmap extracted);

        // Assert
        pattern.ObjectCount.Should().Be(2);
        pattern.Duration.Should().Be(TimeSpan.FromMilliseconds(1000));
        extracted.HitObjects.Select(item => item.Time).Should().Equal(1000, 2000);
        source.HitObjects.Should().HaveCount(4);
    }
}
