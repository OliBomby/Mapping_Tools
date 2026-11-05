using Mapping_Tools.Application.Tools.SliderMerger;

namespace Mapping_Tools.Cli.Commands;

/// <summary>Names of CLI metadata and shared settings stored in project documents.</summary>
internal static class CliDocumentKeys
{
    internal const string PREFIX = "Cli";
    internal const string TARGETS = PREFIX + "Targets";
    internal const string SELECTION = PREFIX + nameof(CliOptions.Selection);
    internal const string TIME_CODE = PREFIX + nameof(CliOptions.TimeCode);
    internal const string LIBRARY = PREFIX + nameof(CliOptions.Library);
    internal const string PATTERNS = PREFIX + nameof(CliOptions.Pattern) + "s";
    internal const string ALL_PATTERNS = PREFIX + nameof(CliOptions.AllPatterns);
    internal const string SOURCE_OVERRIDES = PREFIX + "SourceOverrides";
    internal const string TRACK_CURRENT = PREFIX + "TrackCurrent";

    internal const string IMPORT_MODE_SETTING = nameof(SliderMergerServiceOptions.ImportModeSetting);
    internal const string TIME_CODE_SETTING = nameof(SliderMergerServiceOptions.TimeCode);

    // These fields belong to Desktop previews and imported editor state, not CLI settings.
    internal const string LOADED_HIT_OBJECTS = "LoadedHitObjects";
    internal const string PREVIEW_HIT_OBJECT = "PreviewHitObject";
}
