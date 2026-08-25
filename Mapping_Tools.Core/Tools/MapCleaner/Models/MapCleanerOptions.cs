using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;

namespace Mapping_Tools.Core.Tools.MapCleaner.Models;

/// <summary>Defines the framework-independent cleanup operations performed by Map Cleaner.</summary>
public class MapCleanerOptions
{
    /// <summary>Gets or sets whether slider volume changes must be preserved.</summary>
    public bool VolumeSliders { get; set; } = true;

    /// <summary>Gets or sets whether slider sample-set changes must be preserved.</summary>
    public bool SampleSetSliders { get; set; } = true;

    /// <summary>Gets or sets whether spinner volume changes must be preserved.</summary>
    public bool VolumeSpinners { get; set; } = true;

    /// <summary>Gets or sets whether hit objects and slider ends are resnapped.</summary>
    public bool ResnapObjects { get; set; } = true;

    /// <summary>Gets or sets whether editor bookmarks are resnapped.</summary>
    public bool ResnapBookmarks { get; set; }

    /// <summary>Gets or sets whether mapset samples are inspected before rebuilding timing points.</summary>
    public bool AnalyzeSamples { get; set; } = true;

    /// <summary>Gets or sets whether every object hitsound is cleared.</summary>
    public bool RemoveHitsounds { get; set; }

    /// <summary>Gets or sets whether muting volume values are removed from object ends.</summary>
    public bool RemoveMuting { get; set; }

    /// <summary>Gets or sets whether unclickable slider and spinner ends are muted.</summary>
    public bool RemoveUnclickableHitsounds { get; set; }

    /// <summary>Gets or sets the beat divisors accepted while resnapping.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } = RationalBeatDivisor.GetDefaultBeatDivisors();
}
