namespace Mapping_Tools.Core.Audio;

/// <summary>
/// Describes the in-memory PCM representation used by the audio boundary.
/// </summary>
public sealed class AudioFormat : IEquatable<AudioFormat>
{
    /// <summary>
    /// Creates a PCM format description.
    /// </summary>
    /// <param name="sampleRate">The number of frames per second.</param>
    /// <param name="channels">The number of interleaved channels.</param>
    public AudioFormat(int sampleRate, int channels)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (channels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels));
        }

        SampleRate = sampleRate;
        Channels = channels;
    }

    /// <summary>Gets the number of frames per second.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the number of interleaved channels.</summary>
    public int Channels { get; }

    /// <summary>Gets the number of float values in one audio frame.</summary>
    public int ValuesPerFrame => Channels;

    /// <inheritdoc/>
    public bool Equals(AudioFormat? other) => other is not null &&
        SampleRate == other.SampleRate && Channels == other.Channels;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as AudioFormat);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(SampleRate, Channels);
}
