using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>
///     Decodes WAV, Ogg Vorbis, and MP3 files into owned clips without requiring
///     an operating-system media framework.
/// </summary>
public sealed class NaudioAudioDecoder : IAudioDecoder
{
    private static readonly string[] supportedExtensions = [".wav", ".ogg", ".mp3"];

    /// <inheritdoc />
    public Task<AudioClip> DecodeAsync(AudioDecodeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Decode(request.Path, cancellationToken), cancellationToken);
    }

    private static AudioClip Decode(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path)) throw new FileNotFoundException("The audio source does not exist.", path);

        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (!supportedExtensions.Contains(extension, StringComparer.Ordinal)) throw new NotSupportedException($"Audio decoding does not support '{extension}'.");

        return OpenSource(path, extension, cancellationToken);
    }

    private static AudioClip OpenSource(string path, string extension, CancellationToken cancellationToken)
    {
        return extension switch
        {
            ".wav" => DecodeWave(new WaveFileReader(path), cancellationToken),
            ".ogg" => DecodeWave(new VorbisWaveReader(path), cancellationToken),
            ".mp3" => DecodeMp3(path, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, "The audio extension is not supported."),
        };
    }

    private static AudioClip DecodeWave(WaveStream source, CancellationToken cancellationToken)
    {
        using (source)
        {
            var provider = ToSampleProvider(source);
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
    }

    private static AudioClip DecodeMp3(string path, CancellationToken cancellationToken)
    {
        using var source = new MpegFile(path);
        var samples = new List<float>();
        float[] buffer = new float[Math.Max(source.SampleRate * source.Channels, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = source.ReadSamples(buffer, 0, buffer.Length);
            if (read == 0) break;

            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        return new AudioClip(
            new AudioFormat(source.SampleRate, source.Channels),
            samples);
    }

    private static ISampleProvider ToSampleProvider(WaveStream source)
    {
        if (source.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            return source.WaveFormat.BitsPerSample switch
            {
                8 => new Pcm8BitToSampleProvider(source),
                16 => new Pcm16BitToSampleProvider(source),
                24 => new Pcm24BitToSampleProvider(source),
                32 => new Pcm32BitToSampleProvider(source),
                _ => throw new NotSupportedException($"PCM bit depth {source.WaveFormat.BitsPerSample} is not supported."),
            };

        if (source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat) return new WaveToSampleProvider(source);

        throw new NotSupportedException($"Audio encoding {source.WaveFormat.Encoding} is not supported.");
    }
}
