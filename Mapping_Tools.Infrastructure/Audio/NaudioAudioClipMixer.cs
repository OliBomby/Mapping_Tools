using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Core.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Mixes owned clips through NAudio behind the Application port.</summary>
public sealed class NaudioAudioClipMixer : IAudioClipMixer
{
    /// <inheritdoc />
    public Task<AudioClip> MixAsync(
        IReadOnlyList<AudioClip> clips,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Mix(clips, cancellationToken), cancellationToken);
    }

    private static AudioClip Mix(IReadOnlyList<AudioClip> clips, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(clips);
        if (clips.Count == 0) throw new ArgumentException("At least one clip is required.", nameof(clips));

        int sampleRate = clips.Max(clip => clip.Format.SampleRate);
        int channels = clips.Max(clip => clip.Format.Channels);
        var providers = clips.Select(clip =>
        {
            ISampleProvider provider = new AudioClipSampleProvider(clip);
            if (provider.WaveFormat.Channels == 1 && channels == 2)
                provider = new MonoToStereoSampleProvider(provider);
            else if (provider.WaveFormat.Channels == 2 && channels == 1) provider = new StereoToMonoSampleProvider(provider);

            if (provider.WaveFormat.SampleRate != sampleRate) provider = new WdlResamplingSampleProvider(provider, sampleRate);

            return provider;
        });

        MixingSampleProvider mixer = new(providers);
        List<float> samples = [];
        float[] buffer = new float[Math.Max(sampleRate * channels, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = mixer.Read(buffer, 0, buffer.Length);
            if (read == 0) break;
            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        return new AudioClip(new AudioFormat(sampleRate, channels), samples);
    }

}
