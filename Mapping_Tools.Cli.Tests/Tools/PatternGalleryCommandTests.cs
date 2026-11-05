using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class PatternGalleryCommandTests
{
    [TestMethod]
    public async Task RunAsync_ExplicitLibraryCollectionAndPatternCode_PersistsCollectionWithoutDesktop()
    {
        // Arrange
        string library = Path.Combine(Path.GetTempPath(), "mapping-tools-gallery-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(library);

        string code = Path.Combine(library, "objects.txt");
        await File.WriteAllTextAsync(code, "64,64,1000,1,0,0:0:0:0:");

        var created = await CliRunnerTests.RunAsync(
            "pattern-gallery", "collection", "create", "--library", library, "--collection", "streams");

        // Act
        var imported = await CliRunnerTests.RunAsync(
            "pattern-gallery",
            "import",
            "--library",
            library,
            "--collection",
            "streams",
            "--object-code",
            code,
            "--name",
            "First");
        var listed = await CliRunnerTests.RunAsync("pattern-gallery", "list", "--library", library, "--collection", "streams");

        // Assert
        try
        {
            created.Code.Should().Be(0, created.Error);
            imported.Code.Should().Be(0, imported.Error);
            listed.Code.Should().Be(0, listed.Error);
            listed.Output.Should().Contain("First");

            var project = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(library, "streams", "project.json")));
            ((JArray)project["Patterns"]!).Should().ContainSingle();
            Directory.EnumerateFiles(Path.Combine(library, "streams", "Pattern Files"), "*.osu").Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(library, true);
        }
    }
}
