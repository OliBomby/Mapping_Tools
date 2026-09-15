using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.AutoFail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.AutoFail;

[TestClass]
public sealed class AutoFailDetectorEngineTests
{
    [TestMethod]
    public void Analyze_WithAcceptedPositiveFixture_ReproducesLegacyCounts()
    {
        // Arrange
        var beatmap = Load("standard-autofail-2b.osu");
        var detector = CreateDetector(beatmap);

        // Act
        var result = detector.Analyze();

        // Assert
        result.HasAutoFail.Should().BeTrue();
        result.UnloadingObjects.Should().HaveCount(20);
        result.PotentialUnloadingObjects.Should().HaveCount(63);
    }

    [TestMethod]
    public void Analyze_WithAcceptedNegativeFixture_ReturnsNoAutoFail()
    {
        // Arrange
        var beatmap = Load("ComplicatedTestMap.osu");
        var detector = CreateDetector(beatmap);

        // Act
        var result = detector.Analyze();

        // Assert
        result.HasAutoFail.Should().BeFalse();
        result.UnloadingObjects.Should().BeEmpty();
        result.PotentialUnloadingObjects.Should().BeEmpty();
    }

    [TestMethod]
    public void GetFixPlans_WithSimpleUnloadingPattern_ReturnsGuideAndRepairsAutoFail()
    {
        // Arrange
        List<HitObject> hitObjects =
        [
            new()
            {
                Pos = new(256, 192),
                Time = 1000,
                ObjectType = 2,
                Repeat = 1,
                EndTime = 5000,
            },
            new()
            {
                Pos = new(256, 192),
                Time = 1200,
                EndTime = 1200,
                ObjectType = 1,
            },
        ];
        var detector = new AutoFailDetectorEngine(
            hitObjects,
            0,
            10000,
            9000,
            750,
            150,
            9);

        detector.Analyze();

        // Act
        var plan = detector.GetFixPlans().First();
        detector.ApplyFix(plan);
        var repaired = detector.Analyze();

        // Assert
        plan.Guide.Should().StartWith("Auto-fail fix guide. Place these extra objects to fix auto-fail:");
        plan.Padding.Should().HaveCount(2);
        repaired.HasAutoFail.Should().BeFalse();
    }

    private static AutoFailDetectorEngine CreateDetector(Beatmap beatmap)
    {
        double approachRate = beatmap.Difficulty["ApproachRate"].DoubleValue;
        double overallDifficulty = beatmap.Difficulty["OverallDifficulty"].DoubleValue;
        return new AutoFailDetectorEngine(
            beatmap.HitObjects,
            (int)beatmap.GetMapStartTime(),
            (int)beatmap.GetMapEndTime(),
            (int)beatmap.GetAutoFailCheckTime(),
            (int)Beatmap.GetApproachTime(approachRate),
            (int)Math.Ceiling(200 - 10 * overallDifficulty),
            9);
    }

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                fileName)).ToList());
    }
}
