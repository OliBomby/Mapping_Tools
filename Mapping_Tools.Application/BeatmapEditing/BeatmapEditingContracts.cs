using Mapping_Tools.Core.Classes.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
/// Controls whether opening a beatmap may incorporate the unsaved state of
/// the map currently open in osu!.
/// </summary>
public enum LiveBeatmapPreference
{
    /// <summary>
    /// Reads only the file named by the caller and never inspects osu!'s process memory.
    /// </summary>
    DiskOnly,

    /// <summary>
    /// Uses matching live editor state when it is healthy, but keeps the
    /// on-disk document when the editor is unavailable or cannot be read.
    /// </summary>
    PreferLive,

    /// <summary>
    /// Requires healthy live state for the requested beatmap and reports a
    /// failure instead of silently editing an older on-disk version.
    /// </summary>
    RequireLive
}

/// <summary>
/// Describes which source supplied the mutable beatmap returned by an editing session.
/// </summary>
public enum BeatmapEditingSource
{
    /// <summary>
    /// The session contains exactly the version parsed from disk.
    /// </summary>
    Disk,

    /// <summary>
    /// The disk document was updated with unsaved timing, object, bookmark,
    /// and difficulty state read from osu!.
    /// </summary>
    LiveEditor
}

/// <summary>
/// Carries the editor-facing parts of an unsaved osu! beatmap without exposing
/// the third-party Editor Reader library beyond the infrastructure boundary.
/// </summary>
public sealed record LiveBeatmapSnapshot
{
    /// <summary>
    /// Creates an immutable description of one successful read from osu!'s editor.
    /// </summary>
    /// <param name="path">The full path of the beatmap whose memory was read.</param>
    /// <param name="bookmarks">Editor bookmark times in milliseconds.</param>
    /// <param name="timingPoints">The redlines and greenlines currently held by the editor.</param>
    /// <param name="hitObjects">The editor's hit objects, including their selection flags.</param>
    /// <param name="previewTime">The preview timestamp currently configured in the editor.</param>
    /// <param name="sliderMultiplier">The base slider velocity currently configured in the editor.</param>
    /// <param name="sliderTickRate">The slider tick rate currently configured in the editor.</param>
    /// <param name="editorTime">The current editor playhead in milliseconds, when available.</param>
    public LiveBeatmapSnapshot(
        string path,
        IReadOnlyList<double> bookmarks,
        IReadOnlyList<TimingPoint> timingPoints,
        IReadOnlyList<HitObject> hitObjects,
        int previewTime,
        double sliderMultiplier,
        double sliderTickRate,
        double? editorTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bookmarks);
        ArgumentNullException.ThrowIfNull(timingPoints);
        ArgumentNullException.ThrowIfNull(hitObjects);

        Path = path;
        Bookmarks = bookmarks.ToArray();
        TimingPoints = timingPoints.ToArray();
        HitObjects = hitObjects.ToArray();
        PreviewTime = previewTime;
        SliderMultiplier = sliderMultiplier;
        SliderTickRate = sliderTickRate;
        EditorTime = editorTime;
    }

    /// <summary>
    /// Gets the full path reconstructed from osu!'s Songs directory and the
    /// folder and filename reported by the editor.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the bookmark times that have not necessarily been saved to disk yet.
    /// </summary>
    public IReadOnlyList<double> Bookmarks { get; }

    /// <summary>
    /// Gets the complete live timing section after Editor Reader validation.
    /// </summary>
    public IReadOnlyList<TimingPoint> TimingPoints { get; }

    /// <summary>
    /// Gets the complete live hit-object section; selected objects retain
    /// <see cref="HitObject.IsSelected"/> so callers can act on the editor selection.
    /// </summary>
    public IReadOnlyList<HitObject> HitObjects { get; }

    /// <summary>
    /// Gets the live preview timestamp in milliseconds.
    /// </summary>
    public int PreviewTime { get; }

    /// <summary>
    /// Gets the live base slider velocity.
    /// </summary>
    public double SliderMultiplier { get; }

    /// <summary>
    /// Gets the live number of slider ticks per beat.
    /// </summary>
    public double SliderTickRate { get; }

    /// <summary>
    /// Gets the live editor playhead in milliseconds, when the platform reader
    /// can supply it.
    /// </summary>
    public double? EditorTime { get; }
}

/// <summary>
/// Reads the current osu! editor state while keeping process discovery and
/// memory-reading details outside the application layer.
/// </summary>
public interface ILiveBeatmapReader
{
    /// <summary>
    /// Attempts to capture a validated editor snapshot.
    /// </summary>
    /// <param name="cancellationToken">Cancels process discovery or a pending memory read.</param>
    /// <returns>
    /// The live snapshot, or <see langword="null"/> when osu! is not running or
    /// no beatmap is open in its editor.
    /// </returns>
    Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Requests that osu! discard its cached view of the current file and load the
/// freshly written version from disk.
/// </summary>
public interface IEditorReloadService
{
    /// <summary>
    /// Sends the reload gesture to an active osu! editor, or does nothing when
    /// osu! is closed or has no usable window.
    /// </summary>
    /// <param name="cancellationToken">Cancels before or between input operations.</param>
    /// <returns>A task that completes after the reload gesture is delivered or skipped.</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Keeps a mutable beatmap together with enough provenance to distinguish a
/// durable disk version from a live overlay, plus the exact objects selected
/// when that live state was captured.
/// </summary>
public sealed class BeatmapEditingSession
{
    /// <summary>
    /// Creates a session around a parsed beatmap and retains its initial
    /// serialized state for a later safety copy.
    /// </summary>
    /// <param name="editor">The mutable document editor.</param>
    /// <param name="source">Whether the document came only from disk or was overlaid with live state.</param>
    /// <param name="selectedHitObjects">Objects selected in osu! when live state was captured.</param>
    /// <param name="liveReadFailure">
    /// A diagnostic from a best-effort live read that fell back to disk, or
    /// <see langword="null"/> when no read failed.
    /// </param>
    /// <param name="liveEditorTime">The editor playhead captured with live state, when available.</param>
    public BeatmapEditingSession(
        BeatmapEditor2 editor,
        BeatmapEditingSource source,
        IReadOnlyList<HitObject> selectedHitObjects,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(selectedHitObjects);

        Editor = editor;
        Source = source;
        SelectedHitObjects = selectedHitObjects.ToArray();
        LiveReadFailure = liveReadFailure;
        LiveEditorTime = liveEditorTime;
        InitialBeatmapLines = editor.Beatmap.GetLines().ToArray();
    }

    /// <summary>
    /// Gets the mutable editor used for transformations and persistence.
    /// </summary>
    public BeatmapEditor2 Editor { get; }

    /// <summary>
    /// Gets whether the session reflects only disk or also unsaved editor state.
    /// </summary>
    public BeatmapEditingSource Source { get; }

    /// <summary>
    /// Gets the same hit-object instances that are marked selected inside
    /// <see cref="BeatmapEditor2.Beatmap"/>, preserving identity for mutations.
    /// </summary>
    public IReadOnlyList<HitObject> SelectedHitObjects { get; }

    /// <summary>
    /// Gets the failure that caused a best-effort live open to use disk, if any.
    /// </summary>
    public Exception? LiveReadFailure { get; }

    /// <summary>
    /// Gets the editor playhead captured with live state, or <see langword="null"/>
    /// when the session came from disk or the reader did not expose a playhead.
    /// </summary>
    public double? LiveEditorTime { get; }

    /// <summary>
    /// Gets the serialized document captured when this session was opened.
    /// </summary>
    internal IReadOnlyList<string> InitialBeatmapLines { get; }
}

/// <summary>
/// Coordinates loading, saving, and optional osu! reloads without coupling
/// feature code to either the physical filesystem or Editor Reader.
/// </summary>
public interface IBeatmapEditingGateway
{
    /// <summary>
    /// Opens an osu! beatmap and optionally overlays matching unsaved editor state.
    /// </summary>
    /// <param name="path">The on-disk beatmap used as the complete document baseline.</param>
    /// <param name="livePreference">The caller's tolerance for missing or invalid live state.</param>
    /// <param name="cancellationToken">Cancels before or after external reads.</param>
    /// <returns>A mutable session whose selected objects belong to its beatmap.</returns>
    Task<BeatmapEditingSession> OpenBeatmapAsync(
        string path,
        LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens an on-disk storyboard; osu! does not expose storyboard state
    /// through the live beatmap reader.
    /// </summary>
    /// <param name="path">The storyboard file to parse.</param>
    /// <param name="cancellationToken">Cancels before the disk read begins.</param>
    /// <returns>A mutable storyboard editor backed by the configured file store.</returns>
    Task<StoryboardEditor2> OpenStoryboardAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the editor's current document only after a mandatory safety
    /// backup succeeds, then optionally asks osu! to reload it.
    /// </summary>
    /// <param name="editor">The beatmap or storyboard editor to save.</param>
    /// <param name="reloadEditor">Whether an active osu! editor should be refreshed after the write.</param>
    /// <param name="cancellationToken">Cancels before saving or before requesting the reload.</param>
    /// <returns>A task that completes after backup, persistence, and any requested reload.</returns>
    /// <exception cref="IOException">
    /// The safety copy or document write fails; a backup failure leaves the source untouched.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Cancellation occurs before backup, save, or a requested reload completes.
    /// </exception>
    Task SaveAsync(
        Editor2 editor,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a beatmap session using the session's original live state for
    /// the optional companion safety copy, then optionally reloads osu!.
    /// </summary>
    /// <param name="session">The opened beatmap session to save.</param>
    /// <param name="reloadEditor">Whether an active osu! editor should be refreshed after the write.</param>
    /// <param name="cancellationToken">Cancels before saving or before requesting the reload.</param>
    /// <returns>A task that completes after backup, persistence, and any requested reload.</returns>
    /// <exception cref="IOException">
    /// The safety copy or document write fails; a backup failure leaves the source untouched.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Cancellation occurs before backup, save, or a requested reload completes.
    /// </exception>
    Task SaveAsync(
        BeatmapEditingSession session,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Reports that a caller required current editor state but it could not be
/// safely associated with the requested beatmap.
/// </summary>
public sealed class LiveBeatmapUnavailableException : Exception
{
    /// <summary>
    /// Creates an availability error with a user-facing explanation.
    /// </summary>
    /// <param name="message">Why live editor state could not be supplied.</param>
    public LiveBeatmapUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Wraps the process or validation failure that prevented a live read.
    /// </summary>
    /// <param name="message">Why live editor state could not be supplied.</param>
    /// <param name="innerException">The low-level read or validation failure.</param>
    public LiveBeatmapUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
