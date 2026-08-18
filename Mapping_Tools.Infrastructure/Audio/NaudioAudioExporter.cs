using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Encodes owned clips as WAV or Ogg Vorbis files using NAudio-compatible providers.</summary>
public sealed class NaudioAudioExporter : IAudioExporter
{
    /// <inheritdoc/>
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
        AudioClip exportClip = request.Format == AudioExportFormat.WaveIeeeFloat
            ? clip
            : AudioEffectEngine.Apply(
                clip,
                [AudioEffect.CreateSoftLimiter()],
                cancellationToken);

        string? directory = System.IO.Path.GetDirectoryName(request.Path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (request.Format)
        {
            case AudioExportFormat.WaveIeeeFloat:
                WriteWave(request.Path, new ClipSampleProvider(clip).ToWaveProvider(), cancellationToken);
                break;
            case AudioExportFormat.WavePcm:
                WriteWave(request.Path, new ClipSampleProvider(exportClip).ToWaveProvider16(), cancellationToken);
                break;
            case AudioExportFormat.OggVorbis:
                WriteVorbis(request.Path, exportClip, request.Quality, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Format));
        }

        return new AudioExportResult(request.Path, request.Format, new FileInfo(request.Path).Length);
    }

    private static void WriteWave(string path, IWaveProvider source, CancellationToken cancellationToken)
    {
        using var writer = new WaveFileWriter(path, source.WaveFormat);
        byte[] buffer = new byte[Math.Max(source.WaveFormat.AverageBytesPerSecond * 4, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = source.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                return;
            }

            writer.Write(buffer, 0, read);
        }
    }

    private static void WriteVorbis(string path, AudioClip clip, float quality, CancellationToken cancellationToken)
    {
        int sampleRate = OggVorbisFileWriter.GetSupportedSampleRate(clip.Format.SampleRate);
        ISampleProvider sampleProvider = new ClipSampleProvider(clip);
        if (sampleProvider.WaveFormat.SampleRate != sampleRate)
        {
            sampleProvider = new WdlResamplingSampleProvider(sampleProvider, sampleRate);
        }

        IWaveProvider source = sampleProvider.ToWaveProvider();
        using var writer = new OggVorbisFileWriter(path, sampleRate, clip.Format.Channels, quality);
        byte[] buffer = new byte[Math.Max(source.WaveFormat.AverageBytesPerSecond * 4, 4096)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int read = source.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                return;
            }

            writer.WriteWaveData(buffer, read, source.WaveFormat);
        }
    }

    private sealed class ClipSampleProvider : ISampleProvider
    {
        private readonly float[] _samples;
        private int _position;

        public ClipSampleProvider(AudioClip clip)
        {
            _samples = clip.CopySamples();
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(clip.Format.SampleRate, clip.Format.Channels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = Math.Min(count, _samples.Length - _position);
            if (read <= 0)
            {
                return 0;
            }

            for (int index = 0; index < read; index++)
            {
                buffer[offset + index] = _samples[_position + index];
            }
            _position += read;
            return read;
        }
    }
}
