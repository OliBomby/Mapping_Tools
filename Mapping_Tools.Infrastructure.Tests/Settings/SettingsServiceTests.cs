using System.Text.Json;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Settings;
using Mapping_Tools.Core.Settings.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Settings;

[TestClass]
public sealed class SettingsServiceTests
{
    [TestMethod]
    public void Load_WithLegacyDocument_PreservesData()
    {
        // Arrange
        using var test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories, typeof(TestApplicationSettings));

        // Act
        var settings = (TestApplicationSettings)store.Load();

        // Assert
        settings.RecentMaps.Count.Should().Be(20);
        settings.RecentMaps[0].Path.Should().EndWith("[3  (2^n) - 2].osu");
        settings.RecentMaps[0].DisplayDate.Should().Be("18/07/2026 17:38:50");
        settings.FavoriteTools.Count.Should().Be(7);
        settings.MainWindowRestoreBounds.Should().Be(new WindowBounds(440, 256, 1407, 855));
        (settings.QuickRunHotkey?.Key).Should().Be(56);
        (settings.QuickRunHotkey?.Modifiers).Should().Be(1);
        (settings.BetterSaveHotkey?.Key).Should().Be(62);
        (settings.BetterSaveHotkey?.Modifiers).Should().Be(6);
        settings.PeriodicBackupInterval.Should().Be(TimeSpan.FromMinutes(10));
        settings.SkipVersion.Should().Be("1.12.1");
    }

    [TestMethod]
    public void Load_WithLegacySettings_WritesPreferencesWithoutChangingConfiguration()
    {
        // Arrange
        using var test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories, typeof(TestApplicationSettings));
        string legacyJson = File.ReadAllText(test.Directories.ConfigurationFile);

        // Act
        var settings = (TestApplicationSettings)store.Load();

        // Assert
        settings.RecentMaps.Should().HaveCount(20);
        settings.MainWindowRestoreBounds.Should().Be(new WindowBounds(440, 256, 1407, 855));
        File.Exists(test.Directories.PreferencesFile).Should().BeTrue();
        File.ReadAllText(test.Directories.ConfigurationFile).Should().Be(legacyJson);

        using var document = JsonDocument.Parse(
            File.ReadAllText(test.Directories.PreferencesFile));
        var root = document.RootElement;
        root.GetProperty("$schema").GetString().Should().Be("mapping-tools.settings");
        root.GetProperty("$version").GetInt32().Should().Be(1);
        root.GetProperty("MainWindowRestoreBounds").GetProperty("X").GetDouble().Should().Be(440);
        root.GetProperty("QuickRunHotkey").GetProperty("Key").ValueKind.Should().Be(JsonValueKind.Number);
        root.GetProperty("PeriodicBackupInterval").GetString().Should().Be("00:10:00");
        var firstRecent = root.GetProperty("RecentMaps")[0];
        firstRecent.ValueKind.Should().Be(JsonValueKind.Object);
        firstRecent.GetProperty("Path").GetString().Should().Be(settings.RecentMaps[0].Path);
        firstRecent.GetProperty("DisplayDate").GetString().Should().Be(settings.RecentMaps[0].DisplayDate);
        File.Exists(test.Directories.ConfigurationFile + ".bak").Should().BeFalse();
    }

    [TestMethod]
    public void Load_WithCorruptDocument_ThrowsJsonException()
    {
        // Arrange
        using var test = TestDirectory.FromFixture("corrupt.json");
        JsonSettingsStore store = new(test.Directories);

        // Act
        Action act1 = () => store.Load();

        // Assert
        act1.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void Load_WithFutureVersion_ThrowsWithoutWritingLegacyConfiguration()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        test.Directories.EnsureCreated();
        File.WriteAllText(
            test.Directories.PreferencesFile,
            "{\"$schema\":\"mapping-tools.settings\",\"$version\":99}");
        JsonSettingsStore store = new(test.Directories);

        // Act
        Action act = () => store.Load();

        // Assert
        act.Should().Throw<JsonException>();
        File.ReadAllText(test.Directories.PreferencesFile)
            .Should().Be("{\"$schema\":\"mapping-tools.settings\",\"$version\":99}");
        File.Exists(test.Directories.ConfigurationFile).Should().BeFalse();
    }

    [TestMethod]
    public void LoadOrCreate_WithoutFile_PersistsDefaultsBeforeMachinePaths()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        FakeSettingsPathEnvironment environment = new();
        SettingsPathService paths = new(test.Directories, environment);
        JsonSettingsStore store = new(test.Directories);
        SettingsService service = new(store, paths);

        // Act
        var result = service.LoadOrCreate();

        // Assert
        result.WasCreated.Should().BeTrue();
        result.UsedFallbackOsuPath.Should().BeTrue();
        result.Settings.OsuPath.Should().Be(Path.Combine(test.Directories.LocalApplicationData, "osu!"));
        result.Settings.BackupsPath.Should().Be(Path.Combine(test.Directories.ApplicationData, "Backups"));
        result.Settings.SongsPath.Should().Be(Path.Combine(result.Settings.OsuPath, "Custom Songs"));
        environment.CreatedDirectories.Contains(result.Settings.BackupsPath).Should().BeTrue();

        var persistedDefaults = store.Load();
        persistedDefaults.OsuPath.Should().Be("");
        persistedDefaults.BackupsPath.Should().Be("");
    }

    private sealed class FakeSettingsPathEnvironment : ISettingsPathEnvironment
    {
        public HashSet<string> CreatedDirectories { get; } = [];
        public string UserName => "FixtureUser";

        public string? FindOsuInstallation()
        {
            return null;
        }

        public string GetBeatmapDirectory(string configPath)
        {
            return "Custom Songs";
        }

        public void EnsureDirectoryExists(string path)
        {
            CreatedDirectories.Add(path);
            Directory.CreateDirectory(path);
        }
    }

    public sealed class TestApplicationSettings : ApplicationSettings
    {
        public List<string> FavoriteTools { get; set; } = [];

        public WindowBounds? MainWindowRestoreBounds { get; set; }

        public bool MainWindowMaximized { get; set; }

        public bool AlwaysQuickRun { get; set; }

        public HotkeySettings? QuickRunHotkey { get; set; }

        public HotkeySettings? BetterSaveHotkey { get; set; }

        public bool OverrideOsuSave { get; set; }

        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Dark;

        public HotkeySettings? QuickUndoHotkey { get; set; }
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "MappingToolsSettingsTests",
                Guid.NewGuid().ToString("N"));
            Directories = new ApplicationDirectories(Root);
        }

        public string Root { get; }

        public ApplicationDirectories Directories { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        public static TestDirectory Empty()
        {
            return new TestDirectory();
        }

        public static TestDirectory FromFixture(string fixtureName)
        {
            TestDirectory test = new();
            test.Directories.EnsureCreated();
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "Settings", fixtureName),
                test.Directories.ConfigurationFile);
            return test;
        }
    }
}
