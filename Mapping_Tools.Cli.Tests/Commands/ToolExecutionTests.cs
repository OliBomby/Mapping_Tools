using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Commands;

[TestClass]
public sealed class ToolExecutionTests
{
    [TestMethod]
    [DataRow("auto-fail-detector", "Path", true)]
    [DataRow("hitsound-studio", "BaseBeatmap", true)]
    [DataRow("hitsound-copier", "PathTo", false)]
    [DataRow("timing-copier", "ExportPath", false)]
    [DataRow("metadata-manager", "ExportPath", false)]
    [DataRow("rhythm-guide", "GuideGeneratorArgs.Paths", false)]
    public async Task RunAsync_TargetPaths_WritesTheToolSpecificProjectFields(string command, string property, bool single)
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-target-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string project = Path.Combine(directory, "project.json");
        string[] paths = single ? [Path.Combine(directory, "first.osu")]
            : [Path.Combine(directory, "first.osu"), Path.Combine(directory, "second.osu")];

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync([command, .. paths, "--save-project", project, "--no-run"]);

            // Assert
            result.Code.Should().Be(0, result.Error);
            var document = JObject.Parse(await File.ReadAllTextAsync(project));
            document["CliTargets"]!.Values<string>().Should().Equal(paths);
            var target = document.SelectToken(property)!;

            if (target is JArray array)
                array.Values<string>().Should().Equal(paths);
            else
                target.Value<string>()!.Split('|').Should().Equal(paths);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task RunAsync_PartialNestedSettings_PreservesExplicitBeatmapTargets()
    {
        // Arrange
        string project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        string target = Path.GetFullPath("map.osu");

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                "rhythm-guide", target, "--guide-generator-args", "{export-mode = 'new-map'}", "--save-project", project, "--no-run");

            // Assert
            result.Code.Should().Be(0, result.Error);
            var document = JObject.Parse(await File.ReadAllTextAsync(project));
            document.SelectToken("GuideGeneratorArgs.Paths")!.Values<string>().Should().Equal(target);
        }
        finally
        {
            File.Delete(project);
        }
    }

    [TestMethod]
    [DataRow("hitsound-copier")]
    [DataRow("timing-copier")]
    [DataRow("metadata-manager")]
    [DataRow("combo-colour-studio")]
    public async Task RunAsync_ConflictingSourceChoices_RejectsBeforeSavingTheProject(string command)
    {
        // Arrange
        string project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(
                command, "--from-current", "--from", "missing.osu", "--save-project", project, "--no-run");

            // Assert
            result.Code.Should().Be(2);
            result.Error.Should().Contain("--from");
            File.Exists(project).Should().BeFalse();
        }
        finally
        {
            File.Delete(project);
        }
    }

    [TestMethod]
    [DataRow("hitsound-studio")]
    [DataRow("mapset-merger")]
    public async Task RunAsync_CleanOutputWithoutOverwrite_RejectsBeforeSavingTheProject(string command)
    {
        // Arrange
        string project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        try
        {
            // Act
            var result = await CliRunnerTests.RunAsync(command, "--clean-output", "--save-project", project, "--no-run");

            // Assert
            result.Code.Should().Be(2);
            result.Error.Should().Contain("--overwrite");
            File.Exists(project).Should().BeFalse();
        }
        finally
        {
            File.Delete(project);
        }
    }
}
