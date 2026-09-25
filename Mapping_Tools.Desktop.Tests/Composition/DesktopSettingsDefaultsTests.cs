using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Composition;
using Mapping_Tools.Desktop.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Composition;

[TestClass]
public sealed class DesktopSettingsDefaultsTests
{
    [TestMethod]
    public void Create_OnWindows_SelectsWindowsEditorAdapters()
    {
        // Arrange
        const bool is_windows = true;

        // Act
        DesktopApplicationSettings settings = DesktopSettingsDefaults.Create(is_windows);

        // Assert
        settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.MemoryRead);
        settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.EditorReader);
        settings.EditorReload.Should().Be(EditorReloadMode.SimulatedKeypress);
    }

    [TestMethod]
    public void Create_OnNonWindows_SelectsMtipcAdapters()
    {
        // Arrange
        const bool is_windows = false;

        // Act
        DesktopApplicationSettings settings = DesktopSettingsDefaults.Create(is_windows);

        // Assert
        settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.Gosumemory);
        settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.Disabled);
        settings.EditorReload.Should().Be(EditorReloadMode.Disabled);
    }

    [TestMethod]
    public void LoadOrCreate_WithoutSettings_PersistsOsDefaults()
    {
        // Arrange
        MemorySettingsStore store = new(null);
        SettingsService service = new(store, new NoOpSettingsPathService(),
            () => DesktopSettingsDefaults.Create(isWindows: false));

        // Act
        SettingsLoadResult result = service.LoadOrCreate();

        // Assert
        result.WasCreated.Should().BeTrue();
        result.Settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.Gosumemory);
        store.Load().Should().BeSameAs(result.Settings);
        store.SaveCount.Should().Be(1);
    }

    [TestMethod]
    public void LoadOrCreate_WithExistingSettings_PreservesSelectedModes()
    {
        // Arrange
        ApplicationSettings existing = new()
        {
            CurrentBeatmapFetching = CurrentBeatmapFetchingMode.Mtipc,
            BeatmapLiveStateReading = BeatmapLiveStateReadingMode.Disabled,
            EditorReload = EditorReloadMode.Disabled,
        };
        MemorySettingsStore store = new(existing);
        SettingsService service = new(store, new NoOpSettingsPathService(),
            () => DesktopSettingsDefaults.Create(isWindows: false));

        // Act
        SettingsLoadResult result = service.LoadOrCreate();

        // Assert
        result.WasCreated.Should().BeFalse();
        result.Settings.Should().BeSameAs(existing);
        result.Settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.Mtipc);
        result.Settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.Disabled);
        result.Settings.EditorReload.Should().Be(EditorReloadMode.Disabled);
        store.SaveCount.Should().Be(0);
    }

    private sealed class MemorySettingsStore(ApplicationSettings? settings) : ISettingsStore
    {
        private ApplicationSettings? settings = settings;

        public bool Exists => settings is not null;

        public int SaveCount { get; private set; }

        public ApplicationSettings Load() => settings ?? throw new InvalidOperationException();

        public void Save(ApplicationSettings value)
        {
            settings = value;
            SaveCount++;
        }
    }

    private sealed class NoOpSettingsPathService : ISettingsPathService
    {
        public SettingsPathResult ApplyDefaults(ApplicationSettings settings) => new(false);
    }
}
