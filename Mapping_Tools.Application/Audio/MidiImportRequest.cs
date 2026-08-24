using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Describes a MIDI import request.</summary>
public sealed class MidiImportRequest
{
    /// <summary>Creates a MIDI import request.</summary>
    /// <param name="path">The MIDI file path.</param>
    public MidiImportRequest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    /// <summary>Gets the source MIDI path.</summary>
    public string Path { get; }
}

