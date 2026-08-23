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

/// <summary>Determines which expanded timeline events become guide objects.</summary>
public enum RhythmGuideSelectionMode
{
    /// <summary>Includes every expanded timeline event.</summary>
    AllEvents,

    /// <summary>Includes only events that carry hitsounds.</summary>
    HitsoundEvents,

    /// <summary>Separates coincident expanded events into distinct guide objects.</summary>
    AllEventSeparated,

    /// <summary>Creates long-note guide objects where the source rhythm permits them.</summary>
    LongNotes,
}

/// <summary>Contains the framework-neutral transformation options persisted by Rhythm Guide projects.</summary>
public sealed class RhythmGuideOptions
{
    /// <summary>Gets or sets the source beatmap paths whose rhythm is copied.</summary>
    public string[] Paths { get; set; } = [];

    /// <summary>Gets or sets the game mode assigned to newly generated beatmaps.</summary>
    public GameMode OutputGameMode { get; set; } = GameMode.Standard;

    /// <summary>Gets or sets the difficulty name assigned to a new guide beatmap.</summary>
    public string OutputName { get; set; } = "Hitsounds";

    /// <summary>Gets or sets whether every generated object uses night-core timing.</summary>
    public bool NcEverything { get; set; }

    /// <summary>Gets or sets which source rhythm events become guide objects.</summary>
    public RhythmGuideSelectionMode SelectionMode { get; set; } =
        RhythmGuideSelectionMode.HitsoundEvents;

    /// <summary>Gets or sets whether generation creates a map or appends to a target.</summary>
    public RhythmGuideExportMode ExportMode { get; set; } = RhythmGuideExportMode.NewMap;

    /// <summary>Gets or sets the destination beatmap path.</summary>
    public string ExportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the snapping divisors used while expanding source rhythms.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
        [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
}
