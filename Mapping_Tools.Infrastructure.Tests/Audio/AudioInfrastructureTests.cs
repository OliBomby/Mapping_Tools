using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Infrastructure.Audio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Midi;

namespace Mapping_Tools.Infrastructure.Tests.Audio;

[TestClass]
public sealed class AudioInfrastructureTests
{
    [TestMethod]
    public async Task NaudioAudioDecoder_DecodesTheWaveZeroVorbisFixture()
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Audio", "soft-hitwhistle6.ogg");
        var decoder = new NaudioAudioDecoder();

        // Act
        var result = await decoder.DecodeAsync(new AudioDecodeRequest(path));

        // Assert
        result.IsEmpty.Should().BeFalse();
        result.Format.SampleRate.Should().BeGreaterThan(0);
        result.Format.Channels.Should().BeGreaterThan(0);
        result.CopySamples().Should().AllSatisfy(sample => float.IsFinite(sample).Should().BeTrue());
    }

    [TestMethod]
    public async Task NaudioAudioGenerator_DecodesAndAppliesAFrameworkNeutralEffect()
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Audio", "soft-hitwhistle6.ogg");
        var decoder = new NaudioAudioDecoder();
        var generator = new NaudioAudioGenerator(
            decoder,
            new NaudioSoundFontRenderer(),
            new NaudioAudioEffectService());
        var request = new AudioGenerationRequest(
            new SampleGeneratingArgs(path),
            [AudioEffect.CreateDelayFadeOut(0, 1)]);

        // Act
        var result = await generator.GenerateAsync(request);

        // Assert
        result.IsEmpty.Should().BeFalse();
        result.CopySamples().Should().AllSatisfy(sample => float.IsFinite(sample).Should().BeTrue());
    }

    [TestMethod]
    public async Task NaudioSoundFontRenderer_MissingSourceReportsAUsefulFailure()
    {
        // Arrange
        var renderer = new NaudioSoundFontRenderer();
        var sample = new SampleGeneratingArgs(
            Path.Combine(Path.GetTempPath(), $"missing-mapping-tools-{Guid.NewGuid():N}.sf2"),
            0,
            0,
            0,
            60,
            100,
            127);

        // Act
        Func<Task> act = () => renderer.RenderAsync(new SoundFontNoteRequest(sample));

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [TestMethod]
    public async Task NaudioAudioExporter_WaveFloatRoundTripsOwnedSamples()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "roundtrip.wav");
        var source = new AudioClip(new AudioFormat(8000, 1), [0f, 0.25f, -0.5f, 1f]);
        var exporter = new NaudioAudioExporter();
        var decoder = new NaudioAudioDecoder();

        // Act
        var export = await exporter.ExportAsync(
            source,
            new AudioExportRequest(path, AudioExportFormat.WaveIeeeFloat));
        var result = await decoder.DecodeAsync(new AudioDecodeRequest(path));

        // Assert
        export.BytesWritten.Should().BeGreaterThan(0);
        result.Format.Should().Be(source.Format);
        result.CopySamples().Should().Equal(source.CopySamples());
    }

    [TestMethod]
    public async Task NaudioAudioExporter_PcmRoundTripsWithinQuantizationTolerance()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "roundtrip-pcm.wav");
        var source = new AudioClip(new AudioFormat(8000, 1), [0f, 0.1f, -0.2f, 0.3f]);
        var exporter = new NaudioAudioExporter();
        var decoder = new NaudioAudioDecoder();

        // Act
        var export = await exporter.ExportAsync(
            source,
            new AudioExportRequest(path, AudioExportFormat.WavePcm));
        var result = await decoder.DecodeAsync(new AudioDecodeRequest(path));

        // Assert
        export.BytesWritten.Should().BeGreaterThan(0);
        float[] actual = result.CopySamples();
        float[] expected = source.CopySamples();
        actual.Should().HaveSameCount(expected);
        for (int index = 0; index < expected.Length; index++) actual[index].Should().BeApproximately(expected[index], 1e-4f);
    }

    [TestMethod]
    public async Task NaudioAudioExporter_OggRoundTripsAClipAndClosesTheDestination()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "roundtrip.ogg");
        var source = new AudioClip(new AudioFormat(8000, 1), Enumerable.Repeat(0.2f, 800));
        var exporter = new NaudioAudioExporter();
        var decoder = new NaudioAudioDecoder();

        // Act
        var export = await exporter.ExportAsync(
            source,
            new AudioExportRequest(path, AudioExportFormat.OggVorbis));
        var result = await decoder.DecodeAsync(new AudioDecodeRequest(path));

        // Assert
        export.BytesWritten.Should().BeGreaterThan(0);
        result.IsEmpty.Should().BeFalse();
        File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose();
    }

    [TestMethod]
    public async Task FastFourierSpectrumCalculator_ReturnsPeakBinsAndEmptyState()
    {
        // Arrange
        var calculator = new FastFourierSpectrumCalculator();
        var impulse = new AudioClip(new AudioFormat(8000, 1), [0f, 1f, 0f, 0f]);
        var empty = new AudioClip(new AudioFormat(8000, 1), []);

        // Act
        var result = await calculator.CalculateAsync(
            impulse,
            new SpectrumCalculationOptions { FftSize = 4 });
        var emptyResult = await calculator.CalculateAsync(
            empty,
            new SpectrumCalculationOptions { FftSize = 4 });

        // Assert
        result.Magnitudes.Should().HaveCount(3);
        result.PeakMagnitude.Should().BeGreaterThan(0);
        emptyResult.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public async Task NaudioMidiService_ExportsAndImportsNeutralNoteData()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "roundtrip.mid");
        var sequence = new MidiSequence(
            [new MidiNote(250, 500, 0, 4, 60, 100, 1)],
            [new MidiVolumeChange(0, 1, 90)]);
        var service = new NaudioMidiService();

        // Act
        await service.ExportAsync(new MidiExportRequest(path, sequence));
        var result = await service.ImportAsync(new MidiImportRequest(path));

        // Assert
        result.Notes.Should().ContainSingle();
        result.Notes[0].StartMilliseconds.Should().BeApproximately(250, 5);
        result.Notes[0].DurationMilliseconds.Should().BeApproximately(500, 5);
        result.Notes[0].Key.Should().Be(60);
        result.VolumeChanges.Should().ContainSingle();
    }

    [TestMethod]
    public async Task NaudioMidiService_ImportsTempoEventsUsingElapsedTimeBeforeTheFirstTempo()
    {
        // Arrange
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "tempo-offset.mid");
        var collection = new MidiEventCollection(0, 120);
        collection.AddEvent(new TempoEvent(1_000_000, 100), 0);
        collection.AddEvent(new NoteOnEvent(120, 1, 60, 100, 120), 0);
        collection.AddEvent(new NoteEvent(240, 1, MidiCommandCode.NoteOff, 60, 0), 0);
        collection.PrepareForExport();
        MidiFile.Export(path, collection);
        var service = new NaudioMidiService();

        // Act
        var result = await service.ImportAsync(new MidiImportRequest(path));

        // Assert
        result.Notes.Should().ContainSingle();
        result.Notes[0].StartMilliseconds.Should().BeApproximately(583.33, 2);
    }

    [TestMethod]
    public async Task NaudioAudioGenerator_DoesNotReapplySoundFontTransforms()
    {
        // Arrange
        AudioClip rendered = new(new AudioFormat(8000, 2), [0.25f, 0.75f, 0.5f, 0.5f]);
        var renderer = new StubSoundFontRenderer(rendered);
        var generator = new NaudioAudioGenerator(
            new ThrowingDecoder(),
            renderer,
            new NaudioAudioEffectService());
        var sample = new SampleGeneratingArgs(
            "sample.sf2", 0, 0, 0, 60, -1, 127)
        {
            Panning = 0.5,
            PitchShift = 12,
        };

        // Act
        var result = await generator.GenerateAsync(new AudioGenerationRequest(sample));

        // Assert
        result.Format.Should().Be(rendered.Format);
        result.CopySamples().Should().Equal(rendered.CopySamples());
        renderer.LastRequest!.Sample.Panning.Should().Be(0.5);
        renderer.LastRequest.Sample.PitchShift.Should().Be(12);
    }

    [TestMethod]
    public async Task FastFourierSpectrumCalculator_HonorsCancellation()
    {
        // Arrange
        var calculator = new FastFourierSpectrumCalculator();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => calculator.CalculateAsync(
            new AudioClip(new AudioFormat(8000, 1), [1f]),
            cancellationToken: cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory("mapping-tools-audio-");

        public string Path => directory.FullName;

        public void Dispose()
        {
            if (directory.Exists) directory.Delete(true);
        }
    }

    private sealed class StubSoundFontRenderer(AudioClip result) : ISoundFontRenderer
    {
        public SoundFontNoteRequest? LastRequest { get; private set; }

        public Task<AudioClip?> RenderAsync(
            SoundFontNoteRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<AudioClip?>(result);
        }
    }

    private sealed class ThrowingDecoder : IAudioDecoder
    {
        public Task<AudioClip> DecodeAsync(
            AudioDecodeRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("The SoundFont path must not be sent to the audio decoder.");
        }
    }
}
