using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Settings;

[TestClass]
public sealed class SettingsLiveStateMigrationTests
{
    [TestMethod]
    public void Load_WithLegacyLiveStateBooleans_MigratesToIndependentModes()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), $"Mapping Tools Migration Tests {Guid.NewGuid():N}");
        var directories = new ApplicationDirectories(root);
        directories.EnsureCreated();
        File.WriteAllText(
            directories.PreferencesFile,
            "{\"$schema\":\"mapping-tools.settings\",\"$version\":1,\"UseEditorReader\":false,\"AutoReload\":false}");
        JsonSettingsStore store = new(directories);

        // Act
        ApplicationSettings settings = store.Load();

        // Assert
        settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.MemoryRead);
        settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.Disabled);
        settings.EditorReload.Should().Be(EditorReloadMode.Disabled);

        Directory.Delete(root, recursive: true);
    }
}
