using System.Text;
using Mapping_Tools.Infrastructure.Backups;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Backups;

[TestClass]
public sealed class FileSystemBeatmapBackupStoreTests
{
    [TestMethod]
    public async Task CopyAndWriteLinesAsync_WithExistingDestination_ReplacesAtomicallyWithoutTemporaryFiles()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsBackupStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, "source.osu");
            string destination = Path.Combine(directory, "destination.osu");
            await File.WriteAllLinesAsync(source, ["source"]);
            await File.WriteAllLinesAsync(destination, ["previous"]);
            FileSystemBeatmapBackupStore store = new();

            // Act
            await store.CopyAsync(source, destination);
            // Assert
            (await File.ReadAllLinesAsync(destination)).Should().Equal("source");

            await store.WriteLinesAsync(destination, ["snapshot", "complete"]);
            (await File.ReadAllLinesAsync(destination)).Should().Equal("snapshot", "complete");
            File.ReadAllBytes(destination).Should().Equal(
                Encoding.UTF8.GetBytes("snapshot\r\ncomplete\r\n"));
            Directory.EnumerateFiles(directory)
                .Any(path => Path.GetFileName(path)
                    .Contains(".mapping-tools-", StringComparison.Ordinal)).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task CopyAsync_WithMissingSource_PreservesExistingDestination()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsBackupStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string destination = Path.Combine(directory, "destination.osu");
            await File.WriteAllLinesAsync(destination, ["previous"]);
            FileSystemBeatmapBackupStore store = new();

            // Act
            var act1 = () => store.CopyAsync(
                Path.Combine(directory, "missing.osu"),
                destination);

            // Assert
            await act1.Should().ThrowAsync<FileNotFoundException>();

            (await File.ReadAllLinesAsync(destination)).Should().Equal("previous");
            Directory.EnumerateFiles(directory).Count().Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task ListAndDeleteAsync_WithMultipleBackups_OrdersAndDeletesRequestedFile()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsBackupStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string older = Path.Combine(directory, "older.osu");
            string newer = Path.Combine(directory, "newer.osu");
            await File.WriteAllTextAsync(older, "old");
            await File.WriteAllTextAsync(newer, "new");
            File.SetCreationTimeUtc(older, DateTime.UtcNow.AddMinutes(-2));
            File.SetCreationTimeUtc(newer, DateTime.UtcNow.AddMinutes(-1));
            FileSystemBeatmapBackupStore store = new();

            // Act
            var files =
                await store.ListAsync(directory);
            await store.DeleteAsync(older);

            // Assert
            files[0].Path.Should().Be(newer);
            File.Exists(older).Should().BeFalse();
            File.Exists(newer).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestMethod]
    public async Task ListAsync_WithSameCreationTime_PrioritizesEditorReaderBackup()
    {
        // Arrange
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsBackupStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string diskBackup = Path.Combine(
                directory,
                "2026-07-25 14-05-06___map.osu");
            string editorReaderBackup = Path.Combine(
                directory,
                "2026-07-25 14-05-06__2_map.osu");
            await File.WriteAllTextAsync(diskBackup, "disk");
            await File.WriteAllTextAsync(editorReaderBackup, "editor");
            DateTime creationTime = DateTime.UtcNow.AddMinutes(-1);
            File.SetCreationTimeUtc(diskBackup, creationTime);
            File.SetCreationTimeUtc(editorReaderBackup, creationTime);
            FileSystemBeatmapBackupStore store = new();

            // Act
            var files = await store.ListAsync(directory);

            // Assert
            files[0].Path.Should().Be(editorReaderBackup);
            files[1].Path.Should().Be(diskBackup);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
