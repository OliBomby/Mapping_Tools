using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,8");
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
        slider.ControlPoints[0].Type.Should().Be(PathType.Bezier);
        slider.PixelLength.Should().Be(100);
        slider.EdgeHitsounds.Should().Equal(2, 8);
        slider.Repeat.Should().Be(1);
    }

    [TestMethod]
    public void Merge_TwoCircles_PreservesEachCircleSampleSetsOnItsSliderEdge()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        first.SampleSet = SampleSet.Drum;
        first.AdditionSet = SampleSet.Soft;
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,8");
        second.SampleSet = SampleSet.Normal;
        second.AdditionSet = SampleSet.Drum;
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
    public void Merge_TwoSliders_WithLinearConnectionAddsGapAndKeepsTypedSegments()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,2,0,L|164:64,1,100");
        HitObject second = BeatmapTestData.DecodeHitObject("200,64,100,2,0,L|300:64,1,100");
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
        slider.ControlPoints[0].Type.Should().Be(PathType.Linear);
        slider.ControlPoints.Skip(1).Should().Contain(point => point.Type == PathType.Linear);
        slider.PixelLength.Should().Be(236);
        slider.GetAbsoluteControlPointPositions().Should().Contain(new Vector2(200, 64));
    }

    [DataTestMethod]
    [DataRow(PathType.PerfectCurve)]
    [DataRow(PathType.Catmull)]
    [DataRow(PathType.BSpline)]
    public void Merge_CurvedSliderWithCircle_PreservesOriginalTypeAndAddsLinearSegment(PathType pathType)
    {
        // Arrange
        string pathToken = pathType switch
        {
            PathType.PerfectCurve => "P",
            PathType.Catmull => "C",
            PathType.BSpline => "B4",
            _ => throw new ArgumentOutOfRangeException(nameof(pathType)),
        };
        HitObject first = BeatmapTestData.DecodeHitObject($"64,64,0,2,0,{pathToken}|114:164|164:64,1,100");
        HitObject second = BeatmapTestData.DecodeHitObject("200,64,100,1,0");
        var beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(
            beatmap,
            beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 50, MergeOnSliderEnd = false });

        // Assert
        var slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.ControlPoints[0].Type.Should().Be(pathType);
        slider.ControlPoints[^2].Type.Should().Be(PathType.Linear);
    }

    [TestMethod]
    public void Merge_TruncatedCatmullWithSlider_ConnectsAtVisibleEndAfterCuttingSegment()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("0,0,0,2,0,C|50:100|100:0,1,80");
        HitObject second = BeatmapTestData.DecodeHitObject("120,0,100,2,0,L|170:0,1,50");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new()
        {
            Leniency = 200,
            MergeOnSliderEnd = true,
            ConnectionModeSetting = SliderMergerConnectionMode.Linear,
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.ControlPoints[0].Type.Should().Be(PathType.Bezier);
        slider.GetSliderPath(fullLength: true).Distance.Should().BeApproximately(slider.PixelLength, 1);
        Vector2.Distance(slider.GetSliderPath().PositionAt(1), new Vector2(170, 0)).Should().BeLessThan(1);
    }

    [TestMethod]
    public void Merge_TruncatedMixedSlider_KeepsCompletedPerfectSegment()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("0,0,0,2,0,L|100:0|P|100:0|150:50|200:0|L|200:0|300:0,1,300");
        first.PixelLength = first.GetSliderPath(true).Distance - 50;
        HitObject second = BeatmapTestData.DecodeHitObject("330,0,100,2,0,L|380:0,1,50");
        Beatmap beatmap = CreateBeatmap(first, second);
        SliderMergerEngineOptions options = new()
        {
            Leniency = 100,
            MergeOnSliderEnd = true,
            ConnectionModeSetting = SliderMergerConnectionMode.Linear,
        };

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects, options);

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.ControlPoints.Should().Contain(point => point.Type == PathType.PerfectCurve);
        slider.GetSliderPath(true).Distance.Should().BeApproximately(slider.PixelLength, 0.01);
        Vector2.Distance(slider.GetSliderPath().PositionAt(1), new Vector2(380, 0)).Should().BeLessThan(1);
    }

    [TestMethod]
    public void Merge_SliderAndCircle_RetainsExistingEdgeSamplesAndNormalizesRepeat()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,2,0,L|164:64,2,100");
        first.EdgeHitsounds = [2, 4, 8];
        first.EdgeSampleSets = [SampleSet.Drum, SampleSet.Soft, SampleSet.Normal];
        first.EdgeAdditionSets = [SampleSet.Soft, SampleSet.Normal, SampleSet.Drum];
        HitObject second = BeatmapTestData.DecodeHitObject("200,64,100,1,8");
        second.SampleSet = SampleSet.Drum;
        second.AdditionSet = SampleSet.Soft;
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        first.SampleSet = SampleSet.Soft;
        first.AdditionSet = SampleSet.Drum;
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,2,0,L|264:64,1,100");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,2,0,L|264:64,1,100");
        HitObject third = BeatmapTestData.DecodeHitObject("264,64,200,1,8");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,2,0,L|264:64,1,100");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,0");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,0");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,0");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,8");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,8");
        Beatmap beatmap = CreateBeatmap(first, second);

        // Act
        SliderMergerEngine.Merge(beatmap, beatmap.HitObjects,
            new SliderMergerEngineOptions { Leniency = 100, LinearOnLinear = true });

        // Assert
        HitObject slider = beatmap.HitObjects.Should().ContainSingle().Subject;
        slider.ControlPoints[0].Type.Should().Be(PathType.Linear);
        slider.GetAbsoluteControlPointPositions().Should().Equal(new Vector2(64, 64), new Vector2(164, 64));
        slider.PixelLength.Should().Be(100);
    }

    [TestMethod]
    public void Merge_TwoSlidersWithMoveConnection_TranslatesSecondPathWithoutAddingGapLength()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,2,0,L|164:64,1,100");
        HitObject second = BeatmapTestData.DecodeHitObject("200,64,100,2,0,L|300:64,1,100");
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
        slider.GetAbsoluteControlPointPositions()[^1].Should().Be(new Vector2(264, 64));
    }

    [TestMethod]
    public void Merge_TwoLinearSlidersWithLinearOnLinear_RemainsLinearAndRemovesJoinDuplicates()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,2,0,L|164:64,1,100");
        HitObject second = BeatmapTestData.DecodeHitObject("200,64,100,2,0,L|300:64,1,100");
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
        slider.ControlPoints[0].Type.Should().Be(PathType.Linear);
        slider.PixelLength.Should().Be(200);
        slider.GetAbsoluteControlPointPositions().Should().OnlyHaveUniqueItems();
    }

    [TestMethod]
    public void Merge_CoincidentCircles_RemovesSecondWithoutCreatingZeroLengthSlider()
    {
        // Arrange
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("64,64,100,1,8");
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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject unsupported = BeatmapTestData.DecodeHitObject("64,64,100,1,0");
        unsupported.IsCircle = false;
        unsupported.IsSlider = false;
        unsupported.IsSpinner = true;
        HitObject last = BeatmapTestData.DecodeHitObject("64,64,200,1,8");
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
    public void IsLinearBezier_RequiresEverySegmentToBeAnEdge()
    {
        // Arrange
        PathControlPoint[] linear =
        [
            new(new Vector2(0, 0), PathType.Bezier),
            new(new Vector2(10, 0), PathType.Bezier),
            new(new Vector2(20, 0)),
        ];
        PathControlPoint[] curved =
        [
            new(new Vector2(0, 0), PathType.Bezier),
            new(new Vector2(10, 5)),
            new(new Vector2(20, 0)),
        ];

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
        HitObject first = BeatmapTestData.DecodeHitObject("64,64,0,1,2");
        HitObject second = BeatmapTestData.DecodeHitObject("164,64,100,1,8");
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
