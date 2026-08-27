using System.Text;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
public sealed class PhysicalBeatmapsetFileSystemTextTests
{
    [TestMethod]
    public void WriteAllLines_WithOsuPath_UsesCrLfLineEndings()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsFileStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "map.osu");
        PhysicalBeatmapsetFileSystem store = new();

        try
        {
            // Act
            store.WriteAllLines(path, ["first", "second"]);

            // Assert
            File.ReadAllBytes(path).Should().Equal(
                Encoding.UTF8.GetBytes("first\r\nsecond\r\n"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
