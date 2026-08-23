using Mapping_Tools.Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>Identifies the source objects used by Hitsound Copier.</summary>
public enum HitsoundCopierSelectionMode
{
    /// <summary>Uses objects selected in the live osu! editor.</summary>
    Selected,

    /// <summary>Uses objects covered by beatmap bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects returned by the legacy osu! time-code query.</summary>
    Time,

    /// <summary>Uses every object in the source beatmap.</summary>
    Everything,
}

/// <summary>Stores the source, target, matching, and hitsound-copy settings.</summary>
public class HitsoundCopierOptions
{
    /// <summary>Gets or sets the optional source beatmap path.</summary>
    public string PathFrom { get; set; } = string.Empty;

    /// <summary>Gets or sets vertical-bar-separated target beatmap paths.</summary>
    public string PathTo { get; set; } = string.Empty;

    /// <summary>Gets or sets zero for overwrite-all or one for defined-only mode.</summary>
    public int CopyMode { get; set; }

    /// <summary>Gets or sets the maximum rounded millisecond matching distance.</summary>
    public double TemporalLeniency { get; set; } = 5;

    /// <summary>Gets or sets the source object selection mode.</summary>
    public HitsoundCopierSelectionMode SourceSelectionMode { get; set; } =
        HitsoundCopierSelectionMode.Everything;

    /// <summary>Gets or sets the legacy time-code query used by Time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the millisecond shift applied to source events before matching.</summary>
    public double TimingOffset { get; set; }

    /// <summary>Gets or sets whether object and edge hitsounds are copied.</summary>
    public bool CopyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether slider-body timing hitsounds are copied.</summary>
    public bool CopyBodyHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether sample-set and custom-index timing values are copied.</summary>
    public bool CopySampleSets { get; set; } = true;

    /// <summary>Gets or sets whether timing-point volume values are copied.</summary>
    public bool CopyVolumes { get; set; } = true;

    /// <summary>Gets or sets whether target five-percent inherited volume is protected.</summary>
    public bool AlwaysPreserve5Volume { get; set; } = true;

    /// <summary>Gets or sets whether storyboard sound samples are copied.</summary>
    public bool CopyStoryboardedSamples { get; set; }

    /// <summary>Gets or sets whether samples already satisfied by hitsounds are skipped.</summary>
    public bool IgnoreHitsoundSatisfiedSamples { get; set; } = true;

    /// <summary>Gets or sets whether any hitsound suppresses a storyboard sample.</summary>
    public bool IgnoreWheneverHitsound { get; set; }

    /// <summary>Gets or sets whether unmatched source hitsounds may target slider ticks.</summary>
    public bool CopyToSliderTicks { get; set; }

    /// <summary>Gets or sets whether unmatched source hitsounds may target slider slides.</summary>
    public bool CopyToSliderSlides { get; set; }

    /// <summary>Gets or sets the first generated custom sample index.</summary>
    public int StartIndex { get; set; } = 100;

    /// <summary>Gets or sets whether eligible target slider ends are muted.</summary>
    public bool MuteSliderends { get; set; }

    /// <summary>Gets or sets the ordered beat divisors accepted by the muting filter.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } =
    [
        new RationalBeatDivisor(1), new RationalBeatDivisor(4), new RationalBeatDivisor(3),
        new RationalBeatDivisor(8), new RationalBeatDivisor(6), new RationalBeatDivisor(16),
        new RationalBeatDivisor(12),
    ];

    /// <summary>Gets or sets beat divisors classified as muted by the filter.</summary>
    public IBeatDivisor[] MutedDivisors { get; set; } =
    [
        new RationalBeatDivisor(4), new RationalBeatDivisor(3), new RationalBeatDivisor(8),
        new RationalBeatDivisor(6), new RationalBeatDivisor(16), new RationalBeatDivisor(12),
    ];

    /// <summary>Gets or sets the minimum slider duration in beats eligible for muting.</summary>
    public double MinLength { get; set; } = 0.5;

    /// <summary>Gets or sets the optional custom index used for muted ends.</summary>
    public int MutedIndex { get; set; } = -1;

    /// <summary>Gets or sets the sample family used for muted ends.</summary>
    public SampleSet MutedSampleSet { get; set; } = SampleSet.None;
}
