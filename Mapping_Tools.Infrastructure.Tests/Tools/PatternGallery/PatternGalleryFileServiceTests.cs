using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryFileServiceTests
{
    [TestMethod]
    public void ApplyCollectionFileChanges_UndoAndRedo_RestoresPatternBytes()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        PatternGalleryFileService service = new();
        var paths = service.Resolve(root, new() { CollectionFolderName = "Gallery", PatternFilesFolderName = "Patterns" });
        service.EnsureCollection(paths);
        string patternPath = service.GetPatternPath(paths, "pattern.osu");
        byte[] original = [1, 2, 3];
        byte[] changed = [4, 5, 6];
        File.WriteAllBytes(patternPath, changed);
        PatternGalleryFileChange[] changes = [new(Path.Combine("Patterns", "pattern.osu"), original, changed)];

        try
        {
            // Act
            service.ApplyCollectionFileChanges(paths, changes, true);
            byte[] undone = File.ReadAllBytes(patternPath);
            service.ApplyCollectionFileChanges(paths, changes, false);

            // Assert
            undone.Should().Equal(original);
            File.ReadAllBytes(patternPath).Should().Equal(changed);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void ApplyCollectionFileChanges_WithUnexpectedFileChange_PreservesExternalEdit()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        PatternGalleryFileService service = new();
        var paths = service.Resolve(root, new() { CollectionFolderName = "Gallery", PatternFilesFolderName = "Patterns" });
        service.EnsureCollection(paths);
        string patternPath = service.GetPatternPath(paths, "pattern.osu");
        byte[] external = [9, 9, 9];
        File.WriteAllBytes(patternPath, external);
        PatternGalleryFileChange[] changes = [new(Path.Combine("Patterns", "pattern.osu"), [1, 2, 3], [4, 5, 6])];

        try
        {
            // Act
            Action act = () => service.ApplyCollectionFileChanges(paths, changes, true);

            // Assert
            act.Should().Throw<IOException>();
            File.ReadAllBytes(patternPath).Should().Equal(external);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void GetPatternPath_WithTraversalFilename_ThrowsArgumentException()
    {
        // Arrange
        PatternGalleryFileService service = new();
        PatternGalleryCollectionPaths paths = new("C:\\Collections", "C:\\Collections\\Gallery", "C:\\Collections\\Gallery\\Pattern Files",
            "C:\\Collections\\Gallery\\project.json");

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
            var act = () => service.WritePatternBytes(path, [4, 5, 6]);

            // Assert
            act.Should().Throw<IOException>();
            File.ReadAllBytes(path).Should().Equal(original);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
