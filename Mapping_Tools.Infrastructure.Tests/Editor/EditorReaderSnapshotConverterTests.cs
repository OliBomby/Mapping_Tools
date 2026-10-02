using Editor_Reader;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
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
        var reader = CreateValidReader();
        reader.ApproachRate = 9;
        reader.CircleSize = 4;
        reader.hitObjects[0] = new HitObject
        {
            SpatialLength = 160,
            StartTime = 1000,
            EndTime = 2000,
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
            sliderCurvePoints = [100, 200, 150, 225, 150, 225, 200, 250],
            SoundTypeList = [2],
            SampleSetList = [1],
            SampleSetAdditionsList = [2],
        };

        // Act
        var snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs",
            2222);
        var converted =
            snapshot.HitObjects[0];

        // Assert
        snapshot.Path.Should().Be(Path.Combine(
            @"C:\osu!\Songs",
            "123 Artist - Title",
            "map.osu"));
        snapshot.EditorTime.Should().Be(2222);
        snapshot.ApproachRate.Should().Be(9);
        snapshot.CircleSize.Should().Be(4);
        snapshot.SelectedHitObjects.Should().ContainSingle().Which.Should().BeSameAs(converted);
        converted.Repeat.Should().Be(2);
        converted.TemporalLength.Should().Be(500);
        converted.EndTime.Should().Be(2000);
        converted.ControlPoints.Count.Should().Be(3);
        converted.ControlPoints.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(50, 25), new Vector2(100, 50));
        converted.ControlPoints[0].Type.Should().Be(PathType.Catmull);
        converted.ControlPoints[1].Type.Should().Be(PathType.Catmull);
        converted.EdgeHitsounds.Count.Should().Be(3);
        converted.EdgeHitsounds.ToArray().Should().Equal(2, 0, 0);
        converted.EdgeSampleSets.Count.Should().Be(3);
        converted.EdgeAdditionSets.Count.Should().Be(3);
    }

    [TestMethod]
    public void Convert_WithMismatchedReaderCounts_ThrowsInvalidDataException()
    {
        // Arrange
        var reader = CreateValidReader();
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
        var reader = CreateValidReader();
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
        var reader = CreateValidReader();
        reader.bookmarks = [250, 500];
        reader.controlPoints[0].EffectFlags = 9;

        // Act
        var snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs");

        // Assert
        snapshot.Bookmarks.ToArray().Should().Equal(250d, 500d);
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
                    TimingChange = true,
                },
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
                    SampleFile = string.Empty,
                },
            ],
            PreviewTime = 1234,
            SliderMultiplier = 1.8,
            SliderTickRate = 2,
        };
    }
}
