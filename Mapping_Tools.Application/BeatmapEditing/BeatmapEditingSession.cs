using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Keeps a mutable beatmap together with enough provenance to distinguish a
///     durable disk version from a live overlay, plus the exact objects selected
///     when that live state was captured.
/// </summary>
public sealed class BeatmapEditingSession
{
    /// <summary>
    ///     Creates a session around a parsed beatmap and retains its initial
    ///     serialized state for a later backup.
    /// </summary>
    /// <param name="editor">The mutable document editor.</param>
    /// <param name="source">Whether the document came only from disk or was overlaid with live state.</param>
    /// <param name="selectedHitObjects">Objects selected in osu! when live state was captured.</param>
    /// <param name="liveReadFailure">
    ///     A diagnostic from a best-effort live read that fell back to disk, or
    ///     <see langword="null" /> when no read failed.
    /// </param>
    /// <param name="liveEditorTime">The editor playhead captured with live state, when available.</param>
    public BeatmapEditingSession(
        BeatmapEditor editor,
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
    ///     Gets the mutable editor used for transformations and persistence.
    /// </summary>
    public BeatmapEditor Editor { get; }

    /// <summary>
    ///     Gets whether the session reflects only disk or also unsaved editor state.
    /// </summary>
    public BeatmapEditingSource Source { get; }

    /// <summary>
    ///     Gets the same hit-object instances that are marked selected inside
    ///     <see cref="BeatmapEditor.Beatmap" />, preserving identity for mutations.
    /// </summary>
    public IReadOnlyList<HitObject> SelectedHitObjects { get; }

    /// <summary>
    ///     Gets the failure that caused a best-effort live open to use disk, if any.
    /// </summary>
    public Exception? LiveReadFailure { get; }

    /// <summary>
    ///     Gets the editor playhead captured with live state, or <see langword="null" />
    ///     when the session came from disk or the reader did not expose a playhead.
    /// </summary>
    public double? LiveEditorTime { get; }

    /// <summary>
    ///     Gets the serialized document captured when this session was opened.
    /// </summary>
    internal IReadOnlyList<string> InitialBeatmapLines { get; }
}

