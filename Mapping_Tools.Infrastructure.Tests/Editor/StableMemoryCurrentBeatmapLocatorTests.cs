using System.Diagnostics.CodeAnalysis;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class StableMemoryCurrentBeatmapLocatorTests
{
    [TestMethod]
    public async Task FindCurrentBeatmapAsync_WinePathSeparators_ReturnsExistingNativePath()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string folder = Path.Combine(root, "Artist - Title", "nested");
        Directory.CreateDirectory(folder);
        string expectedPath = Path.Combine(folder, "map.osu");
        await File.WriteAllTextAsync(expectedPath, "osu file format v14");
        int attachmentChecks = 0;
        var sut = new StableMemoryCurrentBeatmapLocator(
            new ApplicationSettings { SongsPath = root }, () => true,
            () => ++attachmentChecks > 1,
            () => new CurrentBeatmap { FolderName = @"Artist - Title\nested", OsuFileName = "map.osu" });
        try
        {
            // Act
            string result = await sut.FindCurrentBeatmapAsync();

            // Assert
            result.Should().Be(expectedPath);
            attachmentChecks.Should().Be(2);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task FindCurrentBeatmapAsync_NoLoadedBeatmap_ThrowsUnavailableError()
    {
        // Arrange
        var sut = new StableMemoryCurrentBeatmapLocator(
            new ApplicationSettings { SongsPath = Path.GetTempPath() }, () => true, () => true,
            () => null);

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync();

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message
            .Should().Contain("Open a beatmap in osu!");
    }

    [TestMethod]
    public async Task FindCurrentBeatmapAsync_MissingFile_ThrowsSongsFolderError()
    {
        // Arrange
        var sut = new StableMemoryCurrentBeatmapLocator(
            new ApplicationSettings { SongsPath = Path.GetTempPath() }, () => true, () => true,
            () => new CurrentBeatmap { FolderName = Guid.NewGuid().ToString(), OsuFileName = "map.osu" });

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync();

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Message
            .Should().Contain("current beatmap file does not exist");
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure",
        Justification = "The operation is awaited before the cancellation source is disposed.")]
    public async Task FindCurrentBeatmapAsync_CanceledWhileAttaching_StopsWaiting()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var sut = new StableMemoryCurrentBeatmapLocator(
            new ApplicationSettings { SongsPath = Path.GetTempPath() }, () => true,
            () => { cancellation.Cancel(); return false; }, () => null);

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync(cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task FindCurrentBeatmapAsync_UnsupportedPlatform_DoesNotInitializeReader()
    {
        // Arrange
        bool initialized = false;
        var sut = new StableMemoryCurrentBeatmapLocator(new ApplicationSettings(), () => false,
            () => { initialized = true; return true; }, () => null);

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        initialized.Should().BeFalse();
    }
}
