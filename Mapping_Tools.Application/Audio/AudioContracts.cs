using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Audio;

/// <summary>Describes a request to decode one audio file into owned samples.</summary>
public sealed class AudioDecodeRequest
{
    /// <summary>Creates a file-decoding request.</summary>
    /// <param name="path">The audio file path.</param>
    public AudioDecodeRequest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    /// <summary>Gets the source audio path.</summary>
    public string Path { get; }
}

/// <summary>Provides fully decoded audio without leaking decoder-owned resources.</summary>
public interface IAudioDecoder
{
    /// <summary>Decodes a supported audio file into an owned floating-point clip.</summary>
    /// <param name="request">The source file request.</param>
    /// <param name="cancellationToken">Token checked while reading frames.</param>
    /// <returns>The decoded clip.</returns>
    Task<AudioClip> DecodeAsync(AudioDecodeRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Describes generation transforms applied to a hitsound sample.</summary>
public sealed class AudioGenerationRequest
{
    /// <summary>Creates a generation request and copies the mutable sample specification.</summary>
    /// <param name="sample">The source and SoundFont selection arguments.</param>
    /// <param name="effects">Optional effects applied after generation.</param>
    public AudioGenerationRequest(SampleGeneratingArgs sample, IEnumerable<AudioEffect>? effects = null)
    {
        ArgumentNullException.ThrowIfNull(sample);
        Sample = sample.Copy();
        Effects = (effects ?? []).ToArray();
    }

    /// <summary>Gets an independent copy of the sample-generation arguments.</summary>
    public SampleGeneratingArgs Sample { get; }

    /// <summary>Gets the ordered, framework-neutral effect descriptions.</summary>
    public IReadOnlyList<AudioEffect> Effects { get; }
}

/// <summary>Generates a complete hitsound clip from a source file or SoundFont note.</summary>
public interface IAudioGenerator
{
    /// <summary>Generates samples and disposes all source resources before completing.</summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Token checked during source rendering.</param>
    /// <returns>An owned generated clip.</returns>
    Task<AudioClip> GenerateAsync(AudioGenerationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Applies the registered audio effects to an owned clip.</summary>
public interface IAudioEffectService
{
    /// <summary>Processes a clip without mutating the source.</summary>
    /// <param name="source">The source clip.</param>
    /// <param name="effects">The ordered effect descriptions.</param>
    /// <param name="cancellationToken">Token checked while processing samples.</param>
    /// <returns>A new processed clip.</returns>
    AudioClip Apply(
        AudioClip source,
        IEnumerable<AudioEffect> effects,
        CancellationToken cancellationToken = default);
}

/// <summary>Describes a request to play an owned clip.</summary>
public sealed class AudioPlaybackOptions
{
    /// <summary>Gets or sets whether playback should repeat until stopped.</summary>
    public bool Loop { get; set; }
}

/// <summary>Reports the lifecycle state of an audio preview session.</summary>
public enum AudioPlaybackState
{
    /// <summary>The session has not started or has been stopped.</summary>
    Stopped,

    /// <summary>The output device is currently playing.</summary>
    Playing,

    /// <summary>The output device is paused and can resume.</summary>
    Paused,

    /// <summary>The output device reported an unrecoverable failure.</summary>
    Failed,
}

/// <summary>Owns one active playback device and its generated stream.</summary>
public interface IAudioPlaybackSession : IAsyncDisposable
{
    /// <summary>Gets the current playback state.</summary>
    AudioPlaybackState State { get; }

    /// <summary>Gets the best-effort current position.</summary>
    TimeSpan Position { get; }

    /// <summary>Completes when playback stops or fails.</summary>
    Task Completion { get; }

    /// <summary>Pauses output while retaining the current session.</summary>
    void Pause();

    /// <summary>Resumes output after a pause.</summary>
    void Resume();

    /// <summary>Stops output and releases device and stream resources.</summary>
    ValueTask StopAsync();
}

/// <summary>Creates deterministic playback sessions for fully owned clips.</summary>
public interface IAudioPlaybackService
{
    /// <summary>Starts playback and transfers resource ownership to the returned session.</summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="options">Playback settings.</param>
    /// <param name="cancellationToken">Token checked before opening the device.</param>
    /// <returns>The disposable playback session.</returns>
    Task<IAudioPlaybackSession> PlayAsync(
        AudioClip clip,
        AudioPlaybackOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Identifies the supported file encodings for generated samples.</summary>
public enum AudioExportFormat
{
    /// <summary>32-bit IEEE floating-point WAV.</summary>
    WaveIeeeFloat,

    /// <summary>16-bit PCM WAV.</summary>
    WavePcm,

    /// <summary>Ogg Vorbis.</summary>
    OggVorbis,
}

/// <summary>Describes one file export request.</summary>
public sealed class AudioExportRequest
{
    /// <summary>Creates an export request.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="format">The target encoding.</param>
    /// <param name="quality">Vorbis quality, ignored for WAV formats.</param>
    public AudioExportRequest(string path, AudioExportFormat format, float quality = 0.5f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!float.IsFinite(quality) || quality is < -0.1f or > 1f) throw new ArgumentOutOfRangeException(nameof(quality));

        Path = path;
        Format = format;
        Quality = quality;
    }

    /// <summary>Gets the destination file path.</summary>
    public string Path { get; }

    /// <summary>Gets the requested encoding.</summary>
    public AudioExportFormat Format { get; }

    /// <summary>Gets the Vorbis quality value.</summary>
    public float Quality { get; }
}

/// <summary>Describes the result of a completed audio export.</summary>
public sealed class AudioExportResult
{
    /// <summary>Creates an export result.</summary>
    /// <param name="path">The written destination path.</param>
    /// <param name="format">The encoding used.</param>
    /// <param name="bytesWritten">The resulting file length.</param>
    public AudioExportResult(string path, AudioExportFormat format, long bytesWritten)
    {
        Path = path;
        Format = format;
        BytesWritten = bytesWritten;
    }

    /// <summary>Gets the written path.</summary>
    public string Path { get; }

    /// <summary>Gets the encoding used.</summary>
    public AudioExportFormat Format { get; }

    /// <summary>Gets the resulting file length in bytes.</summary>
    public long BytesWritten { get; }
}

/// <summary>Writes generated clips to supported audio file formats.</summary>
public interface IAudioExporter
{
    /// <summary>Exports a clip and returns only after the destination is closed.</summary>
    /// <param name="clip">The clip to encode.</param>
    /// <param name="request">The output request.</param>
    /// <param name="cancellationToken">Token checked while encoding.</param>
    /// <returns>The completed export result.</returns>
    Task<AudioExportResult> ExportAsync(
        AudioClip clip,
        AudioExportRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Describes a MIDI import request.</summary>
public sealed class MidiImportRequest
{
    /// <summary>Creates a MIDI import request.</summary>
    /// <param name="path">The MIDI file path.</param>
    public MidiImportRequest(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = path;
    }

    /// <summary>Gets the source MIDI path.</summary>
    public string Path { get; }
}

/// <summary>Describes a MIDI export request using neutral event models.</summary>
public sealed class MidiExportRequest
{
    /// <summary>Creates a MIDI export request.</summary>
    /// <param name="path">The destination path.</param>
    /// <param name="sequence">The note and volume events.</param>
    /// <param name="beatsPerMinute">The tempo used to convert milliseconds to ticks.</param>
    public MidiExportRequest(string path, MidiSequence sequence, double beatsPerMinute = 60)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(sequence);
        if (!double.IsFinite(beatsPerMinute) || beatsPerMinute <= 0) throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));

        Path = path;
        Sequence = sequence;
        BeatsPerMinute = beatsPerMinute;
    }

    /// <summary>Gets the destination path.</summary>
    public string Path { get; }

    /// <summary>Gets the neutral MIDI sequence.</summary>
    public MidiSequence Sequence { get; }

    /// <summary>Gets the tempo in beats per minute.</summary>
    public double BeatsPerMinute { get; }
}

/// <summary>Imports and exports neutral MIDI event models.</summary>
public interface IMidiService
{
    /// <summary>Imports note and volume events from a MIDI file.</summary>
    /// <param name="request">The source file.</param>
    /// <param name="cancellationToken">Token checked while reading tracks.</param>
    /// <returns>The imported sequence.</returns>
    Task<MidiSequence> ImportAsync(MidiImportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Exports a neutral MIDI sequence to a file.</summary>
    /// <param name="request">The destination and event data.</param>
    /// <param name="cancellationToken">Token checked while building tracks.</param>
    Task ExportAsync(MidiExportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Describes one SoundFont note-rendering request.</summary>
public sealed class SoundFontNoteRequest
{
    /// <summary>Creates a SoundFont request from the existing sample-generation model.</summary>
    /// <param name="sample">The SoundFont path and note selectors.</param>
    public SoundFontNoteRequest(SampleGeneratingArgs sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        if (!sample.UsesSoundFont) throw new ArgumentException("The sample specification must point to an .sf2 file.", nameof(sample));

        Sample = sample.Copy();
    }

    /// <summary>Gets an independent SoundFont sample specification.</summary>
    public SampleGeneratingArgs Sample { get; }
}

/// <summary>Renders selected SoundFont notes without exposing audio-library SoundFont types.</summary>
public interface ISoundFontRenderer
{
    /// <summary>Renders a SoundFont note into owned floating-point samples.</summary>
    /// <param name="request">The SoundFont note request.</param>
    /// <param name="cancellationToken">Token checked during rendering.</param>
    /// <returns>The generated clip, or <see langword="null" /> when no zone matches.</returns>
    Task<AudioClip?> RenderAsync(SoundFontNoteRequest request, CancellationToken cancellationToken = default);
}
