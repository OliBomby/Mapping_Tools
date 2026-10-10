using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.RhythmGuide.Models;

/// <summary>Contains the framework-neutral transformation options persisted by Rhythm Guide projects.</summary>
public class RhythmGuideEngineOptions
{
    /// <summary>Gets or sets the game mode assigned to newly generated beatmaps.</summary>
    public GameMode OutputGameMode { get; set; } = GameMode.Standard;

    /// <summary>Gets or sets the difficulty name assigned to a new guide beatmap.</summary>
    public string OutputName { get; set; } = "Hitsounds";

    /// <summary>Gets or sets whether every generated object uses night-core timing.</summary>
    public bool NcEverything { get; set; }

    /// <summary>Gets or sets which source rhythm events become guide objects.</summary>
    public RhythmGuideSelectionMode SelectionMode { get; set; } =
        RhythmGuideSelectionMode.HitsoundEvents;

    /// <summary>Gets or sets the snapping divisors used while expanding source rhythms.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
        [new RationalBeatDivisor(16), new RationalBeatDivisor(12)];
}
