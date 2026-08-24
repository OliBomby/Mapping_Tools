using Mapping_Tools.Core.Audio.Midi;

namespace Mapping_Tools.Application.Audio.Models;

/// <summary>Describes a MIDI export request using neutral event models.</summary>
public sealed class MidiExportRequest
{
    /// <summary>Creates a MIDI export request.</summary>
    /// <param name="path">The destination path.</param>
    /// <param name="sequence">The note and volume events.</param>
    /// <param name="beatsPerMinute">The tempo used to convert milliseconds to ticks.</param>
    public MidiExportRequest(string path, MidiSequence sequence, double beatsPerMinute = 60)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(sequence);
        if (!double.IsFinite(beatsPerMinute) || beatsPerMinute <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));

        Path = path;
        Sequence = sequence;
        BeatsPerMinute = beatsPerMinute;
    }

    /// <summary>Gets the destination path.</summary>
    public string Path { get; }

    /// <summary>Gets the neutral MIDI sequence.</summary>
    public MidiSequence Sequence { get; }

    /// <summary>Gets the tempo in beats per minute.</summary>
    public double BeatsPerMinute { get; }
}

