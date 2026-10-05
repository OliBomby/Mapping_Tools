using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Describes one SoundFont note-rendering request.</summary>
public sealed class SoundFontNoteRequest
{
    /// <summary>Creates a SoundFont request from the existing sample-generation model.</summary>
    /// <param name="sample">The SoundFont path and note selectors.</param>
    public SoundFontNoteRequest(SampleGeneratingArgs sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (!sample.UsesSoundFont) throw new ArgumentException(ApplicationStrings.SoundFontNoteRequest_SampleMustUseSf2File, nameof(sample));

        Sample = sample.Copy();
    }

    /// <summary>Gets an independent SoundFont sample specification.</summary>
    public SampleGeneratingArgs Sample { get; }
}
