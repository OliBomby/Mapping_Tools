using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.SliderMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class SliderMergerEngineTests
{
    [TestMethod]
    public void Merge_TwoCircles_CreatesSliderAndPreservesEndpointEdges()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerOptions { Leniency = 100 });

        // Assert
        merged.Should().Be(2);
        beatmap.HitObjects.Should().ContainSingle();
        HitObject slider = beatmap.HitObjects[0];
        slider.IsSlider.Should().BeTrue();
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.PixelLength.Should().Be(100);
        slider.EdgeHitsounds.Should().Equal(2, 8);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_TwoSliders_WithLinearConnectionAddsGapAndKeepsBezierType()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,1,100");
        HitObject second = new("200,64,100,2,0,L|300:64,1,100");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerOptions options = new()
        {
            Leniency = 50,
            MergeOnSliderEnd = false,
            ConnectionModeSetting = SliderMergerConnectionMode.Linear
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.PixelLength.Should().Be(236);
        slider.GetAllCurvePoints().Should().Contain(new Vector2(200, 64));
    }

    [TestMethod]
    public void Merge_TwoSliders_WithBezierConnectionUsesRawBezierBridge()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,1,100");
        HitObject second = new("200,100,100,2,0,L|300:100,1,100");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerOptions options = new()
        {
            Leniency = 100,
            MergeOnSliderEnd = false,
            ConnectionModeSetting = SliderMergerConnectionMode.Bezier,
            LinearOnLinear = true
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.GetAllCurvePoints().Should().Contain(new Vector2(200, 100));
        slider.EdgeHitsounds.Should().Equal(0, 0);
    }

    [TestMethod]
    public void Merge_SliderAndCircle_PreservesOuterEdgeSamplesAndNormalizesRepeat()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,2,100");
        first.EdgeHitsounds = [2, 4, 8];
        first.EdgeSampleSets = [SampleSet.Drum, SampleSet.Soft, SampleSet.Normal];
        first.EdgeAdditionSets = [SampleSet.Soft, SampleSet.Normal, SampleSet.Drum];
        HitObject second = new("200,64,100,1,8")
        {
            SampleSet = SampleSet.Drum,
            AdditionSet = SampleSet.Soft
        };
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerOptions { Leniency = 50 });

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.EdgeHitsounds.Should().Equal(2, 8);
        slider.EdgeSampleSets.Should().Equal(SampleSet.Drum, SampleSet.Drum);
        slider.EdgeAdditionSets.Should().Equal(SampleSet.Soft, SampleSet.Soft);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_CircleAndSlider_PreservesOuterEdgeHitsoundsWhenEdgeSamplesAreIncomplete()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2")
        {
            SampleSet = SampleSet.Soft,
            AdditionSet = SampleSet.Drum
        };
        HitObject second = new("164,64,100,2,0,L|264:64,1,100");
        second.EdgeHitsounds = [4];
        second.EdgeSampleSets = [];
        second.EdgeAdditionSets = [];
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerOptions { Leniency = 100 });

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.EdgeHitsounds.Should().Equal(2, 4);
        slider.EdgeSampleSets.Should().Equal(SampleSet.Soft, SampleSet.None);
        slider.EdgeAdditionSets.Should().Equal(SampleSet.Drum, SampleSet.None);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_CircleSliderCircle_RemovesOnlyConsumedSourceObjectsAndContinuesChain()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,2,0,L|264:64,1,100");
        HitObject third = new("264,64,200,1,8");
        Beatmap beatmap = CreateBeatmap(first, second, third);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerOptions { Leniency = 100 });

        // Assert
        merged.Should().Be(3);
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.Pos.Should().Be(new Vector2(64, 64));
        slider.EdgeHitsounds.Should().Equal(2, 8);
    }

    [TestMethod]
    public void Merge_WithPlayableEndMatchingUsesSliderGeometry()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|264:64,1,100");
        HitObject second = new("164,64,100,1,0");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerOptions options = new() { Leniency = 0, MergeOnSliderEnd = true };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        beatmap.HitObjects.Should().ContainSingle();
    }

    [TestMethod]
    public void Merge_WithNegativeLeniency_ThrowsBeforeMutation()
    {
        // Arrange
        HitObject first = new("64,64,0,1,0");
        HitObject second = new("164,64,100,1,0");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        Action act = () => SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerOptions { Leniency = -1 });

        // Assert
        act.Should().Throw<ArgumentException>();
        beatmap.HitObjects.Should().HaveCount(2);
    }

    private static Beatmap CreateBeatmap(params HitObject[] objects)
    {
        TimingPoint redline = new(
            0,
            500,
            4,
            SampleSet.Normal,
            0,
            100,
            uninherited: true,
            kiai: false,
            omitFirstBarLine: false);
        return new Beatmap(objects.ToList(), [redline], redline);
    }
}
