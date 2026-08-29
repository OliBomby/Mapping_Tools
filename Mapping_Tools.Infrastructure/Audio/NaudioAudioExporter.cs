using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Audio.Effects;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Encodes owned clips as WAV or Ogg Vorbis files using NAudio-compatible providers.</summary>
public sealed class NaudioAudioExporter : IAudioExporter
{
    /// <inheritdoc />
    public Task<AudioExportResult> ExportAsync(
        AudioClip clip,
        AudioExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Export(clip, request, cancellationToken), cancellationToken);
    }

    private static AudioExportResult Export(
        AudioClip clip,
        AudioExportRequest request,
        CancellationToken cancellationToken)
    {
        var exportClip = request.Format != AudioExportFormat.WaveIeeeFloat
                         && NeedsLimiter(clip, cancellationToken)
            ? AudioEffectEngine.Apply(
                clip,
                [new SoftLimiterEffect()],
                cancellationToken)
            : clip;

        string? directory = Path.GetDirectoryName(request.Path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        switch (request.Format)
        {
            case AudioExportFormat.WaveIeeeFloat:
                WriteWave(request.Path, new AudioClipSampleProvider(clip).ToWaveProvider(), cancellationToken);
                break;
            case AudioExportFormat.WavePcm:
                WriteWave(request.Path, new AudioClipSampleProvider(exportClip).ToWaveProvider16(), cancellationToken);
                break;
            case AudioExportFormat.OggVorbis:
                WriteVorbis(request.Path, exportClip, request.Quality, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Format));
        }

        return new AudioExportResult(request.Path, request.Format, new FileInfo(request.Path).Length);
    }

    private static bool NeedsLimiter(AudioClip clip, CancellationToken cancellationToken)
    {
        foreach (float sample in clip.Samples.Span)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sample is < -1f or > 1f) return true;
        }

        return false;
    }

    private static void WriteWave(string path, IWaveProvider source, CancellationToken cancellationToken)
    {
        using var writer = new WaveFileWriter(path, source.WaveFormat);
        byte[] buffer = new byte[Math.Max(source.WaveFormat.AverageBytesPerSecond * 4, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) return;

            writer.Write(buffer, 0, read);
        }
    }

    private static void WriteVorbis(string path, AudioClip clip, float quality, CancellationToken cancellationToken)
    {
        int sampleRate = OggVorbisFileWriter.GetSupportedSampleRate(clip.Format.SampleRate);
        ISampleProvider sampleProvider = new AudioClipSampleProvider(clip);
        if (sampleProvider.WaveFormat.SampleRate != sampleRate) sampleProvider = new WdlResamplingSampleProvider(sampleProvider, sampleRate);

        var source = sampleProvider.ToWaveProvider();
        using var writer = new OggVorbisFileWriter(path, sampleRate, clip.Format.Channels, quality);
        byte[] buffer = new byte[Math.Max(source.WaveFormat.AverageBytesPerSecond * 4, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) return;

            writer.WriteWaveData(buffer, read, source.WaveFormat);
        }
    }

}
