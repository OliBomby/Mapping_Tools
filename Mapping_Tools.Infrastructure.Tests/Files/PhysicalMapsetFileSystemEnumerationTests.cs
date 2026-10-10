using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
public sealed class PhysicalBeatmapsetFileSystemEnumerationTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-mapset-enumeration-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [TestInitialize]
    public void Initialize()
    {
        Directory.CreateDirectory(root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public void EnumerateFiles_WithNestedSources_UsesStablePathOrder()
    {
        // Arrange
        File.WriteAllText(Path.Combine(root, "A.osu"), string.Empty);
        File.WriteAllText(Path.Combine(root, "b.osu"), string.Empty);
        string nested = Path.Combine(root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "c.osu"), string.Empty);

        // Act
        var files = new PhysicalBeatmapsetFileSystem()
            .EnumerateFiles(root, "*.osu", SearchOption.AllDirectories);

        // Assert
        files.Select(Path.GetFileName).Should().Equal("A.osu", "b.osu", "c.osu");
    }
}
