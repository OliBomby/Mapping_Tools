using Editor_Reader;
using Mapping_Tools.ApplicationServices.BeatmapEditing;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class EditorReaderSnapshotConverterTests
{
    [TestMethod]
    public void ConvertPreservesSelectionAndLegacySliderEdgeDefaults()
    {
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

        LiveBeatmapSnapshot snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs");
        Mapping_Tools.Classes.BeatmapHelper.HitObject converted =
            snapshot.HitObjects[0];

        Assert.AreEqual(
            Path.Combine(
                @"C:\osu!\Songs",
                "123 Artist - Title",
                "map.osu"),
            snapshot.Path);
        Assert.IsTrue(converted.IsSelected);
        Assert.AreEqual(2, converted.Repeat);
        Assert.AreEqual(2, converted.CurvePoints.Count);
        Assert.AreEqual(3, converted.EdgeHitsounds.Count);
        CollectionAssert.AreEqual(
            new[] { 2, 0, 0 },
            converted.EdgeHitsounds.ToArray());
        Assert.AreEqual(3, converted.EdgeSampleSets.Count);
        Assert.AreEqual(3, converted.EdgeAdditionSets.Count);
    }

    [TestMethod]
    public void ConvertRejectsReaderCountsThatNoLongerMatchAfterRepair()
    {
        EditorReader reader = CreateValidReader();
        reader.hitObjects[0].Type = 0;

        Assert.ThrowsException<InvalidDataException>(
            () => EditorReaderSnapshotConverter.Convert(
                reader,
                @"C:\osu!\Songs"));
    }

    [TestMethod]
    public void ConvertMapsTimingEffectsAndBookmarks()
    {
        EditorReader reader = CreateValidReader();
        reader.bookmarks = [250, 500];
        reader.controlPoints[0].EffectFlags = 9;

        LiveBeatmapSnapshot snapshot = EditorReaderSnapshotConverter.Convert(
            reader,
            @"C:\osu!\Songs");

        CollectionAssert.AreEqual(
            new[] { 250d, 500d },
            snapshot.Bookmarks.ToArray());
        Assert.IsTrue(snapshot.TimingPoints[0].Kiai);
        Assert.IsTrue(snapshot.TimingPoints[0].OmitFirstBarLine);
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
