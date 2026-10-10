using System.Text.Json;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Migration;
using Mapping_Tools.Infrastructure.Settings;
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
    public void Load_WithLegacySettings_DoesNotChangeConfiguration()
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
        File.ReadAllText(test.Directories.ConfigurationFile).Should().Be(legacyJson);
        File.Exists(test.Directories.ConfigurationFile + ".bak").Should().BeFalse();
    }

    [TestMethod]
    public void Load_WithVersionedConfiguration_RewritesCanonicalConfiguration()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        test.Directories.EnsureCreated();
        File.WriteAllText(
            test.Directories.ConfigurationFile,
            "{\"$schema\":\"mapping-tools.settings\",\"$version\":1}");
        JsonSettingsStore store = new(test.Directories);

        // Act
        _ = store.Load();

        // Assert
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(test.Directories.ConfigurationFile));
        document.RootElement.GetProperty("$version").GetInt32().Should().Be(2);
    }

    [TestMethod]
    public void Exists_WithPreferencesFileOnly_ReturnsFalse()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        test.Directories.EnsureCreated();
        File.WriteAllText(
            Path.Combine(test.Directories.ApplicationData, "preferences.json"),
            "{\"$schema\":\"mapping-tools.settings\",\"$version\":1}");
        JsonSettingsStore store = new(test.Directories);

        // Act
        bool exists = store.Exists;

        // Assert
        exists.Should().BeFalse();
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
    public void Load_WithFutureVersion_ThrowsWithoutChangingConfiguration()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        test.Directories.EnsureCreated();
        File.WriteAllText(
            test.Directories.ConfigurationFile,
            "{\"$schema\":\"mapping-tools.settings\",\"$version\":99}");
        JsonSettingsStore store = new(test.Directories);

        // Act
        Action act = () => store.Load();

        // Assert
        act.Should().Throw<JsonException>();
        File.ReadAllText(test.Directories.ConfigurationFile)
            .Should().Be("{\"$schema\":\"mapping-tools.settings\",\"$version\":99}");
    }

    [TestMethod]
    public void LoadOrCreate_WhenLegacyConfigurationExists_MigratesBeforeCreatingCurrentSettings()
    {
        // Arrange
        using var test = TestDirectory.Empty();
        ApplicationDirectories directories = new(
            Path.Combine(test.Root, "Roaming"),
            legacyApplicationDataRoot: Path.Combine(test.Root, "Local"));
        directories.EnsureCreated();
        Directory.CreateDirectory(directories.LegacyApplicationData);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Settings", "legacy-config.json"),
            Path.Combine(directories.LegacyApplicationData, "config.json"));
        JsonSettingsStore store = new(directories, typeof(TestApplicationSettings));
        SettingsPathService paths = new(directories, new FakeSettingsPathEnvironment());
        ApplicationDataMigrationService migrationService = new(directories);
        SettingsService service = new(
            store,
            paths,
            static () => new TestApplicationSettings(),
            migrationService);

        // Act
        SettingsLoadResult result = service.LoadOrCreate();

        // Assert
        result.WasCreated.Should().BeFalse();
        ((TestApplicationSettings)result.Settings).FavoriteTools.Should().HaveCount(7);
        result.Settings.RecentMaps.Should().HaveCount(20);
        migrationService.LastMigrationResult.Should().NotBeNull();
        File.Exists(directories.ConfigurationFile).Should().BeTrue();
        File.Exists(Path.Combine(directories.LegacyApplicationData, "config.json")).Should().BeTrue();
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
        File.Exists(test.Directories.ConfigurationFile).Should().BeTrue();

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
