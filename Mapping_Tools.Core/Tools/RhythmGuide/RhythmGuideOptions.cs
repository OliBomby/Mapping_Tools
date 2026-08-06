using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.RhythmGuide;

/// <summary>Determines whether a guide is created from a source map or appended to a target.</summary>
public enum RhythmGuideExportMode
{
    NewMap,
    AddToMap
}

/// <summary>Determines which expanded timeline events become guide objects.</summary>
public enum RhythmGuideSelectionMode
{
    AllEvents,
    HitsoundEvents,
    AllEventSeparated,
    LongNotes
}

/// <summary>Contains the framework-neutral transformation options persisted by Rhythm Guide projects.</summary>
public sealed class RhythmGuideOptions
{
    public string[] Paths { get; set; } = [];

    public GameMode OutputGameMode { get; set; } = GameMode.Standard;

    public string OutputName { get; set; } = "Hitsounds";

    public bool NcEverything { get; set; }

    public RhythmGuideSelectionMode SelectionMode { get; set; } =
        RhythmGuideSelectionMode.HitsoundEvents;

    public RhythmGuideExportMode ExportMode { get; set; } = RhythmGuideExportMode.NewMap;

    public string ExportPath { get; set; } = string.Empty;

    public IBeatDivisor[] BeatDivisors { get; set; } =
        [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
}
