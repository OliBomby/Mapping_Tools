namespace Mapping_Tools.Core.Audio;

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

