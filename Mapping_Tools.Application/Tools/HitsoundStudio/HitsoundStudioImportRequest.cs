using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Describes one import operation requested by the Hitsound Studio dialog.</summary>
public sealed record HitsoundStudioImportRequest
{
    /// <summary>Gets or sets the import kind.</summary>
    public ImportType ImportType { get; init; }

    /// <summary>Gets or sets the layer name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets or sets the layer sample family.</summary>
    public SampleSet SampleSet { get; init; } = SampleSet.Normal;

    /// <summary>Gets or sets the layer hitsound.</summary>
    public Hitsound Hitsound { get; init; } = Hitsound.Normal;

    /// <summary>Gets or sets a direct sample path.</summary>
    public string SamplePath { get; init; } = string.Empty;

    /// <summary>Gets or sets one or more source beatmap/MIDI paths.</summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>Gets or sets the stack X filter, or -1 for wildcard.</summary>
    public double X { get; init; } = -1;

    /// <summary>Gets or sets the stack Y filter, or -1 for wildcard.</summary>
    public double Y { get; init; } = -1;

    /// <summary>Gets or sets the MIDI time offset.</summary>
    public double Offset { get; init; }

    /// <summary>Gets or sets whether beatmap volumes make distinct layers.</summary>
    public bool DiscriminateVolumes { get; init; }

    /// <summary>Gets or sets whether identical source files are collapsed.</summary>
    public bool DetectDuplicateSamples { get; init; }

    /// <summary>Gets or sets whether duplicate events are removed.</summary>
    public bool RemoveDuplicates { get; init; }

    /// <summary>Gets or sets whether storyboard sound events are included.</summary>
    public bool IncludeStoryboard { get; init; }

    /// <summary>Gets or sets whether MIDI instruments are part of layer identity.</summary>
    public bool DiscriminateInstruments { get; init; } = true;

    /// <summary>Gets or sets whether MIDI keys are part of layer identity.</summary>
    public bool DiscriminateKeys { get; init; } = true;

    /// <summary>Gets or sets whether MIDI lengths are part of layer identity.</summary>
    public bool DiscriminateLengths { get; init; }

    /// <summary>Gets or sets MIDI length rounding roughness.</summary>
    public double LengthRoughness { get; init; } = 2;

    /// <summary>Gets or sets whether MIDI velocities are part of layer identity.</summary>
    public bool DiscriminateVelocities { get; init; }

    /// <summary>Gets or sets MIDI velocity rounding roughness.</summary>
    public double VelocityRoughness { get; init; } = 10;
}

