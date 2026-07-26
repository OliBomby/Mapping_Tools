using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Infrastructure.SafetyCopies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

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
            (await File.ReadAllLinesAsync(destination)).Should().Equal(new[] { "source" });

            await store.WriteLinesAsync(destination, ["snapshot", "complete"]);
            (await File.ReadAllLinesAsync(destination)).Should().Equal(new[] { "snapshot", "complete" });
            Directory.EnumerateFiles(directory)
                .Any(path => Path.GetFileName(path)
                    .Contains(".mapping-tools-", StringComparison.Ordinal)).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
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
            Func<Task> act1 = () => store.CopyAsync(
                    Path.Combine(directory, "missing.osu"),
                    destination);

            // Assert
            await act1.Should().ThrowAsync<FileNotFoundException>();

            (await File.ReadAllLinesAsync(destination)).Should().Equal(new[] { "previous" });
            Directory.EnumerateFiles(directory).Count().Should().Be(1);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
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
            IReadOnlyList<StoredBeatmapBackup> files =
                await store.ListAsync(directory);
            await store.DeleteAsync(older);

            // Assert
            files[0].Path.Should().Be(newer);
            File.Exists(older).Should().BeFalse();
            File.Exists(newer).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
