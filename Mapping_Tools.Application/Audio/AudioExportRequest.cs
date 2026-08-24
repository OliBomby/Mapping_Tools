using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Describes one file export request.</summary>
public sealed class AudioExportRequest
{
    /// <summary>Creates an export request.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="format">The target encoding.</param>
    /// <param name="quality">Vorbis quality, ignored for WAV formats.</param>
    public AudioExportRequest(string path, AudioExportFormat format, float quality = 0.5f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!float.IsFinite(quality) || quality is < -0.1f or > 1f) throw new ArgumentOutOfRangeException(nameof(quality));

        Path = path;
        Format = format;
        Quality = quality;
    }

    /// <summary>Gets the destination file path.</summary>
    public string Path { get; }

    /// <summary>Gets the requested encoding.</summary>
    public AudioExportFormat Format { get; }

    /// <summary>Gets the Vorbis quality value.</summary>
    public float Quality { get; }
}

