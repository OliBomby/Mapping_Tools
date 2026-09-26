using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class HitObjectTests
{
    [TestMethod]
    public void CircleLine_ParsesAndRoundTrips()
    {
        // Arrange
        // Act
        const string line = "256,192,1000,5,2,2:3:4:75:custom.wav";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsCircle.Should().BeTrue();
        hitObject.NewCombo.Should().BeTrue();
        hitObject.Whistle.Should().BeTrue();
        hitObject.SampleSet.Should().Be(SampleSet.Soft);
        hitObject.AdditionSet.Should().Be(SampleSet.Drum);
        hitObject.CustomIndex.Should().Be(4);
        hitObject.SampleVolume.Should().Be(75d);
        hitObject.Filename.Should().Be("custom.wav");
        hitObject.GetLine().Should().Be(line);
    }

    [TestMethod]
    public void SliderLine_ParsesPathEdgesAndRoundTrips()
    {
        // Arrange
        // Act
        const string line = "64,96,1200,6,2,B|128:96|192:128,2,240,2|8|0,1:2|2:3|3:1,1:2:3:60:";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsSlider.Should().BeTrue();
        hitObject.ControlPoints[0].Type.Should().Be(PathType.Bezier);
        hitObject.ControlPoints.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(64, 0), new Vector2(128, 32));
        hitObject.Repeat.Should().Be(2);
        hitObject.EdgeHitsounds.Should().Equal(2, 8, 0);
        hitObject.EdgeSampleSets.Should().Equal(SampleSet.Normal, SampleSet.Soft, SampleSet.Drum);
        hitObject.EdgeAdditionSets.Should().Equal(SampleSet.Soft, SampleSet.Drum, SampleSet.Normal);
        hitObject.GetLine().Should().Be(line);
    }

    [DataTestMethod]
    [DataRow("C", PathType.Catmull)]
    [DataRow("B", PathType.Bezier)]
    [DataRow("L", PathType.Linear)]
    [DataRow("P", PathType.PerfectCurve)]
    [DataRow("B4", PathType.BSpline)]
    public void SliderLine_WithEachPathToken_PreservesItsPathTypeAndToken(string token, PathType expectedType)
    {
        // Arrange
        string line = $"64,96,1200,2,0,{token}|128:96|192:96,1,128,0|0,0:0|0:0,0:0:0:0:";

        // Act
        HitObject hitObject = new(line);

        // Assert
        hitObject.ControlPoints[0].Type.Should().Be(expectedType);
        hitObject.GetLine().Split(',')[5].Should().Be($"{token}|128:96|192:96");
    }

    [TestMethod]
    public void SliderLine_WithMultiplePathMarkers_PreservesTypedRelativeControlPoints()
    {
        // Arrange
        const string line = "64,96,1200,2,0,B|128:96|L|192:128|256:96,1,240,0|0,0:0|0:0,0:0:0:0:";

        // Act
        HitObject hitObject = new(line);

        // Assert
        hitObject.ControlPoints.Select(point => point.Type).Should().Equal(
            PathType.Bezier, null, PathType.Linear, null);
        hitObject.ControlPoints.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(64, 0), new Vector2(128, 32), new Vector2(192, 0));
        hitObject.GetLine().Split(',')[5].Should().Be("B|128:96|L|192:128|256:96");
    }

    [TestMethod]
    public void SliderLine_WithLegacyDuplicateBoundary_NormalizesToOneTypedAnchor()
    {
        // Arrange
        const string line = "0,0,1000,2,0,B|100:0|100:0|200:0,1,200";

        // Act
        HitObject hitObject = new(line, 14);

        // Assert
        hitObject.ControlPoints.Select(point => (point.Position, point.Type)).Should().Equal(
            (Vector2.Zero, PathType.Bezier),
            (new Vector2(100, 0), PathType.Bezier),
            (new Vector2(200, 0), null));
        hitObject.GetSliderPath().SegmentStarts.Should().HaveCount(2);
        hitObject.GetLine(14).Split(',')[5].Should().Be("B|100:0|100:0|200:0");
        hitObject.GetLine(128).Split(',')[5].Should().Be("B|B|100:0|200:0");
    }

    [TestMethod]
    public void SliderLine_WithModernDuplicatePositions_DoesNotInferSegmentBoundary()
    {
        // Arrange
        const string line = "0,0,1000,2,0,B|100:0|100:0|200:0,1,200";

        // Act
        HitObject hitObject = new(line, 128);

        // Assert
        hitObject.ControlPoints.Should().HaveCount(4);
        hitObject.ControlPoints.Skip(1).Should().OnlyContain(point => point.Type == null);
        hitObject.GetSliderPath().SegmentStarts.Should().ContainSingle();
        hitObject.GetLine(128).Split(',')[5].Should().Be("B|100:0|100:0|200:0");
    }

    [TestMethod]
    public void SliderLine_WithThreeLegacyBoundaryPositions_RoundTripsThroughTypedSegments()
    {
        // Arrange
        const string line = "0,0,1000,2,0,B|100:0|100:0|100:0|200:0,1,200";

        // Act
        HitObject hitObject = new(line, 14);

        // Assert
        hitObject.ControlPoints.Select(point => point.Type).Should().Equal(
            PathType.Bezier, PathType.Bezier, PathType.Bezier, null);
        hitObject.GetSliderPath().SegmentStarts.Should().HaveCount(3);
        hitObject.GetLine(14).Split(',')[5].Should().Be("B|100:0|100:0|100:0|200:0");
    }

    [TestMethod]
    public void SliderLine_WithLegacyDuplicateAfterTypeMarker_UsesActiveSegmentType()
    {
        // Arrange
        const string line = "0,0,1000,2,0,B|50:0|L|100:0|150:0|150:0|200:0,1,200";

        // Act
        HitObject hitObject = new(line, 14);

        // Assert
        hitObject.ControlPoints.Select(point => point.Type).Should().Equal(
            PathType.Bezier, null, PathType.Linear, PathType.Linear, null);
    }

    [TestMethod]
    public void SliderLine_WithLegacyBoundaryAtStart_RoundTripsWithoutAddingAnAnchor()
    {
        // Arrange
        const string line = "0,0,1000,2,0,B|0:0|100:0,1,100";

        // Act
        HitObject hitObject = new(line, 14);

        // Assert
        hitObject.ControlPoints.Should().HaveCount(3);
        hitObject.ControlPoints[1].Type.Should().Be(PathType.Bezier);
        hitObject.GetLine(14).Split(',')[5].Should().Be("B|0:0|100:0");
    }

    [TestMethod]
    public void SliderLine_WithUnknownPathMarkers_UsesCatmullForEachMarker()
    {
        // Arrange
        const string line = "0,0,0,2,0,X|100:0|L|200:0|Y|250:0,1,250";

        // Act
        HitObject hitObject = new(line);

        // Assert
        hitObject.ControlPoints.Select(point => point.Type).Should().Equal(
            PathType.Catmull, null, PathType.Linear, PathType.Catmull);
        hitObject.GetLine(128).Split(',')[5].Should().Be("C|100:0|L|200:0|C|250:0");
    }

    [TestMethod]
    public void GetLine_WithMixedPathAndLegacyVersion_ConvertsOnlySerializedOutputToBezier()
    {
        // Arrange
        HitObject hitObject = new("64,96,1200,2,0,P|128:160|192:96|L|256:96,1,240");
        var originalPoints = hitObject.ControlPoints.Select(point => (point.Position, point.Type)).ToArray();

        // Act
        string legacyLine = hitObject.GetLine(14);
        string lazerLine = hitObject.GetLine(128);

        // Assert
        legacyLine.Split(',')[5].Should().StartWith("B|");
        legacyLine.Split(',')[5].Should().NotContain("|P|").And.NotContain("|L|");
        lazerLine.Split(',')[5].Should().Be("P|128:160|192:96|L|256:96");
        hitObject.ControlPoints.Select(point => (point.Position, point.Type)).Should().Equal(originalPoints);
    }

    [TestMethod]
    public void GetLine_WithDuplicatedTypedBoundary_WritesOneBezierSegmentBoundary()
    {
        // Arrange
        HitObject hitObject = new("0,0,1000,2,0,L|100:0|B|100:0|150:100|200:0,1,300");

        // Act
        string legacyPath = hitObject.GetLine(14).Split(',')[5];

        // Assert
        legacyPath.Should().Be("B|100:0|100:0|150:100|200:0");
        hitObject.GetLine(128).Split(',')[5].Should().Be("L|B|100:0|150:100|200:0");
    }

    [TestMethod]
    public void GetLine_WithBSplineAndLegacyVersion_WritesBezierWithoutChangingStoredType()
    {
        // Arrange
        HitObject hitObject = new("64,96,1200,2,0,B4|128:96|192:128|256:96,1,240");

        // Act
        string legacyLine = hitObject.GetLine(14);

        // Assert
        legacyLine.Split(',')[5].Should().StartWith("B|");
        legacyLine.Split(',')[5].Should().NotStartWith("B4|");
        hitObject.ControlPoints[0].Type.Should().Be(PathType.BSpline);
        hitObject.GetLine(128).Split(',')[5].Should().Be("B4|128:96|192:128|256:96");
    }

    [TestMethod]
    public void GetSliderPath_WithMixedPathTypes_EvaluatesBothSegments()
    {
        // Arrange
        HitObject hitObject = new("64,96,1200,2,0,L|164:96|B|214:146|264:96,1,250");

        // Act
        SliderPath path = hitObject.GetSliderPath(fullLength: true);

        // Assert
        path.PathControlPoints.Select(point => point.Type).Should().Equal(
            PathType.Linear, null, PathType.Bezier, null);
        path.PositionAt(0).Should().Be(new Vector2(64, 96));
        path.PositionAt(1).Should().Be(new Vector2(264, 96));
        path.CalculatedPath.Should().Contain(point => point.Y > 96);
    }

    [TestMethod]
    public void GetSliderPath_WithTypedDuplicateAnchor_UsesTheNewSegmentType()
    {
        // Arrange
        HitObject hitObject = new("0,0,1000,2,0,L|100:0|B|100:0|150:100|200:0,1,300");

        // Act
        SliderPath path = hitObject.GetSliderPath(fullLength: true);

        // Assert
        path.CalculatedPath.Should().NotContain(new Vector2(150, 100));
        path.CalculatedPath.Should().Contain(point => point.Y > 0);
        path.PositionAt(1).Should().Be(new Vector2(200, 0));
    }

    [TestMethod]
    public void Move_WithSliderControlPoints_KeepsTheirPositionsRelativeToTheStart()
    {
        // Arrange
        HitObject hitObject = new("64,96,1200,2,0,P|128:160|192:96,1,200");
        var originalPositions = hitObject.ControlPoints.Select(point => point.Position).ToArray();

        // Act
        hitObject.Move(new Vector2(20, -10));

        // Assert
        hitObject.Pos.Should().Be(new Vector2(84, 86));
        hitObject.ControlPoints.Select(point => point.Position).Should().Equal(originalPositions);
        hitObject.GetAbsoluteControlPointPositions().Should().Equal(
            new Vector2(84, 86), new Vector2(148, 150), new Vector2(212, 86));
    }

    [TestMethod]
    public void SetSliderPath_WithExistingMultiplePathMarkers_ReplacesGeometryAndClearsStaleMarkers()
    {
        // Arrange
        HitObject hitObject = new(
            "64,96,1200,2,0,B|128:96|L|192:128|256:96,1,240,0|0,0:0|0:0,0:0:0:0:");
        SliderPath replacement = new(PathType.BSpline,
            [new Vector2(10, 10), new Vector2(30, 10), new Vector2(30, 40)], 50);

        // Act
        hitObject.SetSliderPath(replacement);

        // Assert
        hitObject.ControlPoints[0].Type.Should().Be(PathType.BSpline);
        hitObject.ControlPoints.Select(point => point.Position).Should().Equal(
            Vector2.Zero, new Vector2(20, 0), new Vector2(20, 30));
        hitObject.PixelLength.Should().Be(50);
        hitObject.ControlPoints.Skip(1).Should().OnlyContain(point => point.Type == null);
        hitObject.GetLine().Split(',')[5].Should().Be("B4|30:10|30:40");
    }

    [TestMethod]
    public void ControlPoints_WithReplacementControlPoints_ClearsTypedSegmentMarkers()
    {
        // Arrange
        HitObject hitObject = new(
            "64,96,1200,2,0,B|128:96|L|192:128|256:96,1,240,0|0,0:0|0:0,0:0:0:0:");

        // Act
        hitObject.Pos = new Vector2(10, 10);
        hitObject.ControlPoints =
        [
            new PathControlPoint(Vector2.Zero, PathType.Bezier),
            new PathControlPoint(new Vector2(20, 0)),
            new PathControlPoint(new Vector2(20, 30)),
        ];

        // Assert
        hitObject.ControlPoints.Skip(1).Should().OnlyContain(point => point.Type == null);
        hitObject.GetLine().Split(',')[5].Should().Be("B|30:10|30:40");
    }

    [TestMethod]
    public void GetSliderPath_WithPixelLengthAndFullLength_PreservesTypeAndSeparatesRequestedFromGeometricLength()
    {
        // Arrange
        HitObject hitObject = new(
            "64,96,1200,2,0,B4|128:96|192:96,1,50,0|0,0:0|0:0,0:0:0:0:");

        // Act
        SliderPath requestedLength = hitObject.GetSliderPath();
        SliderPath fullLength = hitObject.GetSliderPath(fullLength: true);

        // Assert
        requestedLength.Type.Should().Be(PathType.BSpline);
        requestedLength.ExpectedDistance.Should().Be(50);
        requestedLength.Distance.Should().Be(50);
        fullLength.Type.Should().Be(PathType.BSpline);
        fullLength.ExpectedDistance.Should().BeNull();
        fullLength.Distance.Should().BeGreaterThan(50);
        requestedLength.ControlPoints.Should().Equal(fullLength.ControlPoints);
    }

    [TestMethod]
    public void DeepCopy_WithMultiplePathMarkers_CopiesTypedControlPoints()
    {
        // Arrange
        HitObject original = new(
            "64,96,1200,2,0,B|128:96|L|192:128|256:96,1,240,0|0,0:0|0:0,0:0:0:0:");

        // Act
        HitObject copy = original.DeepCopy();
        copy.ControlPoints[0].Type = PathType.Catmull;

        // Assert
        copy.ControlPoints.Select(point => point.Type).Should().Equal(PathType.Catmull, null, PathType.Linear, null);
        original.ControlPoints.Select(point => point.Type).Should().Equal(PathType.Bezier, null, PathType.Linear, null);
        copy.ControlPoints.Should().NotBeSameAs(original.ControlPoints);
        copy.ControlPoints[0].Should().NotBeSameAs(original.ControlPoints[0]);
    }

    [TestMethod]
    public void HoldNoteLine_UsesEndTimeFromObjectParams()
    {
        // Arrange
        // Act
        const string line = "128,192,2000,128,0,2500:1:2:3:40:hold.wav";
        var hitObject = new HitObject(line);

        // Assert
        hitObject.IsHoldNote.Should().BeTrue();
        hitObject.EndTime.Should().Be(2500d);
        hitObject.GetLine().Should().Be(line);
    }

    [TestMethod]
    public void Comparer_CanIgnorePositionAndTime()
    {
        // Arrange
        // Act
        var first = new HitObject("64,96,1000,1,0,0:0:0:0:");
        var second = new HitObject("128,192,2000,1,0,0:0:0:0:");

        // Assert
        new HitObjectComparer(false, false).Equals(first, second).Should().BeTrue();
        new HitObjectComparer().Equals(first, second).Should().BeFalse();
    }

    [TestMethod]
    public void GetSliderTickTimes_ReversesTicksOnEveryOtherSpan()
    {
        // Arrange
        HitObject slider = CreateSlider();

        // Act
        List<double> tickTimes = slider.GetSliderTickTimes(4);

        // Assert
        tickTimes.Should().Equal(1125, 1250, 1375, 1625, 1750, 1875);
    }

    [TestMethod]
    public void GetSliderTickTimes_ExcludesTicksWithinTenMillisecondsOfSpanEnd()
    {
        // Arrange
        HitObject slider = CreateSlider();
        slider.TemporalLength = 260;

        // Act
        List<double> tickTimes = slider.GetSliderTickTimes(2);

        // Assert
        tickTimes.Should().BeEmpty();
    }

    [TestMethod]
    public void GetPlayingBodyFilenames_UsesBodySampleSetAndOmitsDefaultSamplesWhenRequested()
    {
        // Arrange
        HitObject slider = CreateSlider();
        slider.Whistle = true;
        slider.TimingPoint = CreateTimingPoint(0, -100, SampleSet.Soft, 0, false);
        slider.BodyHitsounds.Add(CreateTimingPoint(1100, -100, SampleSet.Drum, 3, false));

        // Act
        List<string> filenames = slider.GetPlayingBodyFilenames(2, includeDefaults: false);

        // Assert
        filenames.Should().Equal(
            "drum-sliderslide3.wav",
            "drum-sliderwhistle3.wav",
            "drum-slidertick3.wav",
            "drum-slidertick3.wav");
    }

    [TestMethod]
    public void MoveEndTime_OnRepeatedSlider_UpdatesSpanLengthPixelLengthAndEndTime()
    {
        // Arrange
        HitObject slider = CreateSlider();
        slider.PixelLength = 140;
        double originalEndTime = slider.EndTime;
        double originalPixelLength = slider.PixelLength;
        Timing timing = new([CreateTimingPoint(0, 500, SampleSet.Normal, 0, true)], 1.4);

        // Act
        slider.MoveEndTime(timing, 200);

        // Assert
        slider.EndTime.Should().BeApproximately(originalEndTime + 200, 0.000001);
        slider.PixelLength.Should().BeGreaterThan(originalPixelLength);
        slider.EndTime.Should().BeApproximately(slider.Time + slider.TemporalLength * slider.Repeat, 0.000001);
    }

    [TestMethod]
    public void GetAllTloTimes_ForHoldNote_ReturnsStartAndEndEdges()
    {
        // Arrange
        HitObject hold = new("128,192,2000,128,0,2500:1:2:3:40:hold.wav");
        Timing timing = new(1.4);

        // Act
        List<double> times = hold.GetAllTloTimes(timing);

        // Assert
        times.Should().Equal(2000, 2500);
    }

    private static HitObject CreateSlider()
    {
        return new HitObject("256,192,1000,2,0,L|456:192,2,140,0|0|0,0:0|0:0|0:0,0:0:0:0:")
        {
            TemporalLength = 500,
            SliderVelocity = -100,
            TimingPoint = CreateTimingPoint(0, -100, SampleSet.Normal, 0, false),
            UnInheritedTimingPoint = CreateTimingPoint(0, 500, SampleSet.Normal, 0, true),
        };
    }

    private static TimingPoint CreateTimingPoint(
        double offset,
        double millisecondsPerBeat,
        SampleSet sampleSet,
        int sampleIndex,
        bool uninherited)
    {
        return new TimingPoint(
            offset,
            millisecondsPerBeat,
            4,
            sampleSet,
            sampleIndex,
            100,
            uninherited,
            false,
            false);
    }
}
