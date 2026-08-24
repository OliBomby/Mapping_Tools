namespace Mapping_Tools.Application.Platform.FilePicker;

/// <summary>
///     Provides immutable native-file-picker filters shared by application workflows.
/// </summary>
public static class CommonFilePickerFilters
{
    /// <summary>
    ///     Matches osu! beatmap files.
    /// </summary>
    public static FilePickerFilter Beatmaps { get; } = new(
        "osu! beatmap",
        ["*.osu"],
        ["application/x-osu-beatmap"]);

    /// <summary>
    ///     Matches osu! beatmap and storyboard files.
    /// </summary>
    public static FilePickerFilter BeatmapsAndStoryboards { get; } = new(
        "osu! beatmaps and storyboards",
        ["*.osu", "*.osb"],
        ["application/x-osu-beatmap", "text/plain"],
        ["public.data", "public.text"]);

    /// <summary>
    ///     Matches osu! beatmap and storyboard backups.
    /// </summary>
    public static FilePickerFilter BeatmapBackups { get; } = new(
        "osu! beatmap backups",
        ["*.osu", "*.osb"],
        ["application/x-osu-beatmap", "text/plain"],
        ["public.data", "public.text"]);

    /// <summary>
    ///     Matches Mapping Tools project files.
    /// </summary>
    public static FilePickerFilter MappingToolsProjects { get; } = new(
        "Mapping Tools project",
        ["*.json"],
        ["application/json"],
        ["public.json"]);

    /// <summary>
    ///     Matches osu! user configuration files.
    /// </summary>
    public static FilePickerFilter OsuConfiguration { get; } = new(
        "osu! user configuration",
        ["osu!.*.cfg"]);
}
