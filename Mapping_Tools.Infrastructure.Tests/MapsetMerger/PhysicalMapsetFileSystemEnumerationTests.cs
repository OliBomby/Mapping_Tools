using Mapping_Tools.Infrastructure.MapsetMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.MapsetMerger;

[TestClass]
public sealed class PhysicalMapsetFileSystemEnumerationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-mapset-enumeration-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    [TestInitialize]
    public void Initialize()
    {
        Directory.CreateDirectory(_root);
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
        File.WriteAllText(Path.Combine(_root, "A.osu"), string.Empty);
        File.WriteAllText(Path.Combine(_root, "b.osu"), string.Empty);
        string nested = Path.Combine(_root, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "c.osu"), string.Empty);

        // Act
        var files = new PhysicalMapsetFileSystem()
            .EnumerateFiles(_root, "*.osu");

        // Assert
        files.Select(Path.GetFileName).Should().Equal("A.osu", "b.osu", "c.osu");
    }
}
