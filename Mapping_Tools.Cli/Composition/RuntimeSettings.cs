using System.Globalization;
using System.CommandLine;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Composition;

/// <summary>Creates per-invocation Application preferences from CLI values instead of loading Desktop preferences.</summary>
internal static class RuntimeSettings
{
    /// <summary>Translates effective CLI defaults and flags into Application adapter and backup settings.</summary>
    /// <param name="parseResult">System.CommandLine's successful parse.</param>
    /// <param name="configurationDefaults">Validated saved global preferences.</param>
    /// <returns>Fresh settings with periodic backups disabled.</returns>
    /// <remarks>Reloads remain disabled unless reload-editor was explicitly requested, even when a reload adapter was saved.</remarks>
    internal static ApplicationSettings Create(ParseResult parseResult, JObject configurationDefaults)
    {
        ApplicationSettings settings = new()
        {
            MakeBackups = CliOptions.MakeBackups.WasSpecified(parseResult)
                ? CliOptions.MakeBackups.GetValue(parseResult)
                : CliOptions.NoBackups.WasSpecified(parseResult)
                    ? !CliOptions.NoBackups.GetValue(parseResult)
                    : configurationDefaults[CliOptions.MakeBackups.Name]?.Value<bool>() ?? true,
            AutoDetectLazerExternalEdit = GetBoolean(
                parseResult, configurationDefaults, CliOptions.AutoDetectLazerExternalEdit, true),
            MaxBackupFiles = GetInteger(parseResult, configurationDefaults, CliOptions.MaxBackups, 1000),
            MakePeriodicBackups = false,
            BackupsPath = GetString(parseResult, configurationDefaults, CliOptions.BackupPath) ?? "",
            OsuPath = GetString(parseResult, configurationDefaults, CliOptions.OsuPath) ?? "",
            SongsPath = GetString(parseResult, configurationDefaults, CliOptions.SongsPath) ?? "",
            OsuConfigPath = GetString(parseResult, configurationDefaults, CliOptions.OsuConfig) ?? "",
            EditorReload = GetEditorReloadMode(parseResult, configurationDefaults),
            BeatmapLiveStateReading = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
                ? BeatmapLiveStateReadingMode.EditorReader : BeatmapLiveStateReadingMode.Disabled,
        };

        if (GetString(parseResult, configurationDefaults, CliOptions.EditorReader) is { } reader)
            settings.BeatmapLiveStateReading = Parse<BeatmapLiveStateReadingMode>(reader);

        if (GetString(parseResult, configurationDefaults, CliOptions.CurrentBeatmapFetching) is { } locator)
            settings.CurrentBeatmapFetching = Parse<CurrentBeatmapFetchingMode>(locator);

        return settings;
    }

    private static EditorReloadMode GetEditorReloadMode(ParseResult parseResult, JObject configurationDefaults)
    {
        if (!CliOptions.ReloadEditor.GetValue(parseResult))
            return EditorReloadMode.Disabled;

        if (GetString(parseResult, configurationDefaults, CliOptions.EditorReloadMode) is { } mode)
            return Parse<EditorReloadMode>(mode);

        return OperatingSystem.IsWindows() ? EditorReloadMode.SimulatedKeypress : EditorReloadMode.Mtipc;
    }

    private static string? GetString(ParseResult parseResult, JObject defaults, CliOption<string> option) =>
        option.WasSpecified(parseResult)
            ? option.GetValue(parseResult)
            : defaults[option.Name]?.Value<string>();

    private static bool GetBoolean(ParseResult parseResult, JObject defaults, CliOption<bool> option, bool fallback) =>
        option.WasSpecified(parseResult)
            ? option.GetValue(parseResult)
            : defaults[option.Name]?.Value<bool>() ?? fallback;

    private static int GetInteger(ParseResult parseResult, JObject defaults, CliOption<string> option, int fallback) =>
        option.WasSpecified(parseResult)
            ? int.Parse(option.GetValue(parseResult)!, CultureInfo.InvariantCulture)
            : defaults[option.Name]?.Value<int>() ?? fallback;

    private static T Parse<T>(string text) where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
            if (Configuration.OptionSchema.Kebab(value.ToString()) == text
                || value.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
                return value;

        throw new ArgumentException(ApplicationText.Format(
            CliStrings.InvalidEnum,
            typeof(T).Name,
            string.Join(", ", Enum.GetNames<T>().Select(Configuration.OptionSchema.Kebab))));
    }
}
