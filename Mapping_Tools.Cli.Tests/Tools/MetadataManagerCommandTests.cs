using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class MetadataManagerCommandTests
{
    [TestMethod]
    public async Task RunAsync_MetadataImportAndExplicitOverride_SavesPreparedSettingsWithoutEditingSourceOrCreatingBackups()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-import-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string source = Path.Combine(directory, "source.osu");
        string project = Path.Combine(directory, "metadata.json");
        string backups = Path.Combine(directory, "backups");
        string original = """
            osu file format v14

            [General]
            Mode:0

            [Metadata]
            Artist:Original artist
            ArtistUnicode:Original artist
            Title:Original title
            TitleUnicode:Original title
            Creator:Mapper
            Version:Test

            [Difficulty]
            SliderMultiplier:1.4

            [TimingPoints]
            0,500,4,2,1,100,1,0

            [HitObjects]
            64,64,1000,1,0,0:0:0:0:
            """;
        await File.WriteAllTextAsync(source, original);

        // Act
        var result = await CliRunnerTests.RunAsync(
            "metadata-manager",
            "--from",
            source,
            "--title",
            "Explicit title",
            "--backup-path",
            backups,
            "--editor-reader",
            "disabled",
            "--save-project",
            project,
            "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var document = JObject.Parse(await File.ReadAllTextAsync(project));
            document["Title"]!.Value<string>().Should().Be("Explicit title");
            document["Artist"]!.Value<string>().Should().Be("Original artist");
            (await File.ReadAllTextAsync(source)).Should().Be(original);
            Directory.EnumerateFiles(backups, "*", SearchOption.AllDirectories).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
