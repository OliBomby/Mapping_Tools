namespace Mapping_Tools.Core.Audio;

/// <summary>
///     Owns decoded, interleaved floating-point samples independently of an audio library.
/// </summary>
public sealed class AudioClip
{
    private readonly float[] samples;

    /// <summary>
    ///     Creates an audio clip and copies the supplied sample values.
    /// </summary>
    /// <param name="format">The sample rate and channel layout.</param>
    /// <param name="samples">Interleaved normalized samples.</param>
    public AudioClip(AudioFormat format, IEnumerable<float> samples)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(samples);

        this.samples = samples.ToArray();
        if (this.samples.Length % format.Channels != 0) throw new ArgumentException("The sample count must contain complete interleaved frames.", nameof(samples));

        Format = format;
    }

    /// <summary>Gets the sample rate and channel layout.</summary>
    public AudioFormat Format { get; }

    /// <summary>Gets a read-only view of the interleaved normalized samples.</summary>
    public ReadOnlyMemory<float> Samples => samples;

    /// <summary>Gets the number of complete audio frames.</summary>
    public int FrameCount => samples.Length / Format.Channels;

    /// <summary>Gets the duration represented by the clip.</summary>
    public TimeSpan Duration => TimeSpan.FromSeconds((double)FrameCount / Format.SampleRate);

    /// <summary>Gets whether no audio frames are available.</summary>
    public bool IsEmpty => samples.Length == 0;

    /// <summary>Copies the interleaved sample values for a consumer that needs mutable storage.</summary>
    /// <returns>A new array containing all sample values.</returns>
    public float[] CopySamples()
    {
        return (float[])samples.Clone();
    }
}
