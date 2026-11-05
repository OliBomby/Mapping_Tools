using AwesomeAssertions;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class SlideratorCommandTests
{
    private const string beatmap_text = """
        osu file format v14

        [General]
        Mode:0

        [Metadata]
        Version:Test

        [Difficulty]
        CircleSize:4
        SliderMultiplier:1.4
        SliderTickRate:1

        [TimingPoints]
        0,500,4,2,1,100,1,0
        1000,250,4,2,1,100,1,0

        [HitObjects]
        128,128,0,1,0,0:0:0:0:
        64,64,100,2,0,L|164:64,1,100
        64,64,1000,2,0,L|264:64,1,200
        """;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RunAsync_MultipleSliders_RunsEachSourceWithItsOwnTimingAndExplicitOverrides(bool overrideTime)
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-sliderator-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "map.osu");
        await File.WriteAllTextAsync(path, beatmap_text);
        string[] options = overrideTime ? ["--export-time", "2000", "--export-mode", "add"] : [];

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                ["sliderator", path, "--selection", "everything", "--editor-reader", "disabled", "--no-backups", .. options]);
            var beatmap = new BeatmapDecoder().Decode(await File.ReadAllTextAsync(path));

            // Assert
            result.Code.Should().Be(0, result.Error);
            result.Output.Should().Contain("Completed: 2");
            result.Output.Split(Environment.NewLine).Count(line => line.StartsWith("Processed ", StringComparison.Ordinal)).Should().Be(2);
            beatmap.HitObjects.Count(hitObject => hitObject.IsCircle).Should().Be(1);

            if (overrideTime)
            {
                beatmap.HitObjects.Where(hitObject => hitObject.IsSlider).Select(hitObject => hitObject.Time)
                    .Should().Equal(100, 1000, 2000, 2000);
                beatmap.HitObjects.Should().HaveCount(5);
            }
            else
            {
                beatmap.HitObjects.Where(hitObject => hitObject.IsSlider).Select(hitObject => hitObject.Time).Should().Equal(100, 1000);
                beatmap.HitObjects.Where(hitObject => hitObject.IsSlider).Select(hitObject => hitObject.PixelLength)
                    .Should().Equal([100.0, 200.0], (actual, expected) => Math.Abs(actual - expected) < 1e-8);
                beatmap.HitObjects.Should().HaveCount(3);
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_EmptySelection_ReportsZeroWithoutSaving()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-sliderator-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "map.osu");
        await File.WriteAllTextAsync(path, beatmap_text);

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                "sliderator", path, "--selection", "bookmarked", "--editor-reader", "disabled", "--no-backups");

            // Assert
            result.Code.Should().Be(0, result.Error);
            result.Output.Should().Contain("Completed: 0").And.NotContain("Processed ");
            (await File.ReadAllTextAsync(path)).Should().Be(beatmap_text);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
