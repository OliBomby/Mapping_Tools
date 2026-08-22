using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Serializable Pattern Gallery state, including the legacy option names used
/// by existing <c>patterngalleryproject.json</c> and collection files.
/// </summary>
public sealed class PatternGalleryProject
{
    /// <summary>Gets or sets the user-visible collection name.</summary>
    public string CollectionName { get; set; } = "My Pattern Collection";

    /// <summary>Gets the indexed patterns in their persisted order.</summary>
    public List<PatternGalleryPattern> Patterns { get; set; } = [];

    /// <summary>Gets or sets the collection-folder metadata.</summary>
    public PatternGalleryCollectionMetadata FileHandler { get; set; } = new();

    /// <summary>Gets or sets the time reference used during export.</summary>
    public ExportTimeMode ExportTimeMode { get; set; } = ExportTimeMode.Current;

    /// <summary>Gets or sets the custom export time in milliseconds.</summary>
    public double CustomExportTime { get; set; }

    /// <summary>Gets or sets the extra deletion margin in milliseconds.</summary>
    public double Padding { get; set; } = 5;

    /// <summary>Gets or sets the minimum beat gap used for partitioning.</summary>
    public double PartingDistance { get; set; } = 4;

    /// <summary>Gets or sets the target-object overwrite mode.</summary>
    public PatternOverwriteMode PatternOverwriteMode { get; set; } = PatternOverwriteMode.PartitionedOverwrite;

    /// <summary>Gets or sets the timing overwrite mode.</summary>
    public TimingOverwriteMode TimingOverwriteMode { get; set; } = TimingOverwriteMode.OriginalTimingOnly;

    /// <summary>Gets or sets whether pattern hitsounds are included.</summary>
    public bool IncludeHitsounds { get; set; }

    /// <summary>Gets or sets whether pattern kiai state is included.</summary>
    public bool IncludeKiai { get; set; }

    /// <summary>Gets or sets whether spacing is scaled to the target Circle Size.</summary>
    public bool ScaleToNewCircleSize { get; set; }

    /// <summary>Gets or sets whether pattern timing is scaled to the target timing.</summary>
    public bool ScaleToNewTiming { get; set; } = true;

    /// <summary>Gets or sets whether objects are resnapped to target timing.</summary>
    public bool SnapToNewTiming { get; set; } = true;

    /// <summary>Gets or sets the beat divisors used by target-timing resnapping.</summary>
    public IBeatDivisor[] BeatDivisors { get; set; } = RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets or sets whether global slider velocity is compensated.</summary>
    public bool FixGlobalSv { get; set; } = true;

    /// <summary>Gets or sets whether BPM-dependent slider velocity is compensated.</summary>
    public bool FixBpmSv { get; set; }

    /// <summary>Gets or sets whether combo colour skips are repaired.</summary>
    public bool FixColourHax { get; set; } = true;

    /// <summary>Gets or sets whether osu! stacks are converted to manual positions.</summary>
    public bool FixStackLeniency { get; set; }

    /// <summary>Gets or sets whether slider tick rate is compensated.</summary>
    public bool FixTickRate { get; set; }

    /// <summary>Gets or sets the optional spatial scale multiplier.</summary>
    public double CustomScale { get; set; } = 1;

    /// <summary>Gets or sets the clockwise spatial rotation in degrees.</summary>
    public double CustomRotate { get; set; }

    /// <summary>Creates the mutable Core placement helper from persisted options.</summary>
    /// <returns>A placement helper containing an independent option snapshot.</returns>
    public PatternGalleryPlacer CreatePlacer() => new()
    {
        Padding = Padding,
        PartingDistance = PartingDistance,
        PatternOverwriteMode = PatternOverwriteMode,
        TimingOverwriteMode = TimingOverwriteMode,
        IncludeHitsounds = IncludeHitsounds,
        IncludeKiai = IncludeKiai,
        ScaleToNewCircleSize = ScaleToNewCircleSize,
        ScaleToNewTiming = ScaleToNewTiming,
        SnapToNewTiming = SnapToNewTiming,
        BeatDivisors = BeatDivisors?.ToArray() ?? [],
        FixGlobalSv = FixGlobalSv,
        FixBpmSv = FixBpmSv,
        FixColourHax = FixColourHax,
        FixStackLeniency = FixStackLeniency,
        FixTickRate = FixTickRate,
        CustomScale = CustomScale,
        CustomRotate = MathHelper.DegreesToRadians(CustomRotate)
    };
}
