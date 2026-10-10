using System.IO.Compression;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Infrastructure.Tools.PatternGallery;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.PatternGallery;

[TestClass]
public sealed class PatternGalleryFileEditTests
{
    [TestMethod]
    public void MergeCollection_WithNewCollection_RecordsFilesForUndoAndRedo()
    {
        // Arrange
        using TestCollection collection = new();
        var service = CreateService(collection.Files);
        PatternGalleryCollectionChange? change = null;
        PatternGalleryServiceOptions project = new();
        PatternGalleryServiceOptions imported = new();
        imported.Patterns.Add(new PatternGalleryPattern { FileName = "pattern.osu" });
        byte[] bytes = [1, 2, 3];

        // Act
        using (PatternGalleryFileEdit edit = new(collection.Files, value => change = value, collection.Paths))
            service.MergeCollection(project, imported, [new("pattern.osu", bytes)], collection.Paths, edit);
        change!.Undo();
        bool removed = !collection.Files.CollectionExists(collection.Paths);
        change.Redo();

        // Assert
        removed.Should().BeTrue();
        project.Patterns.Should().ContainSingle();
        collection.Files.ReadPatternBytes(collection.PatternPath).Should().Equal(bytes);
    }

    [TestMethod]
    public async Task DeleteAsync_WithUnrelatedLockedFile_RecordsOnlyDeletedPattern()
    {
        // Arrange
        using TestCollection collection = new();
        collection.Files.EnsureCollection(collection.Paths);
        byte[] bytes = [1, 2, 3];
        File.WriteAllBytes(collection.PatternPath, bytes);
        using FileStream unrelated = new(Path.Combine(collection.Paths.Collection, "unrelated.bin"),
            FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        var service = CreateService(collection.Files);
        PatternGalleryCollectionChange? change = null;

        // Act
        using (PatternGalleryFileEdit edit = new(collection.Files, value => change = value, collection.Paths))
            await service.DeleteAsync([new PatternGalleryPattern { FileName = "pattern.osu" }],
                collection.Paths, fileEdit: edit);
        change!.Undo();
        byte[] restored = collection.Files.ReadPatternBytes(collection.PatternPath);
        change.Redo();

        // Assert
        restored.Should().Equal(bytes);
        File.Exists(collection.PatternPath).Should().BeFalse();
        change.EstimatedBytes.Should().Be(bytes.Length);
    }

    [TestMethod]
    public void Undo_NewCollectionWithExternalFile_PreservesExternalFileAndRecordedPattern()
    {
        // Arrange
        using TestCollection collection = new();
        PatternGalleryCollectionChange? change = null;
        using (PatternGalleryFileEdit edit = new(collection.Files, value => change = value, collection.Paths))
        {
            edit.CaptureFile(collection.PatternPath);
            collection.Files.WritePatternBytes(collection.PatternPath, [1, 2, 3]);
        }
        string externalPath = Path.Combine(collection.Paths.Collection, "external.txt");
        File.WriteAllText(externalPath, "external edit");

        // Act
        Action act = change!.Undo;

        // Assert
        act.Should().Throw<IOException>();
        File.ReadAllText(externalPath).Should().Be("external edit");
        File.ReadAllBytes(collection.PatternPath).Should().Equal(1, 2, 3);
    }

    [TestMethod]
    public async Task ExtractAsync_WithAdditionalArchiveAsset_RecordsEveryExtractedFile()
    {
        // Arrange
        using TestCollection collection = new();
        string archivePath = Path.Combine(collection.Root, "collection.zip");
        using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
            using StreamWriter writer = new(archive.CreateEntry("Gallery/assets/extra.txt").Open());
            writer.Write("asset");
        }
        PatternGalleryCollectionChange? change = null;
        PatternGalleryArchiveService archives = new();

        // Act
        using (PatternGalleryFileEdit edit = new(collection.Files, value => change = value, collection.Paths))
            await archives.ExtractAsync(archivePath, collection.Root, fileEdit: edit);
        change!.Undo();
        bool removed = !collection.Files.CollectionExists(collection.Paths);
        change.Redo();

        // Assert
        removed.Should().BeTrue();
        File.ReadAllText(Path.Combine(collection.Paths.Collection, "assets", "extra.txt")).Should().Be("asset");
    }

    private static PatternGalleryService CreateService(PatternGalleryFileService files)
    {
        return new PatternGalleryService(new RecordingBeatmapEditingGateway(), files,
            new ApplicationSettings(), new BeatmapDecoder(), new BeatmapEncoder());
    }

    private sealed class TestCollection : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "MappingToolsUndo", Guid.NewGuid().ToString("N"));

        public PatternGalleryFileService Files { get; } = new();

        public PatternGalleryCollectionPaths Paths { get; }

        public string PatternPath => Files.GetPatternPath(Paths, "pattern.osu");

        public TestCollection()
        {
            Directory.CreateDirectory(Root);
            Paths = Files.Resolve(Root, new PatternGalleryCollectionMetadata
            {
                CollectionFolderName = "Gallery",
                PatternFilesFolderName = "Patterns",
            });
        }

        public void Dispose() => Directory.Delete(Root, true);
    }
}
