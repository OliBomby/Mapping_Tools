using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class LazerExternalEditBeatmapLocatorTests
{
    [TestMethod]
    public void FindMountedBeatmap_WhenMountAppearsAndDisappears_TracksItsLifetime()
    {
        // Arrange
        using TestTempDirectory temp = new();
        LazerExternalEditBeatmapLocator sut = new(temp.Root);
        string unrelated = temp.CreateBeatmap("ordinary-folder", "unrelated.osu");
        string mounted = temp.CreateBeatmap(new string('a', 64), "mounted.osu");

        // Act
        string? active = sut.FindMountedBeatmap();
        Directory.Delete(Path.GetDirectoryName(mounted)!, true);
        string? afterRemoval = sut.FindMountedBeatmap();

        // Assert
        active.Should().Be(mounted);
        afterRemoval.Should().BeNull();
        File.Exists(unrelated).Should().BeTrue();
    }

    [TestMethod]
    public void FindMountedBeatmap_WithSeveralDifficulties_UsesNewestThenPreferredDifficulty()
    {
        // Arrange
        using TestTempDirectory temp = new();
        string first = temp.CreateBeatmap(new string('b', 64), "first.osu");
        string second = temp.CreateBeatmap(new string('b', 64), "second.osu");
        File.SetLastWriteTimeUtc(first, DateTime.UtcNow.AddMinutes(-2));
        File.SetLastWriteTimeUtc(second, DateTime.UtcNow.AddMinutes(-1));
        LazerExternalEditBeatmapLocator sut = new(temp.Root);

        // Act
        string? newest = sut.FindMountedBeatmap();
        sut.Prefer(first);
        string? preferred = sut.FindMountedBeatmap();

        // Assert
        newest.Should().Be(second);
        preferred.Should().Be(first);
    }

    [TestMethod]
    public async Task FindCurrentBeatmapAsync_WhenExternalEditToggled_OverridesAndRestoresConfiguredBackend()
    {
        // Arrange
        using TestTempDirectory temp = new();
        string mounted = temp.CreateBeatmap(new string('c', 64), "mounted.osu");
        ApplicationSettings settings = new();
        StubCurrentBeatmapLocator backend = new();
        ConfiguredCurrentBeatmapLocator sut = new(
            settings, backend, backend, backend, new LazerExternalEditBeatmapLocator(temp.Root));

        // Act
        string active = await sut.FindCurrentBeatmapAsync();
        settings.AutoDetectLazerExternalEdit = false;
        string disabled = await sut.FindCurrentBeatmapAsync();
        settings.AutoDetectLazerExternalEdit = true;
        Directory.Delete(Path.GetDirectoryName(mounted)!, true);
        string afterRemoval = await sut.FindCurrentBeatmapAsync();

        // Assert
        active.Should().Be(mounted);
        disabled.Should().Be("stable.osu");
        afterRemoval.Should().Be("stable.osu");
        backend.CallCount.Should().Be(2);
    }

    private sealed class StubCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public int CallCount { get; private set; }

        public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult("stable.osu");
        }
    }

    private sealed class TestTempDirectory : IDisposable
    {
        public string Root { get; } = Path.Combine(
            Path.GetTempPath(), $"MappingToolsLazerEditTests-{Guid.NewGuid():N}");

        public TestTempDirectory()
        {
            Directory.CreateDirectory(Root);
        }

        public string CreateBeatmap(string folder, string name)
        {
            string directory = Path.Combine(Root, folder);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, name);
            File.WriteAllText(path, "osu file format v14");
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Root, true);
        }
    }
}
