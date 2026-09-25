using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;
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
        var detector = CreateSimpleDetector();

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

    [TestMethod]
    public void Analyze_WithPotentialUnloadingPattern_ReportsDisruptorWithoutConfirmedAutoFail()
    {
        // Arrange
        AutoFailDetectorEngine detector = CreateSimpleDetector();

        // Act
        AutoFailAnalysis result = detector.Analyze();

        // Assert
        result.HasAutoFail.Should().BeFalse();
        result.UnloadingObjects.Should().BeEmpty();
        result.PotentialUnloadingObjects.Should().Equal(1000);
        result.Disruptors.Should().Equal(1200);
    }

    [TestMethod]
    public void GetFixPlans_WithNoProblemAreas_ReturnsNoPlans()
    {
        // Arrange
        List<HitObject> hitObjects = [new() { Time = 1000, EndTime = 1000, ObjectType = 1 }];
        AutoFailDetectorEngine detector = new(hitObjects, 0, 2000, 1800, 750, 150, 9);
        AutoFailAnalysis result = detector.Analyze();

        // Act
        List<AutoFailFixPlan> plans = detector.GetFixPlans().ToList();

        // Assert
        result.HasAutoFail.Should().BeFalse();
        plans.Should().BeEmpty();
    }

    [TestMethod]
    public void SetHitObjects_AfterAnalysis_InvalidatesOldFixPlansAndAnalyzesNewObjects()
    {
        // Arrange
        AutoFailDetectorEngine detector = CreateSimpleDetector();
        detector.Analyze();
        detector.SetHitObjects([new HitObject { Time = 1000, EndTime = 1000, ObjectType = 1 }]);

        // Act
        var oldPlan = () => detector.GetFixPlans().ToList();

        // Assert
        oldPlan.Should().Throw<InvalidOperationException>().WithMessage("*Analyze*");

        // Act
        AutoFailAnalysis result = detector.Analyze();

        // Assert
        result.HasAutoFail.Should().BeFalse();
        result.PotentialUnloadingObjects.Should().BeEmpty();
    }

    [TestMethod]
    public void ApplyFix_WithPlanForDifferentAnalysis_RejectsPaddingLength()
    {
        // Arrange
        AutoFailDetectorEngine detector = CreateSimpleDetector();
        detector.Analyze();
        AutoFailFixPlan invalidPlan = new([0], "invalid");

        // Act
        var act = () => detector.ApplyFix(invalidPlan);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*does not match*");
    }

    [TestMethod]
    public void GetFixPlans_EnumeratesAlternativePlansInNondecreasingPaddingCount()
    {
        // Arrange
        AutoFailDetectorEngine detector = CreateSimpleDetector();
        detector.Analyze();

        // Act
        List<AutoFailFixPlan> plans = detector.GetFixPlans().Take(2).ToList();

        // Assert
        plans.Should().HaveCount(2);
        plans[0].Padding.Should().OnlyContain(count => count >= 0);
        plans[1].Padding.Should().OnlyContain(count => count >= 0);
        plans[0].Padding.Sum().Should().BeLessThanOrEqualTo(plans[1].Padding.Sum());
        plans.Should().OnlyContain(plan => plan.Padding.Count == plans[0].Padding.Count);
    }

    [TestMethod]
    public void GetFixPlans_WhenCancelledBeforeEnumeration_ThrowsOperationCanceledException()
    {
        // Arrange
        AutoFailDetectorEngine detector = CreateSimpleDetector();
        detector.Analyze();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Action act = () => detector.GetFixPlans(cancellation.Token).ToList();

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    private static AutoFailDetectorEngine CreateSimpleDetector()
    {
        List<HitObject> hitObjects =
        [
            new()
            {
                Pos = new Vector2(256, 192),
                Time = 1000,
                ObjectType = 2,
                Repeat = 1,
                EndTime = 5000,
            },
            new()
            {
                Pos = new Vector2(256, 192),
                Time = 1200,
                EndTime = 1200,
                ObjectType = 1,
            },
        ];
        return new AutoFailDetectorEngine(hitObjects, 0, 10000, 9000, 750, 150, 9);
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
