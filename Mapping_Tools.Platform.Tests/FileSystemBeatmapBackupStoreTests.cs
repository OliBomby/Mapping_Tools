using Mapping_Tools.ApplicationServices.Backups;
using Mapping_Tools.Infrastructure.Backups;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class FileSystemBeatmapBackupStoreTests
{
    [TestMethod]
    public async Task CopyAndSnapshotWritesReplaceAtomicallyWithoutLeavingTemporaryFiles()
    {
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

            await store.CopyAsync(source, destination);
            CollectionAssert.AreEqual(
                new[] { "source" },
                await File.ReadAllLinesAsync(destination));

            await store.WriteLinesAsync(destination, ["snapshot", "complete"]);
            CollectionAssert.AreEqual(
                new[] { "snapshot", "complete" },
                await File.ReadAllLinesAsync(destination));
            Assert.IsFalse(
                Directory.EnumerateFiles(directory)
                    .Any(path => Path.GetFileName(path)
                        .Contains(".mapping-tools-", StringComparison.Ordinal)));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task FailedCopyPreservesExistingDestination()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"MappingToolsBackupStore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            string destination = Path.Combine(directory, "destination.osu");
            await File.WriteAllLinesAsync(destination, ["previous"]);
            FileSystemBeatmapBackupStore store = new();

            await Assert.ThrowsExceptionAsync<FileNotFoundException>(
                () => store.CopyAsync(
                    Path.Combine(directory, "missing.osu"),
                    destination));

            CollectionAssert.AreEqual(
                new[] { "previous" },
                await File.ReadAllLinesAsync(destination));
            Assert.AreEqual(1, Directory.EnumerateFiles(directory).Count());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task ListOrdersNewestFirstAndDeleteRemovesOnlyRequestedBackup()
    {
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

            IReadOnlyList<StoredBeatmapBackup> files =
                await store.ListAsync(directory);
            await store.DeleteAsync(older);

            Assert.AreEqual(newer, files[0].Path);
            Assert.IsFalse(File.Exists(older));
            Assert.IsTrue(File.Exists(newer));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
