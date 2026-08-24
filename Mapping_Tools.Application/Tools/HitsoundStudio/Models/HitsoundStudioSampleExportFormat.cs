namespace Mapping_Tools.Application.Tools.HitsoundStudio.Models;

/// <summary>Chooses the encoding used for one generated sample family.</summary>
public enum HitsoundStudioSampleExportFormat
{
    /// <summary>Copy compatible sources or fall back to floating-point WAV.</summary>
    Default,

    /// <summary>32-bit floating-point WAV.</summary>
    WaveIeeeFloat,

    /// <summary>16-bit PCM WAV.</summary>
    WavePcm,

    /// <summary>Ogg Vorbis.</summary>
    OggVorbis,

    /// <summary>A single-chord MIDI file.</summary>
    MidiChords,
}

