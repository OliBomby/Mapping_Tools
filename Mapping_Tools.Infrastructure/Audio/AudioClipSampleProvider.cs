using Mapping_Tools.Core.Audio;
using NAudio.Wave;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Exposes an owned <see cref="AudioClip" /> through NAudio's pull-based sample provider contract.</summary>
internal sealed class AudioClipSampleProvider : ISampleProvider
{
    private readonly float[] samples;
    private int position;

    /// <summary>Creates a provider that reads a private snapshot of the clip's samples.</summary>
    /// <param name="clip">The clip to expose to NAudio.</param>
    public AudioClipSampleProvider(AudioClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        samples = clip.CopySamples();
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(clip.Format.SampleRate, clip.Format.Channels);
    }

    /// <inheritdoc />
    public WaveFormat WaveFormat { get; }

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        int read = Math.Min(count, samples.Length - position);
        if (read <= 0) return 0;

        for (int index = 0; index < read; index++) buffer[offset + index] = samples[position + index];
        position += read;
        return read;
    }
}
