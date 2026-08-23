using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Generates ordinary audio files and SoundFont notes behind the Application port.</summary>
public sealed class NaudioAudioGenerator : IAudioGenerator
{
    private readonly IAudioDecoder decoder;
    private readonly IAudioEffectService effects;
    private readonly ISoundFontRenderer soundFontRenderer;

    /// <summary>Creates the NAudio-backed sample generator.</summary>
    /// <param name="decoder">The file decoder.</param>
    /// <param name="soundFontRenderer">The SoundFont renderer.</param>
    /// <param name="effects">The neutral effect adapter.</param>
    public NaudioAudioGenerator(
        IAudioDecoder decoder,
        ISoundFontRenderer soundFontRenderer,
        IAudioEffectService effects)
    {
        this.decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
        this.soundFontRenderer = soundFontRenderer ?? throw new ArgumentNullException(nameof(soundFontRenderer));
        this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
    }

    /// <inheritdoc />
    public async Task<AudioClip> GenerateAsync(
        AudioGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var source = request.Sample.UsesSoundFont
            ? await soundFontRenderer.RenderAsync(new SoundFontNoteRequest(request.Sample), cancellationToken)
                .ConfigureAwait(false)
            : await decoder.DecodeAsync(new AudioDecodeRequest(request.Sample.Path), cancellationToken)
                .ConfigureAwait(false);

        if (source is null) throw new InvalidDataException("No SoundFont zone matched the requested sample.");

        var transformed = request.Sample.UsesSoundFont
            ? source
            : ApplySampleArguments(source, request.Sample, cancellationToken);
        return request.Effects.Count == 0
            ? transformed
            : effects.Apply(transformed, request.Effects, cancellationToken);
    }

    private static AudioClip ApplySampleArguments(
        AudioClip source,
        SampleGeneratingArgs arguments,
        CancellationToken cancellationToken)
    {
        ISampleProvider provider = new ClipSampleProvider(source);
        if (!NearlyEqual(arguments.Volume, 1))
            provider = new VolumeSampleProvider(provider)
            {
                Volume = (float)AudioVolume.ToAmplitude(arguments.Volume),
            };

        if (!NearlyEqual(arguments.Panning, 0))
        {
            if (provider.WaveFormat.Channels == 2) provider = new StereoToMonoSampleProvider(provider);

            provider = new PanningSampleProvider(provider) { Pan = (float)arguments.Panning };
        }

        if (!NearlyEqual(arguments.PitchShift, 0))
        {
            float factor = (float)Math.Pow(2, arguments.PitchShift / 12d);
            provider = new SmbPitchShiftingSampleProvider(provider, 1024, 4, factor);
        }

        var samples = new List<float>();
        float[] buffer = new float[Math.Max(provider.WaveFormat.SampleRate * provider.WaveFormat.Channels, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = provider.Read(buffer, 0, buffer.Length);
            if (read == 0) break;

            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        return new AudioClip(
            new AudioFormat(provider.WaveFormat.SampleRate, provider.WaveFormat.Channels),
            samples);
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) < 1e-12;
    }

    private sealed class ClipSampleProvider : ISampleProvider
    {
        private readonly float[] samples;
        private int position;

        public ClipSampleProvider(AudioClip clip)
        {
            samples = clip.CopySamples();
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(clip.Format.SampleRate, clip.Format.Channels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Math.Min(count, samples.Length - position);
            if (read <= 0) return 0;

            for (int index = 0; index < read; index++) buffer[offset + index] = samples[position + index];
            position += read;
            return read;
        }
    }
}
