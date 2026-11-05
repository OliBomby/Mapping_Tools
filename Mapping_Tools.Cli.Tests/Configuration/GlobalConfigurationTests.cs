using AwesomeAssertions;
using System.CommandLine;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Composition;
using Mapping_Tools.Cli.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Tests.Configuration;

[TestClass]
public sealed class GlobalConfigurationTests
{
    [TestMethod]
    [DataRow("make-backups", "true")]
    [DataRow("no-backups", "false")]
    public void Create_ExplicitRuntimeOptions_OverridesSavedDefaultsWithoutRequestingReload(string backupFlag, string value)
    {
        // Arrange
        JObject saved = new()
        {
            ["make-backups"] = false,
            ["editor-reader"] = "disabled",
            ["editor-reload-mode"] = "mtipc",
            ["auto-detect-lazer-external-edit"] = false,
        };
        var parseResult = Parse("--" + backupFlag + "=" + value, "--editor-reader", "mtipc");

        // Act
        var settings = RuntimeSettings.Create(parseResult, saved);

        // Assert
        settings.MakeBackups.Should().BeTrue();
        settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.Mtipc);
        settings.EditorReload.Should().Be(EditorReloadMode.Disabled);
        settings.AutoDetectLazerExternalEdit.Should().BeFalse();
        saved["make-backups"]!.Value<bool>().Should().BeFalse();
    }

    [TestMethod]
    public void Create_SavedRuntimeOptions_UsesSavedPreferencesWithoutRequestingReload()
    {
        // Arrange
        JObject saved = new()
        {
            ["make-backups"] = false,
            ["max-backups"] = 12,
            ["editor-reader"] = "mtipc",
            ["current-beatmap-fetching"] = "gosumemory",
            ["editor-reload-mode"] = "mtipc",
        };
        var parseResult = Parse();

        // Act
        var settings = RuntimeSettings.Create(parseResult, saved);

        // Assert
        settings.MakeBackups.Should().BeFalse();
        settings.MaxBackupFiles.Should().Be(12);
        settings.BeatmapLiveStateReading.Should().Be(BeatmapLiveStateReadingMode.Mtipc);
        settings.CurrentBeatmapFetching.Should().Be(CurrentBeatmapFetchingMode.Gosumemory);
        settings.EditorReload.Should().Be(EditorReloadMode.Disabled);
        settings.OsuPath.Should().BeEmpty();
        settings.SongsPath.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Load_ManuallyEditedRelativePaths_ResolvesAgainstTheConfigurationDirectory()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), "mapping-tools-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "cli.toml");
        await File.WriteAllTextAsync(path, "osu-path = 'osu'\nsongs-path = 'maps'\n");

        try
        {
            // Act
            var loaded = new GlobalConfiguration(path).Load();
            var commandPath = GlobalConfiguration.ParseValue("backup-path", "backups");

            // Assert
            loaded["osu-path"]!.Value<string>().Should().Be(Path.Combine(directory, "osu"));
            loaded["songs-path"]!.Value<string>().Should().Be(Path.Combine(directory, "maps"));
            commandPath.Value<string>().Should().Be(Path.GetFullPath("backups"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static ParseResult Parse(params string[] arguments)
    {
        RootCommand root = new();
        CommandOptions.Add(root, CommandOptions.Common);

        return root.Parse(arguments);
    }
}
