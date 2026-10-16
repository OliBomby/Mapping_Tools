using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper.Serialization;

[TestClass]
public sealed class BeatmapEncoderTests
{
    [DataTestMethod]
    [DataRow("P", PathType.PerfectCurve)]
    [DataRow("C", PathType.Catmull)]
    public void Encode_RepeatedCurvedSegmentsInV14_ConvertsToBezierAndPreservesShape(string token, PathType type)
    {
        // Arrange
        HitObject slider = BeatmapTestData.DecodeHitObject(
            $"64,128,1000,2,0,{token}|114:78|{token}|164:128|214:178|264:128,1,300", 128);
        Beatmap beatmap = new() { Version = 14, HitObjects = [slider] };
        var originalPath = slider.GetSliderPath();
        var originalPoints = slider.ControlPoints.Select(point => point.Copy()).ToList();

        // Act
        string encoded = new BeatmapEncoder().Encode(beatmap);
        Beatmap decoded = new BeatmapDecoder().Decode(encoded);

        // Assert
        encoded.Should().StartWith("osu file format v14\r\n");
        HitObject savedSlider = decoded.HitObjects.Should().ContainSingle().Subject;
        savedSlider.ControlPoints[0].Type.Should().Be(PathType.Bezier);
        savedSlider.ControlPoints.Should().OnlyContain(point => point.Type == null || point.Type == PathType.Bezier);
        savedSlider.ControlPoints.Skip(1).Should().Contain(point => point.Type == PathType.Bezier);
        savedSlider.PixelLength.Should().Be(slider.PixelLength);
        for (int index = 0; index <= 100; index++)
            Vector2.Distance(savedSlider.GetSliderPath().PositionAt(index / 100d), originalPath.PositionAt(index / 100d))
                .Should().BeLessThan(2);

        slider.ControlPoints.Should().BeEquivalentTo(originalPoints, options => options.WithStrictOrdering());
        slider.ControlPoints[0].Type.Should().Be(type);
    }

    [DataTestMethod]
    [DataRow("P")]
    [DataRow("C")]
    public void Encode_SingleCurvedSegmentInV14_PreservesPathToken(string token)
    {
        // Arrange
        HitObject slider = BeatmapTestData.DecodeHitObject($"64,128,1000,2,0,{token}|114:78|164:128,1,150");

        // Act
        string encoded = BeatmapTestData.EncodeHitObject(slider, 14);

        // Assert
        encoded.Split(',')[5].Should().Be($"{token}|114:78|164:128");
    }

    [DataTestMethod]
    [DataRow("P")]
    [DataRow("C")]
    public void Encode_RepeatedCurvedSegmentsInV128_PreservesExplicitTypes(string token)
    {
        // Arrange
        string path = $"{token}|114:78|{token}|164:128|214:178|264:128";
        HitObject slider = BeatmapTestData.DecodeHitObject($"64,128,1000,2,0,{path},1,300", 128);

        // Act
        string encoded = BeatmapTestData.EncodeHitObject(slider);

        // Assert
        encoded.Split(',')[5].Should().Be(path);
    }
}
