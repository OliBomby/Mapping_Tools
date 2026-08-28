using Mapping_Tools.Core.Audio.Effects;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Describes generation transforms applied to a hitsound sample.</summary>
public sealed class AudioGenerationRequest
{
    /// <summary>Creates a generation request and copies the mutable sample specification.</summary>
    /// <param name="sample">The source and SoundFont selection arguments.</param>
    /// <param name="effects">Optional effect instances applied after generation.</param>
    public AudioGenerationRequest(SampleGeneratingArgs sample, IEnumerable<AudioEffect>? effects = null)
    {
        ArgumentNullException.ThrowIfNull(sample);
        Sample = sample.Copy();
        Effects = (effects ?? []).ToArray();
    }

    /// <summary>Gets an independent copy of the sample-generation arguments.</summary>
    public SampleGeneratingArgs Sample { get; }

    /// <summary>Gets the ordered, framework-neutral effects.</summary>
    public IReadOnlyList<AudioEffect> Effects { get; }
}
