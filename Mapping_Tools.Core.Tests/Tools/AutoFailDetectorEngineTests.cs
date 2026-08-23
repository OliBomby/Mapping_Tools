using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.AutoFail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

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
