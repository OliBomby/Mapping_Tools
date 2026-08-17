using Mapping_Tools.Infrastructure.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.PatternGallery;

[TestClass]
public sealed class PatternGalleryArchiveServiceTests
{
    [TestMethod]
    public async Task ExportAsync_ThenReadAsync_PreservesProjectAndPatternBytes()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string archivePath = Path.Combine(root, "collection.zip");
        PatternGalleryArchiveService service = new();
        byte[] patternBytes = [1, 2, 3, 4];

        try
        {
            // Act
            await service.ExportAsync(
                archivePath,
                "Collection_01",
                "project.json",
                "{\"CollectionName\":\"Test\"}",
                [new("pattern.osu", patternBytes)]);
            var archive = await service.ReadAsync(archivePath);

            // Assert
            archive.CollectionFolderName.Should().Be("Collection_01");
            archive.ProjectFileName.Should().Be("project.json");
            archive.ProjectJson.Should().Contain("Test");
            archive.PatternFiles.Should().ContainSingle();
            archive.PatternFiles[0].FileName.Should().Be("pattern.osu");
            archive.PatternFiles[0].Content.Should().Equal(patternBytes);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task ReadAsync_WithTraversalEntry_RejectsUnsafeArchive()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string archivePath = Path.Combine(root, "unsafe.zip");
        using (var archive = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create))
        {
            archive.CreateEntry("../project.json");
        }
        PatternGalleryArchiveService service = new();

        try
        {
            // Act
            Func<Task> act = () => service.ReadAsync(archivePath);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("The archive contains an unsafe path.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task ReadAsync_WithRootedEntry_RejectsUnsafeArchive()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string archivePath = Path.Combine(root, "unsafe-rooted.zip");
        using (var archive = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create))
        {
            archive.CreateEntry("/project.json");
        }
        PatternGalleryArchiveService service = new();

        try
        {
            // Act
            Func<Task> act = () => service.ReadAsync(archivePath);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>()
                .WithMessage("The archive contains an unsafe path.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
