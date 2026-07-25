using System.Text.Json;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    [TestMethod]
    public void LegacySettingsDocumentLoadsWithoutDataLoss()
    {
        using TestDirectory test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories);

        ApplicationSettings settings = store.Load();

        Assert.AreEqual(20, settings.RecentMaps.Count);
        StringAssert.EndsWith(
            settings.RecentMaps[0].Path,
            "[3  (2^n) - 2].osu");
        Assert.AreEqual("18/07/2026 17:38:50", settings.RecentMaps[0].DisplayDate);
        Assert.AreEqual(7, settings.FavoriteTools.Count);
        Assert.AreEqual(
            new WindowBounds(440, 256, 1407, 855),
            settings.MainWindowRestoreBounds);
        Assert.AreEqual(56, settings.QuickRunHotkey?.Key);
        Assert.AreEqual(1, settings.QuickRunHotkey?.Modifiers);
        Assert.AreEqual(62, settings.BetterSaveHotkey?.Key);
        Assert.AreEqual(6, settings.BetterSaveHotkey?.Modifiers);
        Assert.AreEqual(TimeSpan.FromMinutes(10), settings.PeriodicBackupInterval);
        Assert.AreEqual("1.12.1", settings.SkipVersion);
    }

    [TestMethod]
    public void LegacySettingsRoundTripPreservesJsonShapes()
    {
        using TestDirectory test = TestDirectory.FromFixture("legacy-config.json");
        JsonSettingsStore store = new(test.Directories);
        ApplicationSettings settings = store.Load();

        store.Save(settings);
        ApplicationSettings reloaded = store.Load();

        Assert.AreEqual(settings.MainWindowRestoreBounds, reloaded.MainWindowRestoreBounds);
        Assert.AreEqual(settings.QuickUndoHotkey, reloaded.QuickUndoHotkey);

        using JsonDocument document = JsonDocument.Parse(
            File.ReadAllText(test.Directories.ConfigurationFile));
        JsonElement root = document.RootElement;
        Assert.AreEqual(
            "440,256,1407,855",
            root.GetProperty("MainWindowRestoreBounds").GetString());
        Assert.AreEqual(
            JsonValueKind.Number,
            root.GetProperty("QuickRunHotkey").GetProperty("Key").ValueKind);
        Assert.AreEqual(
            "00:10:00",
            root.GetProperty("PeriodicBackupInterval").GetString());
        JsonElement firstRecent = root.GetProperty("RecentMaps")[0];
        Assert.AreEqual(JsonValueKind.Array, firstRecent.ValueKind);
        Assert.AreEqual(2, firstRecent.GetArrayLength());
        Assert.AreEqual(
            settings.RecentMaps[0].DisplayDate,
            firstRecent[1].GetString());
    }

    [TestMethod]
    public void CorruptSettingsDocumentIsRejected()
    {
        using TestDirectory test = TestDirectory.FromFixture("corrupt.json");
        JsonSettingsStore store = new(test.Directories);

        Assert.ThrowsException<JsonException>(() => store.Load());
    }

    [TestMethod]
    public void LoadOrCreatePersistsDefaultsBeforeApplyingMachinePaths()
    {
        using TestDirectory test = TestDirectory.Empty();
        FakeSettingsPathEnvironment environment = new();
        SettingsPathService paths = new(test.Directories, environment);
        JsonSettingsStore store = new(test.Directories);
        SettingsService service = new(store, paths);

        SettingsLoadResult result = service.LoadOrCreate();

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.UsedFallbackOsuPath);
        Assert.AreEqual(
            Path.Combine(test.Directories.LocalApplicationData, "osu!"),
            result.Settings.OsuPath);
        Assert.AreEqual(
            Path.Combine(test.Directories.ApplicationData, "Backups"),
            result.Settings.BackupsPath);
        Assert.AreEqual(
            Path.Combine(result.Settings.OsuPath, "Custom Songs"),
            result.Settings.SongsPath);
        Assert.IsTrue(environment.CreatedDirectories.Contains(result.Settings.BackupsPath));

        ApplicationSettings persistedDefaults = store.Load();
        Assert.AreEqual("", persistedDefaults.OsuPath);
        Assert.AreEqual("", persistedDefaults.BackupsPath);
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
