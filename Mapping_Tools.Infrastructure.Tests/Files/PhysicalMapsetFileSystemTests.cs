using System.Diagnostics.CodeAnalysis;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public sealed class PhysicalBeatmapsetFileSystemTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "mapping-tools-mapset-transaction-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    [TestInitialize]
    public void Initialize()
    {
        Directory.CreateDirectory(root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    [TestMethod]
    public void ComponentOperations_ReadWriteCopyMoveDeleteAndEnsureDirectories()
    {
        // Arrange
        PhysicalBeatmapsetFileSystem fileSystem = new();
        string components = Path.Combine(root, "components");
        string source = Path.Combine(components, "source.bin");
        string copy = Path.Combine(components, "copies", "copy.bin");
        string moved = Path.Combine(components, "moved.bin");

        // Act
        fileSystem.EnsureDirectoryExists(components);
        fileSystem.WriteAllBytes(source, [1, 2, 3]);
        fileSystem.CopyFile(source, copy);
        fileSystem.MoveFile(copy, moved);
        byte[] contents = fileSystem.ReadAllBytes(moved);
        fileSystem.Delete(moved);

        // Assert
        contents.Should().Equal(1, 2, 3);
        fileSystem.FileExists(moved).Should().BeFalse();
        fileSystem.DirectoryExists(Path.Combine(components, "copies")).Should().BeTrue();
        fileSystem.EnumerateFiles(root, "*.bin", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Should().Equal("source.bin");
    }

    [TestMethod]
    public async Task CommitAsync_LateOutputCollision_PublishesNoFilesAndPreservesExistingContents()
    {
        // Arrange
        string destination = Path.Combine(root, "export");
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "z-existing.txt"), "original");
        using var transaction = new PhysicalBeatmapsetFileSystem().BeginTransaction(destination);
        await File.WriteAllTextAsync(transaction.GetStagedPath("a-new.txt"), "new");
        await File.WriteAllTextAsync(transaction.GetStagedPath("z-existing.txt"), "replacement");

        // Act
        Func<Task> act = () => transaction.CommitAsync(overwrite: false);

        // Assert
        await act.Should().ThrowAsync<IOException>();
        File.Exists(Path.Combine(destination, "a-new.txt")).Should().BeFalse();
        (await File.ReadAllTextAsync(Path.Combine(destination, "z-existing.txt"))).Should().Be("original");
    }

    [TestMethod]
    public async Task CommitAsync_OverwriteEnabled_ReplacesGeneratedFilesAndPreservesUnrelatedFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "export");
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "generated.txt"), "original");
        await File.WriteAllTextAsync(Path.Combine(destination, "unrelated.txt"), "keep");
        using var transaction = new PhysicalBeatmapsetFileSystem().BeginTransaction(destination);
        await File.WriteAllTextAsync(transaction.GetStagedPath("generated.txt"), "replacement");

        // Act
        var outputFiles = await transaction.CommitAsync(overwrite: true);

        // Assert
        outputFiles.Should().Equal(Path.Combine(destination, "generated.txt"));
        (await File.ReadAllTextAsync(Path.Combine(destination, "generated.txt"))).Should().Be("replacement");
        (await File.ReadAllTextAsync(Path.Combine(destination, "unrelated.txt"))).Should().Be("keep");
    }

    [TestMethod]
    public async Task CommitAsync_CleanupEnabled_RemovesExistingFilesIncludingNestedFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "export");
        Directory.CreateDirectory(Path.Combine(destination, "nested"));
        await File.WriteAllTextAsync(Path.Combine(destination, "nested", "unrelated.txt"), "old");
        using var transaction = new PhysicalBeatmapsetFileSystem().BeginTransaction(destination);
        await File.WriteAllTextAsync(transaction.GetStagedPath("generated.txt"), "new");

        // Act
        var outputFiles = await transaction.CommitAsync(overwrite: true, clearExisting: true);

        // Assert
        outputFiles.Should().Equal(Path.Combine(destination, "generated.txt"));
        Directory.GetFiles(destination, "*", SearchOption.AllDirectories).Should()
            .Equal(Path.Combine(destination, "generated.txt"));
    }

    [TestMethod]
    public async Task CommitAsync_CleanupWithoutOverwritePermission_PreservesExistingFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "export");
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "existing.txt"), "original");
        using var transaction = new PhysicalBeatmapsetFileSystem().BeginTransaction(destination);

        // Act
        Func<Task> act = () => transaction.CommitAsync(overwrite: false, clearExisting: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        (await File.ReadAllTextAsync(Path.Combine(destination, "existing.txt"))).Should().Be("original");
    }

    [TestMethod]
    public async Task CommitAsync_FailureAfterCleanup_RestoresDeletedFilesAndRemovesNewFiles()
    {
        // Arrange
        string destination = Path.Combine(root, "export");
        Directory.CreateDirectory(Path.Combine(destination, "z-failure.txt"));
        await File.WriteAllTextAsync(Path.Combine(destination, "existing.txt"), "original");
        using var transaction = new PhysicalBeatmapsetFileSystem().BeginTransaction(destination);
        await File.WriteAllTextAsync(transaction.GetStagedPath("a-new.txt"), "new");
        await File.WriteAllTextAsync(transaction.GetStagedPath("z-failure.txt"), "blocked");

        // Act
        Func<Task> act = () => transaction.CommitAsync(overwrite: true, clearExisting: true);

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        (exception.Which is IOException or UnauthorizedAccessException).Should().BeTrue();
        (await File.ReadAllTextAsync(Path.Combine(destination, "existing.txt"))).Should().Be("original");
        File.Exists(Path.Combine(destination, "a-new.txt")).Should().BeFalse();
    }

    [TestMethod]
    public async Task CommitAsync_WhenReplacementFails_RestoresOriginalDuplicateTarget()
    {
        // Arrange
        string export = Path.Combine(root, "export");
        Directory.CreateDirectory(export);
        string original = Path.Combine(export, "same.txt");
        await File.WriteAllTextAsync(original, "original");
        string firstSource = Path.Combine(root, "first.txt");
        string secondSource = Path.Combine(root, "second.txt");
        await File.WriteAllTextAsync(firstSource, "first");
        await File.WriteAllTextAsync(secondSource, "second");

        using var transaction =
            new PhysicalBeatmapsetFileSystem().BeginTransaction(export);
        transaction.CopyToStaging(firstSource, "same.txt");
        transaction.CopyToStaging(secondSource, "same.txt");
        transaction.CopyToStaging(firstSource, "new-directory/new.txt");
        Directory.CreateDirectory(Path.Combine(export, "failure.txt"));
        transaction.CopyToStaging(firstSource, "failure.txt");

        // Act
        var act = () => transaction.CommitAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<Exception>();
        (exception.Which is IOException or UnauthorizedAccessException).Should().BeTrue();
        (await File.ReadAllTextAsync(original)).Should().Be("original");
        Directory.Exists(Path.Combine(export, "new-directory")).Should().BeFalse();
        Directory.Exists(Path.Combine(export, "failure.txt")).Should().BeTrue();
        Directory.GetDirectories(root, ".export.mapset-merger-*", SearchOption.TopDirectoryOnly)
            .Should().BeEmpty();
    }
}
