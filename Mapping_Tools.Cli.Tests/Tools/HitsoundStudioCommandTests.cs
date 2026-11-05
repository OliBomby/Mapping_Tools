using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class HitsoundStudioCommandTests
{
    [TestMethod]
    public async Task RunAsync_MidiImportWithLayerOverride_PreservesImportedNotesAndChangesSampleVolumeWithoutExporting()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-import-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        string midi = Path.Combine(directory, "notes.mid");
        string project = Path.Combine(directory, "studio.json");
        await File.WriteAllBytesAsync(
            midi,
            [
                77, 84, 104, 100, 0, 0, 0, 6, 0, 0, 0, 1, 1, 224,
                77, 84, 114, 107, 0, 0, 0, 13,
                0, 144, 60, 100, 131, 96, 128, 60, 0, 0, 255, 47, 0
            ]);

        string import = "{type = 'midi', paths = ['" + midi.Replace('\\', '/') + "'], layer = {sample-args = {volume = 0.5}}}";

        // Act
        var result = await CliRunnerTests.RunAsync("hitsound-studio", "--import", import, "--save-project", project, "--no-run");

        // Assert
        try
        {
            result.Code.Should().Be(0, result.Error);

            var layers = (JArray)JObject.Parse(await File.ReadAllTextAsync(project))["HitsoundLayers"]!;
            layers.Should().NotBeEmpty();
            layers.Select(layer => layer["SampleArgs"]!["Volume"]!.Value<double>()).Should().AllBeEquivalentTo(0.5);
            layers.SelectMany(layer => (JArray)layer["Times"]!).Should().NotBeEmpty();
            Directory.EnumerateFiles(directory).Select(Path.GetFileName).Should().BeEquivalentTo("notes.mid", "studio.json");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
