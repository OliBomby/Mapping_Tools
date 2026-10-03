using Mapping_Tools.Application.Localization;
namespace Mapping_Tools.Application.Platform.FilePicker;

/// <summary>
///     Provides immutable native-file-picker filters shared by application workflows.
/// </summary>
public static class CommonFilePickerFilters
{
    /// <summary>
    ///     Matches osu! beatmap files.
    /// </summary>
    public static FilePickerFilter Beatmaps => new(
        ApplicationStrings.Picker_Beatmaps,
        ["*.osu"],
        ["application/x-osu-beatmap"]);

    /// <summary>
    ///     Matches osu! beatmap and storyboard files.
    /// </summary>
    public static FilePickerFilter BeatmapsAndStoryboards => new(
        ApplicationStrings.Picker_BeatmapsAndStoryboards,
        ["*.osu", "*.osb"],
        ["application/x-osu-beatmap", "text/plain"],
        ["public.data", "public.text"]);

    /// <summary>
    ///     Matches osu! beatmap and storyboard backups.
    /// </summary>
    public static FilePickerFilter BeatmapBackups => new(
        ApplicationStrings.Picker_BeatmapBackups,
        ["*.osu", "*.osb"],
        ["application/x-osu-beatmap", "text/plain"],
        ["public.data", "public.text"]);

    /// <summary>
    ///     Matches Mapping Tools project files.
    /// </summary>
    public static FilePickerFilter MappingToolsProjects => new(
        ApplicationStrings.Picker_MappingToolsProjects,
        ["*.json"],
        ["application/json"],
        ["public.json"]);

    /// <summary>
    ///     Matches osu! user configuration files.
    /// </summary>
    public static FilePickerFilter OsuConfiguration => new(
        ApplicationStrings.Picker_OsuConfiguration,
        ["osu!.*.cfg"]);

    /// <summary>
    ///     Matches audio and SoundFont sample files.
    /// </summary>
    public static FilePickerFilter SampleFiles => new(
        ApplicationStrings.Picker_SampleFiles,
        ["*.wav", "*.ogg", "*.sf2"]);
}
