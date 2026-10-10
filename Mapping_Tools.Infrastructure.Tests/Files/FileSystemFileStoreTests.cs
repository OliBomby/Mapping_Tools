using System.Text;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
public sealed class PhysicalBeatmapsetFileSystemTextTests
{
    [TestMethod]
    public void WriteAllText_WithOsuPath_WritesCompleteText()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsFileStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "map.osu");
        const string text = "first\r\nsecond\r\n";
        PhysicalBeatmapsetFileSystem store = new();

        try
        {
            // Act
            store.WriteAllText(path, text);

            // Assert
            File.ReadAllBytes(path).Should().Equal(Encoding.UTF8.GetBytes(text));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
