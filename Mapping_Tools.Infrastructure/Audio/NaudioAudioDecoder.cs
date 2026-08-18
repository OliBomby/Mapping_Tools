using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>
/// Decodes WAV, Ogg Vorbis, and Windows Media Foundation-supported files into owned clips.
/// </summary>
public sealed class NaudioAudioDecoder : IAudioDecoder
{
    private static readonly string[] SupportedExtensions = [".wav", ".ogg", ".mp3"];

    /// <inheritdoc/>
    public Task<AudioClip> DecodeAsync(AudioDecodeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Decode(request.Path, cancellationToken), cancellationToken);
    }

    private static AudioClip Decode(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The audio source does not exist.", path);
        }

        string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension, StringComparer.Ordinal))
        {
            throw new NotSupportedException($"Audio decoding does not support '{extension}'.");
        }

        using WaveStream source = OpenSource(path, extension);
        ISampleProvider provider = ToSampleProvider(source);
        var samples = new List<float>();
        float[] buffer = new float[Math.Max(provider.WaveFormat.SampleRate * provider.WaveFormat.Channels, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = provider.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            samples.AddRange(buffer.AsSpan(0, read).ToArray());
        }

        return new AudioClip(
            new AudioFormat(provider.WaveFormat.SampleRate, provider.WaveFormat.Channels),
            samples);
    }

    private static WaveStream OpenSource(string path, string extension) => extension switch
    {
        ".wav" => new WaveFileReader(path),
        ".ogg" => new VorbisWaveReader(path),
        _ => new MediaFoundationReader(path)
    };

    private static ISampleProvider ToSampleProvider(WaveStream source)
    {
        if (source.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            return source.WaveFormat.BitsPerSample switch
            {
                8 => new Pcm8BitToSampleProvider(source),
                16 => new Pcm16BitToSampleProvider(source),
                24 => new Pcm24BitToSampleProvider(source),
                32 => new Pcm32BitToSampleProvider(source),
                _ => throw new NotSupportedException($"PCM bit depth {source.WaveFormat.BitsPerSample} is not supported.")
            };
        }

        if (source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            return new WaveToSampleProvider(source);
        }

        throw new NotSupportedException($"Audio encoding {source.WaveFormat.Encoding} is not supported.");
    }
}
