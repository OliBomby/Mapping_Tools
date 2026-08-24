namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Describes a request to decode one audio file into owned samples.</summary>
public sealed class AudioDecodeRequest
{
    /// <summary>Creates a file-decoding request.</summary>
    /// <param name="path">The audio file path.</param>
    public AudioDecodeRequest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    /// <summary>Gets the source audio path.</summary>
    public string Path { get; }
}

