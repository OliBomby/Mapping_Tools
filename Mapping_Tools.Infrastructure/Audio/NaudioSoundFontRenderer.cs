using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using NAudio.SoundFont;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Renders legacy and current NAudio SoundFont zones into owned clips.</summary>
public sealed class NaudioSoundFontRenderer : ISoundFontRenderer
{
    /// <inheritdoc />
    public Task<AudioClip?> RenderAsync(
        SoundFontNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Render(request.Sample, cancellationToken), cancellationToken);
    }

    private static AudioClip? Render(SampleGeneratingArgs args, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(args.Path)) throw new FileNotFoundException("The SoundFont source does not exist.", args.Path);

        SoundFont soundFont = new(args.Path);
        var sounds = Array.Empty<NaudioSampleSoundGenerator>();
        NaudioSampleSoundGenerator? mixer = null;
        try
        {
            foreach (var preset in soundFont.Presets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (preset.PatchNumber != args.Patch && args.Patch != -1 || preset.Bank != args.Bank && args.Bank != -1)
                    continue;

                sounds = ImportPreset(soundFont, preset, args, cancellationToken);
                if (sounds.Length > 0) break;
            }

            if (sounds.Length == 0) sounds = ImportInstruments(soundFont, args, cancellationToken);

            if (sounds.Length == 0) return null;

            int sampleRate = Math.Min(sounds.Max(sound => sound.OutputSampleRate), 44100);
            foreach (var sound in sounds)
            {
                sound.SampleRate = sampleRate;
                sound.Channels = 2;
            }

            mixer = new NaudioSampleSoundGenerator(sounds)
            {
                Panning = args.Panning,
                PitchShift = args.PitchShift,
            };

            return mixer.Render(cancellationToken);
        }
        finally
        {
            if (mixer is not null)
                mixer.Dispose();
            else
                foreach (var sound in sounds)
                    sound.Dispose();
        }
    }

    private static NaudioSampleSoundGenerator[] ImportPreset(
        SoundFont soundFont,
        Preset preset,
        SampleGeneratingArgs args,
        CancellationToken cancellationToken)
    {
        List<NaudioSampleSoundGenerator> result = [];
        try
        {
            if (args.Instrument != -1)
            {
                if (args.Instrument >= preset.Zones.Length) return [];

                result.AddRange(ImportInstrument(soundFont, preset.Zones[args.Instrument].Instrument(), args, cancellationToken));
                return result.ToArray();
            }

            foreach (var zone in ValidZones(preset.Zones, args, true)) result.AddRange(ImportInstrument(soundFont, zone.Instrument(), args, cancellationToken));

            return result.ToArray();
        }
        catch
        {
            DisposeAll(result);
            throw;
        }
    }

    private static IEnumerable<Zone> ValidZones(
        IEnumerable<Zone> zones,
        SampleGeneratingArgs args,
        bool instrument = false,
        bool sampleHeader = false)
    {
        var validZones = zones
            .Where(zone => IsZoneValid(zone, args.Key, args.Velocity) && (!instrument || zone.Instrument() is not null) && (!sampleHeader || zone.SampleHeader() is not null))
            .ToList();
        if (validZones.Count == 0 || args.Key != -1 && args.Velocity != -1) return validZones;

        var firstZone = validZones[0];
        return validZones.Where(zone =>
            RangeOverlap(firstZone.KeyRange(), zone.KeyRange()) && RangeOverlap(firstZone.VelocityRange(), zone.VelocityRange()));
    }

    private static bool RangeOverlap(ushort first, ushort second)
    {
        byte firstLow = (byte)first;
        byte firstHigh = (byte)(first >> 8);
        byte secondLow = (byte)second;
        byte secondHigh = (byte)(second >> 8);
        return first == 0 || second == 0 || firstLow <= secondHigh && firstHigh >= secondLow;
    }

    private static bool IsZoneValid(Zone zone, int key, int velocity)
    {
        ushort keyRange = zone.KeyRange();
        byte keyLow = (byte)keyRange;
        byte keyHigh = (byte)(keyRange >> 8);
        ushort velocityRange = zone.VelocityRange();
        byte velocityLow = (byte)velocityRange;
        byte velocityHigh = (byte)(velocityRange >> 8);
        return (velocityRange == 0 || velocity == -1 || velocity >= velocityLow && velocity <= velocityHigh) && (keyRange == 0 || key == -1 || key >= keyLow && key <= keyHigh);
    }

    private static NaudioSampleSoundGenerator[] ImportInstruments(
        SoundFont soundFont,
        SampleGeneratingArgs args,
        CancellationToken cancellationToken)
    {
        if (args.Instrument != -1)
        {
            if (args.Instrument >= soundFont.Instruments.Length) return [];

            return ImportInstrument(soundFont, soundFont.Instruments[args.Instrument], args, cancellationToken);
        }

        foreach (var instrument in soundFont.Instruments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sounds = ImportInstrument(soundFont, instrument, args, cancellationToken);
            if (sounds.Length > 0) return sounds;
        }

        return [];
    }

    private static NaudioSampleSoundGenerator[] ImportInstrument(
        SoundFont soundFont,
        Instrument? instrument,
        SampleGeneratingArgs args,
        CancellationToken cancellationToken)
    {
        if (instrument is null || instrument.Zones.Length == 0) return [];

        var globalZone = instrument.Zones[0].SampleHeader() is null ? instrument.Zones[0] : null;
        List<NaudioSampleSoundGenerator> result = [];
        try
        {
            foreach (var zone in ValidZones(instrument.Zones, args, sampleHeader: true)) result.Add(GenerateSample(soundFont, zone, args, globalZone, cancellationToken));

            return result.ToArray();
        }
        catch
        {
            DisposeAll(result);
            throw;
        }
    }

    private static NaudioSampleSoundGenerator GenerateSample(
        SoundFont soundFont,
        Zone zone,
        SampleGeneratingArgs args,
        Zone? globalZone,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Generator[] generators = zone.Generators;
        if (globalZone is not null) generators = generators.Concat(globalZone.Generators).ToArray();

        var output = GetSampleWithLength(generators, soundFont.SampleData, args);
        try
        {
            int velocity = generators.Velocity() is var overrideVelocity && overrideVelocity != 0
                ? overrideVelocity
                : args.Velocity != -1
                    ? args.Velocity
                    : 127;
            output.AmplitudeCorrection = velocity / 127d * Math.Pow(10, generators.Attenuation() / -20d);
            output.Panning = generators.Pan();
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static NaudioSampleSoundGenerator GetSampleWithLength(
        Generator[] generators,
        byte[] sample,
        SampleGeneratingArgs args)
    {
        return generators.SampleModes() switch
        {
            1 => GetSampleContinuous(generators, sample, args),
            3 => GetSampleRemainder(generators, sample, args),
            _ => GetSampleWithoutLoop(generators, sample, args),
        };
    }

    private static NaudioSampleSoundGenerator GetSampleWithoutLoop(
        Generator[] generators,
        byte[] sample,
        SampleGeneratingArgs args,
        bool needsFade = false)
    {
        var header = generators.SampleHeader() ?? throw new InvalidDataException("SoundFont zone has no sample header.");
        int start = (int)header.Start + generators.FullStartAddressOffset();
        int end = (int)header.End + generators.FullEndAddressOffset();
        int length = end - start;
        if (start < 0 || length < 0 || start * 2 + length * 2 > sample.Length) throw new InvalidDataException("SoundFont sample bounds are invalid.");

        double lengthInSeconds = length / (double)header.SampleRate;
        bool fade = args.Length >= 0 && args.Length / 1000 < lengthInSeconds || needsFade;
        double fadeStart = args.Length >= 0
            ? needsFade ? Math.Min(args.Length / 1000, lengthInSeconds - 0.3) : args.Length / 1000
            : lengthInSeconds - 0.3;
        double factor = GetRateFactor(args, generators);
        byte[] buffer = new byte[length * 2];
        Array.Copy(sample, start * 2, buffer, 0, buffer.Length);
        NaudioSampleSoundGenerator output = new(BufferToWaveStream(buffer, (uint)(header.SampleRate * factor)));
        if (fade)
        {
            output.FadeStart = fadeStart;
            output.FadeLength = 0.3;
        }

        return output;
    }

    private static double GetRateFactor(SampleGeneratingArgs args, Generator[] generators)
    {
        int keyCorrection = args.Key != -1 ? (args.Key - generators.Key()) * generators.ScaleTuning() : 0;
        keyCorrection += generators.TotalCorrection();
        return Math.Pow(2, keyCorrection / 1200d);
    }

    private static NaudioSampleSoundGenerator GetSampleContinuous(Generator[] generators, byte[] sample, SampleGeneratingArgs args)
    {
        if (args.Length < 0) return GetSampleWithoutLoop(generators, sample, args, true);

        var header = generators.SampleHeader() ?? throw new InvalidDataException("SoundFont zone has no sample header.");
        int start = (int)header.Start + generators.FullStartAddressOffset();
        int startLoop = (int)header.StartLoop + generators.FullStartLoopAddressOffset();
        int endLoop = (int)header.EndLoop + generators.FullEndLoopAddressOffset();
        int firstLength = startLoop - start;
        int loopLength = endLoop - startLoop;
        if (firstLength < 0 || loopLength <= 0) return GetSampleWithoutLoop(generators, sample, args, true);

        double lengthInSeconds = args.Length / 1000d * GetRateFactor(args, generators) + 0.4;
        int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * header.SampleRate);
        int loopSamples = numberOfSamples - firstLength;
        if (loopSamples <= 0) return GetSampleWithoutLoop(generators, sample, args, true);

        byte[] buffer = new byte[numberOfSamples * 2];
        Array.Copy(sample, start * 2, buffer, 0, firstLength * 2);
        for (int index = 0; index < (loopSamples + loopLength - 1) / loopLength; index++)
            Array.Copy(sample, startLoop * 2, buffer, firstLength * 2 + index * loopLength * 2,
                Math.Min(loopLength * 2, buffer.Length - (firstLength * 2 + index * loopLength * 2)));

        return new NaudioSampleSoundGenerator(BufferToWaveStream(buffer, (uint)(header.SampleRate * GetRateFactor(args, generators))))
        {
            FadeStart = lengthInSeconds - 0.4,
            FadeLength = 0.3,
        };
    }

    private static NaudioSampleSoundGenerator GetSampleRemainder(Generator[] generators, byte[] sample, SampleGeneratingArgs args)
    {
        if (args.Length < 0) return GetSampleWithoutLoop(generators, sample, args);

        var header = generators.SampleHeader() ?? throw new InvalidDataException("SoundFont zone has no sample header.");
        int start = (int)header.Start + generators.FullStartAddressOffset();
        int end = (int)header.End + generators.FullEndAddressOffset();
        int startLoop = (int)header.StartLoop + generators.FullStartLoopAddressOffset();
        int endLoop = (int)header.EndLoop + generators.FullEndLoopAddressOffset();
        int loopLength = endLoop - startLoop;
        int firstLength = startLoop - start;
        int secondLength = end - endLoop;
        if (loopLength <= 0 || firstLength < 0 || secondLength < 0) return GetSampleWithoutLoop(generators, sample, args);

        int numberOfSamples = (int)Math.Ceiling(args.Length / 1000d * GetRateFactor(args, generators) * header.SampleRate);
        int loopSamples = numberOfSamples - firstLength;
        loopSamples = (loopSamples + loopLength - 1) / loopLength * loopLength;
        if (loopSamples <= 0) return GetSampleWithoutLoop(generators, sample, args);

        byte[] buffer = new byte[(firstLength + loopSamples + secondLength) * 2];
        Array.Copy(sample, start * 2, buffer, 0, firstLength * 2);
        for (int index = 0; index < loopSamples / loopLength; index++) Array.Copy(sample, startLoop * 2, buffer, firstLength * 2 + index * loopLength * 2, loopLength * 2);

        Array.Copy(sample, endLoop * 2, buffer, firstLength * 2 + loopSamples * 2, secondLength * 2);
        return new NaudioSampleSoundGenerator(BufferToWaveStream(buffer, (uint)(header.SampleRate * GetRateFactor(args, generators))));
    }

    private static WaveStream BufferToWaveStream(byte[] buffer, uint sampleRate)
    {
        return new RawSourceWaveStream(buffer, 0, buffer.Length, new WaveFormat((int)sampleRate, 16, 1));
    }

    private static void DisposeAll(IEnumerable<NaudioSampleSoundGenerator> sounds)
    {
        foreach (var sound in sounds) sound.Dispose();
    }

    private sealed class NaudioSampleSoundGenerator : IDisposable
    {
        private readonly NaudioSampleSoundGenerator[]? generators;
        private readonly WaveStream? wave;
        private bool disposed;

        public NaudioSampleSoundGenerator(WaveStream wave)
        {
            this.wave = wave ?? throw new ArgumentNullException(nameof(wave));
        }

        public NaudioSampleSoundGenerator(NaudioSampleSoundGenerator[] generators)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            if (this.generators.Length == 0) throw new ArgumentException("At least one generator is required.", nameof(generators));
        }

        public double AmplitudeCorrection { get; set; } = 1;
        public double Panning { get; set; }
        public double PitchShift { get; set; }
        public double FadeStart { get; set; } = -1;
        public double FadeLength { get; set; } = -1;
        public int SampleRate { get; set; } = -1;
        public int Channels { get; set; } = -1;
        public int OutputSampleRate => SampleRate > 0 ? SampleRate : wave?.WaveFormat.SampleRate ?? generators![0].OutputSampleRate;
        public int OutputChannels => Channels > 0 ? Channels : wave?.WaveFormat.Channels ?? generators![0].OutputChannels;

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            wave?.Dispose();
            if (generators is not null)
                foreach (var generator in generators)
                    generator.Dispose();
        }

        public AudioClip Render(CancellationToken cancellationToken)
        {
            var provider = GetSampleProvider();
            var samples = new List<float>();
            float[] buffer = new float[Math.Max(provider.WaveFormat.SampleRate * provider.WaveFormat.Channels, 4096)];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int read = provider.Read(buffer, 0, buffer.Length);
                if (read == 0) break;

                samples.AddRange(buffer.AsSpan(0, read).ToArray());
            }

            return new AudioClip(new AudioFormat(provider.WaveFormat.SampleRate, provider.WaveFormat.Channels), samples);
        }

        private ISampleProvider GetSampleProvider()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ISampleProvider output;
            if (wave is not null)
            {
                wave.Position = 0;
                output = ToSampleProvider(wave);
            }
            else
            {
                output = new MixingSampleProvider(generators!.Select(generator => generator.GetSampleProvider()));
            }

            if (FadeStart >= 0 && FadeLength >= 0)
            {
                var fade = new DelayFadeOutSampleProvider(output);
                fade.BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
                output = fade;
            }

            if (Math.Abs(AmplitudeCorrection - 1) >= 1e-12) output = new VolumeSampleProvider(output) { Volume = (float)AmplitudeCorrection };

            if (Math.Abs(Panning) >= 1e-12)
            {
                if (output.WaveFormat.Channels == 2) output = new StereoToMonoSampleProvider(output);

                output = new PanningSampleProvider(output) { Pan = (float)Panning };
            }

            if (Math.Abs(PitchShift) >= 1e-12) output = new SmbPitchShiftingSampleProvider(output, 1024, 4, (float)Math.Pow(2, PitchShift / 12d));

            if (SampleRate > 0) output = new WdlResamplingSampleProvider(output, SampleRate);

            if (Channels > 0)
                output = Channels == 1
                    ? output.WaveFormat.Channels == 2 ? new StereoToMonoSampleProvider(output) : output
                    : output.WaveFormat.Channels == 1
                        ? new MonoToStereoSampleProvider(output)
                        : output;

            return output;
        }

        private static ISampleProvider ToSampleProvider(WaveStream wave)
        {
            return wave.WaveFormat.Encoding switch
            {
                WaveFormatEncoding.Pcm when wave.WaveFormat.BitsPerSample == 8 => new Pcm8BitToSampleProvider(wave),
                WaveFormatEncoding.Pcm when wave.WaveFormat.BitsPerSample == 16 => new Pcm16BitToSampleProvider(wave),
                WaveFormatEncoding.Pcm when wave.WaveFormat.BitsPerSample == 24 => new Pcm24BitToSampleProvider(wave),
                WaveFormatEncoding.Pcm when wave.WaveFormat.BitsPerSample == 32 => new Pcm32BitToSampleProvider(wave),
                WaveFormatEncoding.IeeeFloat => new WaveToSampleProvider(wave),
                _ => throw new NotSupportedException($"SoundFont stream encoding {wave.WaveFormat.Encoding} is not supported."),
            };
        }
    }
}

internal static class SoundFontGeneratorExtensions
{
    public static Instrument? Instrument(this Zone zone)
    {
        return zone.Generators.Instrument();
    }

    public static Instrument? Instrument(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.Instrument)?.Instrument;
    }

    public static short StartAddressOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.StartAddressOffset)?.Int16Amount ?? 0;
    }

    public static short StartAddressCoarseOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.StartAddressCoarseOffset)?.Int16Amount ?? 0;
    }

    public static int FullStartAddressOffset(this Generator[] generators)
    {
        return generators.StartAddressOffset() + 0x8000 * generators.StartAddressCoarseOffset();
    }

    public static short EndAddressOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.EndAddressOffset)?.Int16Amount ?? 0;
    }

    public static short EndAddressCoarseOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.EndAddressCoarseOffset)?.Int16Amount ?? 0;
    }

    public static int FullEndAddressOffset(this Generator[] generators)
    {
        return generators.EndAddressOffset() + 0x8000 * generators.EndAddressCoarseOffset();
    }

    public static short StartLoopAddressOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.StartLoopAddressOffset)?.Int16Amount ?? 0;
    }

    public static short StartLoopAddressCoarseOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.StartLoopAddressCoarseOffset)?.Int16Amount ?? 0;
    }

    public static int FullStartLoopAddressOffset(this Generator[] generators)
    {
        return generators.StartLoopAddressOffset() + 0x8000 * generators.StartLoopAddressCoarseOffset();
    }

    public static short EndLoopAddressOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.EndLoopAddressOffset)?.Int16Amount ?? 0;
    }

    public static short EndLoopAddressCoarseOffset(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.EndLoopAddressCoarseOffset)?.Int16Amount ?? 0;
    }

    public static int FullEndLoopAddressOffset(this Generator[] generators)
    {
        return generators.EndLoopAddressOffset() + 0x8000 * generators.EndLoopAddressCoarseOffset();
    }

    public static ushort KeyRange(this Zone zone)
    {
        return zone.Generators.KeyRange();
    }

    public static ushort KeyRange(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.KeyRange)?.UInt16Amount ?? 0;
    }

    public static ushort VelocityRange(this Zone zone)
    {
        return zone.Generators.VelocityRange();
    }

    public static ushort VelocityRange(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.VelocityRange)?.UInt16Amount ?? 0;
    }

    public static byte Velocity(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.Velocity)?.LowByteAmount ?? 0;
    }

    public static byte OverridingRootKey(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.OverridingRootKey)?.LowByteAmount ?? 0;
    }

    public static double Pan(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.Pan)?.Int16Amount / 500d ?? 0;
    }

    public static double Attenuation(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.InitialAttenuation)?.Int16Amount / 10d ?? 0;
    }

    public static sbyte Correction(this Generator[] generators)
    {
        return generators.SampleHeader()?.PitchCorrection ?? 0;
    }

    public static short CoarseTune(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.CoarseTune)?.Int16Amount ?? 0;
    }

    public static short FineTune(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.FineTune)?.Int16Amount ?? 0;
    }

    public static int TotalCorrection(this Generator[] generators)
    {
        return generators.Correction() + generators.CoarseTune() * 100 + generators.FineTune();
    }

    public static byte Key(this Generator[] generators)
    {
        var header = generators.SampleHeader();
        if (header is null) return 0;

        byte overriding = generators.OverridingRootKey();
        return overriding != 0 ? overriding : header.OriginalPitch;
    }

    public static short ScaleTuning(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.ScaleTuning)?.Int16Amount ?? 100;
    }

    public static int SampleModes(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.SampleModes)?.UInt16Amount ?? 0;
    }

    public static SampleHeader? SampleHeader(this Zone zone)
    {
        return zone.Generators.SampleHeader();
    }

    public static SampleHeader? SampleHeader(this Generator[] generators)
    {
        return generators.SelectByGenerator(GeneratorEnum.SampleID)?.SampleHeader;
    }

    public static Generator? SelectByGenerator(this Generator[] generators, GeneratorEnum type)
    {
        return generators.FirstOrDefault(generator => generator.GeneratorType == type);
    }
}

internal sealed class DelayFadeOutSampleProvider : ISampleProvider
{
    private readonly object gate = new();
    private readonly ISampleProvider source;
    private int fadeFrameCount;
    private int fadeOutDelayFrames;
    private long framesRead;

    public DelayFadeOutSampleProvider(ISampleProvider source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public WaveFormat WaveFormat => source.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        int sourceSamplesRead = source.Read(buffer, offset, count);
        if (sourceSamplesRead == 0) return 0;

        lock (gate)
        {
            int framesRead = sourceSamplesRead / WaveFormat.Channels;
            for (int frame = 0; frame < framesRead; frame++)
            {
                long absoluteFrame = this.framesRead + frame;
                float multiplier;
                if (absoluteFrame < fadeOutDelayFrames)
                {
                    multiplier = 1;
                }
                else
                {
                    double progress = (double)(absoluteFrame - fadeOutDelayFrames) / fadeFrameCount;
                    multiplier = (float)Math.Clamp(1 - progress, 0, 1);
                }

                int sampleOffset = offset + frame * WaveFormat.Channels;
                for (int channel = 0; channel < WaveFormat.Channels; channel++) buffer[sampleOffset + channel] *= multiplier;
            }

            this.framesRead += framesRead;
        }

        return sourceSamplesRead;
    }

    public void BeginFadeOut(double delayMilliseconds, double durationMilliseconds)
    {
        lock (gate)
        {
            framesRead = 0;
            fadeFrameCount = Math.Max(1, (int)(durationMilliseconds * source.WaveFormat.SampleRate / 1000));
            fadeOutDelayFrames = Math.Max(0, (int)(delayMilliseconds * source.WaveFormat.SampleRate / 1000));
        }
    }
}
