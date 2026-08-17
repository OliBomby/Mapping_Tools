using Mapping_Tools.Application.PatternGallery;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Infrastructure.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.PatternGallery;

[TestClass]
public sealed class PatternGalleryFileServiceTests
{
    [TestMethod]
    public void GetPatternPath_WithTraversalFilename_ThrowsArgumentException()
    {
        // Arrange
        PatternGalleryFileService service = new();
        PatternGalleryCollectionPaths paths = new("C:\\Collections", "C:\\Collections\\Gallery", "C:\\Collections\\Gallery\\Pattern Files", "C:\\Collections\\Gallery\\project.json");

        // Act
        Action act = () => service.GetPatternPath(paths, "..\\outside.osu");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void WritePatternBytes_WithExistingDestination_PreservesExistingFile()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "pattern.osu");
        Directory.CreateDirectory(root);
        PatternGalleryFileService service = new();
        byte[] original = [1, 2, 3];

        try
        {
            File.WriteAllBytes(path, original);

            // Act
            Action act = () => service.WritePatternBytes(path, [4, 5, 6]);

            // Assert
            act.Should().Throw<IOException>();
            File.ReadAllBytes(path).Should().Equal(original);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
