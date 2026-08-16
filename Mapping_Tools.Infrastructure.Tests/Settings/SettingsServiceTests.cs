using System.Text.Json;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Infrastructure.Files;
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
        using TestDirectory test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories);

        // Act
        ApplicationSettings settings = store.Load();

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
    public void SaveAndLoad_LegacySettings_PreservesJsonShapes()
    {
        // Arrange
        using TestDirectory test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories);
        ApplicationSettings settings = store.Load();
        settings.Theme = ApplicationTheme.Light;

        // Act
        store.Save(settings);
        ApplicationSettings reloaded = store.Load();

        // Assert
        reloaded.MainWindowRestoreBounds.Should().Be(settings.MainWindowRestoreBounds);
        reloaded.QuickUndoHotkey.Should().Be(settings.QuickUndoHotkey);
        reloaded.Theme.Should().Be(ApplicationTheme.Light);

        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(test.Directories.ConfigurationFile));
        JsonElement root = document.RootElement;
        root.GetProperty("MainWindowRestoreBounds").GetString().Should().Be("440,256,1407,855");
        root.GetProperty("QuickRunHotkey").GetProperty("Key").ValueKind.Should().Be(JsonValueKind.Number);
        root.GetProperty("PeriodicBackupInterval").GetString().Should().Be("00:10:00");
        root.GetProperty("Theme").GetString().Should().Be("Light");
        JsonElement firstRecent = root.GetProperty("RecentMaps")[0];
        firstRecent.ValueKind.Should().Be(JsonValueKind.Array);
        firstRecent.GetArrayLength().Should().Be(2);
        firstRecent[1].GetString().Should().Be(settings.RecentMaps[0].DisplayDate);
    }

    [TestMethod]
    public void Load_WithCorruptDocument_ThrowsJsonException()
    {
        // Arrange
        using TestDirectory test = TestDirectory.FromFixture("corrupt.json");
        JsonSettingsStore store = new(test.Directories);

        // Act
        Action act1 = () => store.Load();

        // Assert
        act1.Should().Throw<JsonException>();
    }

    [TestMethod]
    public void LoadOrCreate_WithoutFile_PersistsDefaultsBeforeMachinePaths()
    {
        // Arrange
        using TestDirectory test = TestDirectory.Empty();
        FakeSettingsPathEnvironment environment = new();
        SettingsPathService paths = new(test.Directories, environment);
        JsonSettingsStore store = new(test.Directories);
        SettingsService service = new(store, paths);

        // Act
        SettingsLoadResult result = service.LoadOrCreate();

        // Assert
        result.WasCreated.Should().BeTrue();
        result.UsedFallbackOsuPath.Should().BeTrue();
        result.Settings.OsuPath.Should().Be(Path.Combine(test.Directories.LocalApplicationData, "osu!"));
        result.Settings.BackupsPath.Should().Be(Path.Combine(test.Directories.ApplicationData, "Backups"));
        result.Settings.SongsPath.Should().Be(Path.Combine(result.Settings.OsuPath, "Custom Songs"));
        environment.CreatedDirectories.Contains(result.Settings.BackupsPath).Should().BeTrue();

        ApplicationSettings persistedDefaults = store.Load();
        persistedDefaults.OsuPath.Should().Be("");
        persistedDefaults.BackupsPath.Should().Be("");
    }

    private sealed class FakeSettingsPathEnvironment : ISettingsPathEnvironment
    {
        public string UserName => "FixtureUser";

        public HashSet<string> CreatedDirectories { get; } = [];

        public string? FindOsuInstallation() => null;

        public string GetBeatmapDirectory(string configPath) => "Custom Songs";

        public void EnsureDirectoryExists(string path)
        {
            CreatedDirectories.Add(path);
            Directory.CreateDirectory(path);
        }
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

        public static TestDirectory Empty() => new();

        public static TestDirectory FromFixture(string fixtureName)
        {
            TestDirectory test = new();
            test.Directories.EnsureCreated();
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "Settings", fixtureName),
                test.Directories.ConfigurationFile);
            return test;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
