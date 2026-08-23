using NAudio.Wave;
using OggVorbisEncoder;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Owns an Ogg Vorbis encoder and flushes its final pages on disposal.</summary>
internal sealed class OggVorbisFileWriter : IDisposable
{
    private static readonly IReadOnlyDictionary<int, int> startBuffers = new Dictionary<int, int>
    {
        [48000] = 1024,
        [44100] = 1024,
        [32000] = 1024,
        [22050] = 512,
        [16000] = 512,
        [11025] = 256,
        [8000] = 256,
    };

    private readonly OggStream oggStream;

    private readonly Stream output;
    private readonly ProcessingState processingState;
    private bool disposed;

    public OggVorbisFileWriter(string path, int sampleRate, int channels, float quality)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!startBuffers.ContainsKey(sampleRate)) throw new InvalidOperationException($"Vorbis writer does not support {sampleRate} sample rate.");

        output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        SampleRate = sampleRate;
        Channels = channels;
        var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, quality);
        oggStream = new OggStream(Random.Shared.Next());
        oggStream.PacketIn(HeaderPacketBuilder.BuildInfoPacket(info));
        oggStream.PacketIn(HeaderPacketBuilder.BuildCommentsPacket(new Comments()));
        oggStream.PacketIn(HeaderPacketBuilder.BuildBooksPacket(info));
        FlushPages(true);
        processingState = ProcessingState.Create(info);
        float[][] silence = Enumerable.Range(0, channels)
            .Select(_ => new float[startBuffers[sampleRate]])
            .ToArray();
        processingState.WriteData(silence, silence[0].Length);
    }

    public int SampleRate { get; }
    public int Channels { get; }

    public void Dispose()
    {
        if (disposed) return;

        disposed = true;
        processingState.WriteEndOfStream();
        while (!oggStream.Finished && processingState.PacketOut(out var packet))
        {
            oggStream.PacketIn(packet);
            FlushPages(false);
        }

        FlushPages(true);
        output.Dispose();
    }

    public static int GetSupportedSampleRate(int sampleRate)
    {
        int selected = 48000;
        foreach (int supported in startBuffers.Keys)
            if (supported >= sampleRate && supported <= selected)
                selected = supported;

        return selected;
    }

    public void WriteWaveData(byte[] data, int count, WaveFormat format)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        int bytesPerSample = format.BitsPerSample / 8;
        double conversion = (double)format.SampleRate / SampleRate;
        int sampleCount = count / bytesPerSample / format.Channels;
        int outputCount = Math.Max(0, (int)(sampleCount / conversion));
        float[][] output = Enumerable.Range(0, Channels)
            .Select(_ => new float[outputCount])
            .ToArray();

        for (int sample = 0; sample < outputCount; sample++)
        for (int channel = 0; channel < Channels; channel++)
        {
            int sourceIndex = (int)(sample * conversion) * format.Channels * bytesPerSample;
            if (channel < format.Channels) sourceIndex += channel * bytesPerSample;

            output[channel][sample] = format.Encoding switch
            {
                WaveFormatEncoding.Pcm when format.BitsPerSample == 8 => data[sourceIndex] / 128f,
                WaveFormatEncoding.Pcm when format.BitsPerSample == 16 =>
                    (short)(data[sourceIndex + 1] << 8 | data[sourceIndex]) / 32768f,
                WaveFormatEncoding.IeeeFloat => BitConverter.ToSingle(data, sourceIndex),
                _ => throw new InvalidOperationException($"Vorbis encoding does not support {format.Encoding}."),
            };
        }

        processingState.WriteData(output, outputCount);
        while (!oggStream.Finished && processingState.PacketOut(out var packet))
        {
            oggStream.PacketIn(packet);
            FlushPages(false);
        }
    }

    private void FlushPages(bool force)
    {
        while (oggStream.PageOut(out var page, force))
        {
            output.Write(page.Header, 0, page.Header.Length);
            output.Write(page.Body, 0, page.Body.Length);
        }
    }
}
