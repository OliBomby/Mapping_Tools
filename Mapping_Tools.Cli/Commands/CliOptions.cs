using System.CommandLine;
using System.CommandLine.Parsing;

namespace Mapping_Tools.Cli.Commands;

/// <summary>A typed identifier for one CLI option.</summary>
/// <typeparam name="T">The value type registered with System.CommandLine.</typeparam>
internal readonly record struct CliOption<T>(string Name)
{
    /// <summary>Reads this option from a successful parse.</summary>
    /// <param name="result">System.CommandLine's parse result.</param>
    /// <returns>The parsed value, or the option's default when it was omitted.</returns>
    internal T? GetValue(ParseResult result) =>
        GetResult(result) is { } option ? option.GetValueOrDefault<T>() : default;

    /// <summary>Checks whether this option was explicitly supplied.</summary>
    /// <param name="result">System.CommandLine's parse result.</param>
    /// <returns>True when the user entered this option.</returns>
    internal bool WasSpecified(ParseResult result) =>
        GetResult(result) is { Implicit: false };

    /// <summary>Finds this option's parse result on the selected command or its ancestors.</summary>
    /// <param name="result">System.CommandLine's parse result.</param>
    /// <returns>The matching option result, or null when the option is absent from this invocation.</returns>
    internal OptionResult? GetResult(ParseResult result)
    {
        string spelling = CliOptionSyntax.LONG_OPTION_PREFIX + Name;

        for (var command = result.CommandResult; command is not null; command = command.Parent as CommandResult)
            foreach (var option in command.Children.OfType<OptionResult>())
                if (option.Option.Name == spelling || option.Option.Aliases.Contains(spelling))
                    return option;

        return null;
    }
}

/// <summary>Typed names for host and CLI-only options consumed by command handlers.</summary>
internal static class CliOptions
{
    internal const string GRAPH_ALIAS = "graph";
    internal const string LAYER_ALIAS = "layer";
    internal const string MAPSET_ALIAS = "mapset";
    internal const string TO_ALIAS = "to";

    internal static readonly CliOption<string[]> Path = new("path");
    internal static readonly CliOption<bool> Current = new("current");
    internal static readonly CliOption<string> Project = new("project");
    internal static readonly CliOption<string> SaveProject = new("save-project");
    internal static readonly CliOption<bool> NoRun = new("no-run");
    internal static readonly CliOption<bool> Overwrite = new("overwrite");
    internal static readonly CliOption<bool> NoBackups = new("no-backups");
    internal static readonly CliOption<string> BackupPath = new("backup-path");
    internal static readonly CliOption<string> OsuPath = new("osu-path");
    internal static readonly CliOption<string> SongsPath = new("songs-path");
    internal static readonly CliOption<string> OsuConfig = new("osu-config");
    internal static readonly CliOption<bool> ReloadEditor = new("reload-editor");
    internal static readonly CliOption<string> EditorReader = new("editor-reader");
    internal static readonly CliOption<string> CurrentBeatmapFetching = new("current-beatmap-fetching");
    internal static readonly CliOption<string> EditorReloadMode = new("editor-reload-mode");
    internal static readonly CliOption<string> MaxBackups = new("max-backups");
    internal static readonly CliOption<bool> MakeBackups = new("make-backups");
    internal static readonly CliOption<bool> AutoDetectLazerExternalEdit = new("auto-detect-lazer-external-edit");
    internal static readonly CliOption<string> Config = new(ConfigCommand.COMMAND);
    internal static readonly CliOption<bool> NoConfig = new("no-config");
    internal static readonly CliOption<string> From = new("from");
    internal static readonly CliOption<bool> FromCurrent = new("from-current");
    internal static readonly CliOption<string> Selection = new("selection");
    internal static readonly CliOption<string> TimeCode = new("time-code");
    internal static readonly CliOption<string> Library = new("library");
    internal static readonly CliOption<string> Fix = new("fix");
    internal static readonly CliOption<bool> Interactive = new("interactive");
    internal static readonly CliOption<bool> FailOnAutoFail = new("fail-on-auto-fail");
    internal static readonly CliOption<string> ImportColourHax = new("import-colour-hax");
    internal static readonly CliOption<string[]> Import = new("import");
    internal static readonly CliOption<bool> CleanOutput = new("clean-output");
    internal static readonly CliOption<bool> Reload = new("reload");
    internal static readonly CliOption<bool> UseSelectedSlider = new("use-selected-slider");
    internal static readonly CliOption<string> Collection = new("collection");
    internal static readonly CliOption<bool> Merge = new("merge");
    internal static readonly CliOption<string> Name = new("name");
    internal static readonly CliOption<string> FolderName = new("folder-name");
    internal static readonly CliOption<string> Source = new("source");
    internal static readonly CliOption<string> Output = new("output");
    internal static readonly CliOption<string[]> Pattern = new("pattern");
    internal static readonly CliOption<bool> AllPatterns = new("all-patterns");
    internal static readonly CliOption<string> ObjectCode = new("object-code");
    internal static readonly CliOption<string> TimingCode = new("timing-code");
    internal static readonly CliOption<string> GlobalSv = new("global-sv");
    internal static readonly CliOption<string> GameMode = new("game-mode");
    internal static readonly CliOption<string> StartTime = new("start-time");
    internal static readonly CliOption<string> EndTime = new("end-time");
    internal static readonly CliOption<string> Group = new("group");
}
