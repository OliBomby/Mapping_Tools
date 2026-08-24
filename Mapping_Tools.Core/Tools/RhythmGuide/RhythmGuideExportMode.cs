using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.RhythmGuide;

/// <summary>Determines whether a guide is created from a source map or appended to a target.</summary>
public enum RhythmGuideExportMode
{
    /// <summary>Creates a new beatmap containing the generated guide objects.</summary>
    NewMap,

    /// <summary>Adds generated guide objects to an existing target beatmap.</summary>
    AddToMap,
}

