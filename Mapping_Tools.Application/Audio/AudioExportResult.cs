using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Describes the result of a completed audio export.</summary>
public sealed class AudioExportResult
{
    /// <summary>Creates an export result.</summary>
    /// <param name="path">The written destination path.</param>
    /// <param name="format">The encoding used.</param>
    /// <param name="bytesWritten">The resulting file length.</param>
    public AudioExportResult(string path, AudioExportFormat format, long bytesWritten)
    {
        Path = path;
        Format = format;
        BytesWritten = bytesWritten;
    }

    /// <summary>Gets the written path.</summary>
    public string Path { get; }

    /// <summary>Gets the encoding used.</summary>
    public AudioExportFormat Format { get; }

    /// <summary>Gets the resulting file length in bytes.</summary>
    public long BytesWritten { get; }
}

