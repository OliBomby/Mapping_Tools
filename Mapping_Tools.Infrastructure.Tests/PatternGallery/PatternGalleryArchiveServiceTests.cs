using System.IO.Compression;
using Mapping_Tools.Application.Tools.PatternGallery;
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
        string fixtureRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Patterns", "legacy-collection");
        string patternFileName = "2025-06-07 21-32-54_NEsChy2u__Pattern 1.osu";
        string projectJson = File.ReadAllText(Path.Combine(fixtureRoot, "project.json"));
        byte[] patternBytes = File.ReadAllBytes(Path.Combine(fixtureRoot, "Pattern Files", patternFileName));

        try
        {
            // Act
            await service.ExportAsync(
                archivePath,
                "Collection_01",
                "project.json",
                projectJson,
                [new PatternGalleryArchiveFile(patternFileName, patternBytes)]);
            var archive = await service.ReadAsync(archivePath);

            // Assert
            archive.CollectionFolderName.Should().Be("Collection_01");
            archive.ProjectFileName.Should().Be("project.json");
            archive.ProjectJson.Should().Contain("My Pattern Collection");
            archive.PatternFiles.Should().ContainSingle();
            archive.PatternFiles[0].FileName.Should().Be(patternFileName);
            archive.PatternFiles[0].Content.Should().Equal(patternBytes);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task ReadAsync_WithTraversalEntry_RejectsUnsafeArchive()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string archivePath = Path.Combine(root, "unsafe.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
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
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task ReadAsync_WithRootedEntry_RejectsUnsafeArchive()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), "MappingToolsPatternGallery", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string archivePath = Path.Combine(root, "unsafe-rooted.zip");
        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
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
            Directory.Delete(root, true);
        }
    }
}
