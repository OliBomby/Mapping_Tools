using Editor_Reader;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class EditorReaderSnapshotConverterTests
{
    [TestMethod]
    public void Convert_WithLegacySliderData_PreservesSelectionAndDefaults()
    {
        // Arrange
        EditorReader reader = CreateValidReader();
        reader.hitObjects[0] = new HitObject
        {
            SpatialLength = 160,
            StartTime = 1000,
            Type = 2,
            SoundType = 2,
            SegmentCount = 2,
            X = 100,
            Y = 200,
            SampleFile = "custom.wav",
            SampleVolume = 80,
            SampleSet = 1,
            SampleSetAdditions = 2,
            CustomSampleSet = 3,
            IsSelected = true,
            CurveType = 0,
            sliderCurvePoints = [100, 200, 150, 225, 200, 250],
            SoundTypeList = [2],
            SampleSetList = [1],
            SampleSetAdditionsList = [2]
        };

        // Act
        LiveBeatmapSnapshot snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs",
            editorTime: 2222);
        Core.Classes.BeatmapHelper.HitObject converted =
            snapshot.HitObjects[0];

        // Assert
        snapshot.Path.Should().Be(Path.Combine(
                @"C:\osu!\Songs",
                "123 Artist - Title",
                "map.osu"));
        snapshot.EditorTime.Should().Be(2222);
        snapshot.SelectedHitObjects.Should().ContainSingle().Which.Should().BeSameAs(converted);
        converted.Repeat.Should().Be(2);
        converted.CurvePoints.Count.Should().Be(2);
        converted.EdgeHitsounds.Count.Should().Be(3);
        converted.EdgeHitsounds.ToArray().Should().Equal(new[] { 2, 0, 0 });
        converted.EdgeSampleSets.Count.Should().Be(3);
        converted.EdgeAdditionSets.Count.Should().Be(3);
    }

    [TestMethod]
    public void Convert_WithMismatchedReaderCounts_ThrowsInvalidDataException()
    {
        // Arrange
        EditorReader reader = CreateValidReader();
        reader.hitObjects[0].Type = 0;

        // Act
        Action act1 = () => EditorReaderSnapshotConverter.Convert(
                reader,
                @"C:\osu!\Songs");

        // Assert
        act1.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void Convert_WithMissingReaderCollections_ThrowsInvalidDataException()
    {
        // Arrange
        EditorReader reader = CreateValidReader();
        reader.hitObjects = null!;

        // Act
        Action act = () => EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs");

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void Convert_WithTimingEffectsAndBookmarks_MapsValues()
    {
        // Arrange
        EditorReader reader = CreateValidReader();
        reader.bookmarks = [250, 500];
        reader.controlPoints[0].EffectFlags = 9;

        // Act
        LiveBeatmapSnapshot snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs");

        // Assert
        snapshot.Bookmarks.ToArray().Should().Equal(new[] { 250d, 500d });
        snapshot.TimingPoints[0].Kiai.Should().BeTrue();
        snapshot.TimingPoints[0].OmitFirstBarLine.Should().BeTrue();
    }

    private static EditorReader CreateValidReader()
    {
        return new EditorReader
        {
            ContainingFolder = "123 Artist - Title",
            Filename = "map.osu",
            bookmarks = [],
            numControlPoints = 1,
            controlPoints =
            [
                new ControlPoint
                {
                    Offset = 0,
                    BeatLength = 500,
                    TimeSignature = 4,
                    SampleSet = 1,
                    CustomSamples = 0,
                    Volume = 70,
                    TimingChange = true
                }
            ],
            numObjects = 1,
            hitObjects =
            [
                new HitObject
                {
                    StartTime = 1000,
                    Type = 1,
                    X = 256,
                    Y = 192,
                    SampleFile = string.Empty
                }
            ],
            PreviewTime = 1234,
            SliderMultiplier = 1.8,
            SliderTickRate = 2
        };
    }
}
