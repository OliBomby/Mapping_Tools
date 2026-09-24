using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor;

[TestClass]
public sealed class ConfiguredBackendSelectionTests
{
    [TestMethod]
    public async Task LiveReader_WhenDisabled_DoesNotCallEitherBackend()
    {
        // Arrange
        var settings = new ApplicationSettings
        {
            BeatmapLiveStateReading = BeatmapLiveStateReadingMode.Disabled,
        };
        var editorReader = new RecordingLiveReader();
        var mtipcReader = new RecordingLiveReader();
        var sut = new ConfiguredLiveBeatmapReader(settings, editorReader, mtipcReader);

        // Act
        LiveBeatmapSnapshot? result = await sut.ReadAsync();

        // Assert
        result.Should().BeNull();
        editorReader.CallCount.Should().Be(0);
        mtipcReader.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task LiveReader_WhenMtipcSelected_OnlyCallsMtipcBackend()
    {
        // Arrange
        var settings = new ApplicationSettings
        {
            BeatmapLiveStateReading = BeatmapLiveStateReadingMode.Mtipc,
        };
        var editorReader = new RecordingLiveReader();
        var mtipcReader = new RecordingLiveReader();
        var sut = new ConfiguredLiveBeatmapReader(settings, editorReader, mtipcReader);

        // Act
        await sut.ReadAsync();

        // Assert
        editorReader.CallCount.Should().Be(0);
        mtipcReader.CallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task CurrentBeatmapLocator_WhenDisabled_DoesNotCallEitherBackend()
    {
        // Arrange
        var settings = new ApplicationSettings
        {
            CurrentBeatmapFetching = CurrentBeatmapFetchingMode.Disabled,
        };
        var memoryReader = new RecordingCurrentBeatmapLocator();
        var mtipcReader = new RecordingCurrentBeatmapLocator();
        var gosumemoryReader = new RecordingCurrentBeatmapLocator();
        var sut = new ConfiguredCurrentBeatmapLocator(
            settings,
            memoryReader,
            mtipcReader,
            gosumemoryReader);

        // Act
        Func<Task> act = () => sut.FindCurrentBeatmapAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        memoryReader.CallCount.Should().Be(0);
        mtipcReader.CallCount.Should().Be(0);
    }

    [TestMethod]
    public async Task EditorReload_WhenDisabled_DoesNotCallEitherBackend()
    {
        // Arrange
        var settings = new ApplicationSettings
        {
            EditorReload = EditorReloadMode.Disabled,
        };
        var simulatedReload = new RecordingReloadService();
        var mtipcReload = new RecordingReloadService();
        var sut = new ConfiguredEditorReloadService(settings, simulatedReload, mtipcReload);

        // Act
        await sut.ReloadAsync();

        // Assert
        simulatedReload.CallCount.Should().Be(0);
        mtipcReload.CallCount.Should().Be(0);
    }

    private sealed class RecordingCurrentBeatmapLocator : ICurrentBeatmapLocator
    {
        public int CallCount { get; private set; }

        public Task<string> FindCurrentBeatmapAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult("map.osu");
        }
    }

    private sealed class RecordingLiveReader : ILiveBeatmapReader
    {
        public int CallCount { get; private set; }

        public Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<LiveBeatmapSnapshot?>(null);
        }
    }

    private sealed class RecordingReloadService : IEditorReloadService
    {
        public int CallCount { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
