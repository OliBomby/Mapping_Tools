namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Identifies the supported file encodings for generated samples.</summary>
public enum AudioExportFormat
{
    /// <summary>32-bit IEEE floating-point WAV.</summary>
    WaveIeeeFloat,

    /// <summary>16-bit PCM WAV.</summary>
    WavePcm,

    /// <summary>Ogg Vorbis.</summary>
    OggVorbis,
}

