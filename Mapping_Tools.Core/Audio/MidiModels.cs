namespace Mapping_Tools.Core.Audio;

/// <summary>Represents one MIDI note with time values expressed in milliseconds.</summary>
public sealed class MidiNote
{
    /// <summary>Creates a MIDI note description.</summary>
    /// <param name="startMilliseconds">Note start time in milliseconds.</param>
    /// <param name="durationMilliseconds">Note duration in milliseconds.</param>
    /// <param name="bank">MIDI bank number.</param>
    /// <param name="patch">MIDI program number.</param>
    /// <param name="key">MIDI key number.</param>
    /// <param name="velocity">MIDI velocity.</param>
    /// <param name="channel">Source MIDI channel, or <c>-1</c> when unspecified.</param>
    public MidiNote(double startMilliseconds, double durationMilliseconds, int bank, int patch, int key, int velocity, int channel = -1)
    {
        if (!double.IsFinite(startMilliseconds) || startMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startMilliseconds));
        }

        if (!double.IsFinite(durationMilliseconds) || durationMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        }

        StartMilliseconds = startMilliseconds;
        DurationMilliseconds = durationMilliseconds;
        Bank = bank;
        Patch = patch;
        Key = key;
        Velocity = velocity;
        Channel = channel;
    }

    /// <summary>Gets the note start in milliseconds.</summary>
    public double StartMilliseconds { get; }

    /// <summary>Gets the note duration in milliseconds.</summary>
    public double DurationMilliseconds { get; }

    /// <summary>Gets the bank number.</summary>
    public int Bank { get; }

    /// <summary>Gets the program/patch number.</summary>
    public int Patch { get; }

    /// <summary>Gets the MIDI key number.</summary>
    public int Key { get; }

    /// <summary>Gets the MIDI velocity.</summary>
    public int Velocity { get; }

    /// <summary>Gets the source MIDI channel, or <c>-1</c> when unspecified.</summary>
    public int Channel { get; }
}

/// <summary>Represents a MIDI main-volume change on a timestamp.</summary>
public sealed class MidiVolumeChange
{
    /// <summary>Creates a volume-change description.</summary>
    /// <param name="timeMilliseconds">Change timestamp in milliseconds.</param>
    /// <param name="channel">MIDI channel to change.</param>
    /// <param name="volume">MIDI volume value.</param>
    public MidiVolumeChange(double timeMilliseconds, int channel, int volume)
    {
        TimeMilliseconds = timeMilliseconds;
        Channel = channel;
        Volume = volume;
    }

    /// <summary>Gets the change timestamp in milliseconds.</summary>
    public double TimeMilliseconds { get; }

    /// <summary>Gets the MIDI channel.</summary>
    public int Channel { get; }

    /// <summary>Gets the MIDI volume value.</summary>
    public int Volume { get; }
}

/// <summary>Owns the note and volume events of an imported or generated MIDI sequence.</summary>
public sealed class MidiSequence
{
    /// <summary>Creates a sequence and copies its event collections.</summary>
    /// <param name="notes">The note events.</param>
    /// <param name="volumeChanges">Optional main-volume events.</param>
    public MidiSequence(IEnumerable<MidiNote> notes, IEnumerable<MidiVolumeChange>? volumeChanges = null)
    {
        ArgumentNullException.ThrowIfNull(notes);
        Notes = Array.AsReadOnly(notes.ToArray());
        VolumeChanges = Array.AsReadOnly((volumeChanges ?? []).ToArray());
    }

    /// <summary>Gets the imported note events.</summary>
    public IReadOnlyList<MidiNote> Notes { get; }

    /// <summary>Gets the imported main-volume events.</summary>
    public IReadOnlyList<MidiVolumeChange> VolumeChanges { get; }
}
