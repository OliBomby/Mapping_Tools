using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SliderMerger;
using Mapping_Tools.Core.Tools.SliderMerger.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.SliderMerger;

[TestClass]
public sealed class SliderMergerEngineTests
{
    [TestMethod]
    public void Merge_TwoCircles_CreatesSliderAndPreservesEndpointEdges()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,1,8");
        var beatmap = CreateBeatmap(first, second);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100 });

        // Assert
        merged.Should().Be(2);
        beatmap.HitObjects.Should().ContainSingle();
        var slider = beatmap.HitObjects[0];
        slider.IsSlider.Should().BeTrue();
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.PixelLength.Should().Be(100);
        slider.EdgeHitsounds.Should().Equal(2, 8);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_TwoCircles_PreservesEachCircleSampleSetsOnItsSliderEdge()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2")
        {
            SampleSet = SampleSet.Drum,
            AdditionSet = SampleSet.Soft,
        };
        HitObject second = new("164,64,100,1,8")
        {
            SampleSet = SampleSet.Normal,
            AdditionSet = SampleSet.Drum,
        };
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100 });

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.EdgeSampleSets.Should().Equal(SampleSet.Drum, SampleSet.Normal);
        slider.EdgeAdditionSets.Should().Equal(SampleSet.Soft, SampleSet.Drum);
    }

    [TestMethod]
    public void Merge_TwoSliders_WithLinearConnectionAddsGapAndKeepsBezierType()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,1,100");
        HitObject second = new("200,64,100,2,0,L|300:64,1,100");
        var beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new()
        {
            Leniency = 50,
            MergeOnSliderEnd = false,
            ConnectionModeSetting = SliderMergerConnectionMode.Linear,
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Bezier);
        slider.PixelLength.Should().Be(236);
        slider.GetAllCurvePoints().Should().Contain(new Vector2(200, 64));
    }

    [DataTestMethod]
    [DataRow(PathType.PerfectCurve)]
    [DataRow(PathType.Catmull)]
    [DataRow(PathType.BSpline)]
    public void Merge_CurvedSliderWithCircle_ConvertsResultToBezier(PathType pathType)
    {
        // Arrange
        string pathToken = pathType switch
        {
            PathType.PerfectCurve => "P",
            PathType.Catmull => "C",
            PathType.BSpline => "B4",
            _ => throw new ArgumentOutOfRangeException(nameof(pathType)),
        };
        HitObject first = new($"64,64,0,2,0,{pathToken}|114:164|164:64,1,100");
        HitObject second = new("200,64,100,1,0");
        var beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 50, MergeOnSliderEnd = false });

        // Assert
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Bezier);
    }

    [TestMethod]
    public void Merge_SliderAndCircle_RetainsExistingEdgeSamplesAndNormalizesRepeat()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,2,100");
        first.EdgeHitsounds = [2, 4, 8];
        first.EdgeSampleSets = [SampleSet.Drum, SampleSet.Soft, SampleSet.Normal];
        first.EdgeAdditionSets = [SampleSet.Soft, SampleSet.Normal, SampleSet.Drum];
        HitObject second = new("200,64,100,1,8")
        {
            SampleSet = SampleSet.Drum,
            AdditionSet = SampleSet.Soft,
        };
        var beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 50 });

        // Assert
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.EdgeHitsounds.Should().Equal(2, 4, 8);
        slider.EdgeSampleSets.Should().Equal(SampleSet.Drum, SampleSet.Soft, SampleSet.Normal);
        slider.EdgeAdditionSets.Should().Equal(SampleSet.Soft, SampleSet.Normal, SampleSet.Drum);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_CircleAndSlider_RetainsExistingEdgeDataWhenSourceIsIncomplete()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2")
        {
            SampleSet = SampleSet.Soft,
            AdditionSet = SampleSet.Drum,
        };
        HitObject second = new("164,64,100,2,0,L|264:64,1,100");
        second.EdgeHitsounds = [4];
        second.EdgeSampleSets = [];
        second.EdgeAdditionSets = [];
        var beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100 });

        // Assert
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.EdgeHitsounds.Should().Equal(4);
        slider.EdgeSampleSets.Should().BeEmpty();
        slider.EdgeAdditionSets.Should().BeEmpty();
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_CircleSliderCircle_RetainsSurvivingSliderEdgeDataAndContinuesChain()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,2,0,L|264:64,1,100");
        HitObject third = new("264,64,200,1,8");
        var beatmap = CreateBeatmap(first, second, third);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100 });

        // Assert
        merged.Should().Be(3);
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.Pos.Should().Be(new Vector2(64, 64));
        slider.EdgeHitsounds.Should().Equal(0, 0);
    }

    [TestMethod]
    public void Merge_WithPlayableEndMatching_UsesSliderGeometry()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|264:64,1,100");
        HitObject second = new("164,64,100,1,0");
        var beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new() { Leniency = 0, MergeOnSliderEnd = true };

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
        var beatmap = CreateBeatmap(first, second);

        // Act
        Action act = () => SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = -1 });

        // Assert
        act.Should().Throw<ArgumentException>();
        beatmap.HitObjects.Should().HaveCount(2);
    }

    [TestMethod]
    public void Merge_WithObjectsOutsideLeniency_LeavesBothCirclesUntouched()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        int merged = SliderMergerEngine.Merge(beatmap, beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 99 });

        // Assert
        merged.Should().Be(0);
        beatmap.HitObjects.Should().Equal(first, second);
        first.IsCircle.Should().BeTrue();
        second.IsCircle.Should().BeTrue();
    }

    [TestMethod]
    public void Merge_TwoCirclesWithLinearOnLinear_ProducesStraightLinearSlider()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100, LinearOnLinear = true });

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Linear);
        slider.GetAllCurvePoints().Should().Equal(new Vector2(64, 64), new Vector2(164, 64));
        slider.PixelLength.Should().Be(100);
    }

    [TestMethod]
    public void Merge_TwoSlidersWithMoveConnection_TranslatesSecondPathWithoutAddingGapLength()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,1,100");
        HitObject second = new("200,64,100,2,0,L|300:64,1,100");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new()
        {
            Leniency = 50,
            MergeOnSliderEnd = false,
            ConnectionModeSetting = SliderMergerConnectionMode.Move,
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.PixelLength.Should().Be(200);
        slider.GetAllCurvePoints()[^1].Should().Be(new Vector2(264, 64));
    }

    [TestMethod]
    public void Merge_TwoLinearSlidersWithLinearOnLinear_RemainsLinearAndRemovesJoinDuplicates()
    {
        // Arrange
        HitObject first = new("64,64,0,2,0,L|164:64,1,100");
        HitObject second = new("200,64,100,2,0,L|300:64,1,100");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new()
        {
            Leniency = 50,
            MergeOnSliderEnd = false,
            ConnectionModeSetting = SliderMergerConnectionMode.Move,
            LinearOnLinear = true,
        };

        // Act
        int merged = SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        merged.Should().Be(2);
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.SliderType.Should().Be(PathType.Linear);
        slider.PixelLength.Should().Be(200);
        slider.GetAllCurvePoints().Should().OnlyHaveUniqueItems();
    }

    [TestMethod]
    public void Merge_CoincidentCircles_RemovesSecondWithoutCreatingZeroLengthSlider()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("64,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 0 });

        // Assert
        merged.Should().Be(2);
        beatmap.HitObjects.Should().ContainSingle().Which.Should().BeSameAs(first);
        first.IsCircle.Should().BeTrue();
        first.IsSlider.Should().BeFalse();
    }

    [TestMethod]
    public void Merge_UnsupportedObjectBetweenCircles_PreventsMergingAcrossIt()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject unsupported = new("64,64,100,1,0")
        {
            IsCircle = false,
            IsSlider = false,
            IsSpinner = true,
        };
        HitObject last = new("64,64,200,1,8");
        Beatmap beatmap = CreateBeatmap(first, unsupported, last);

        // Act
        int merged = SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 0 });

        // Assert
        merged.Should().Be(0);
        beatmap.HitObjects.Should().Equal(first, unsupported, last);
    }

    [TestMethod]
    public void IsLinearBezier_RequiresEveryInteriorPointToRepeatANeighbor()
    {
        // Arrange
        Vector2[] linear = [new(0, 0), new(10, 0), new(10, 0), new(20, 0)];
        Vector2[] curved = [new(0, 0), new(10, 5), new(20, 0)];

        // Act
        bool isLinear = SliderMergerEngine.IsLinearBezier(linear);
        bool isCurved = SliderMergerEngine.IsLinearBezier(curved);

        // Assert
        isLinear.Should().BeTrue();
        isCurved.Should().BeFalse();
    }

    [TestMethod]
    public void Merge_WithNonFiniteLeniency_ThrowsBeforeMutation()
    {
        // Arrange
        HitObject first = new("64,64,0,1,2");
        HitObject second = new("164,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        Action act = () => SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = double.NaN });

        // Assert
        act.Should().Throw<ArgumentException>();
        beatmap.HitObjects.Should().Equal(first, second);
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
            true,
            false,
            false);
        return new Beatmap(objects.ToList(), [redline], redline);
    }
}
