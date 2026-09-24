using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Keeps a mutable beatmap together with enough provenance to distinguish a
///     durable disk version from a live overlay, plus the exact objects selected
///     when that live state was captured.
/// </summary>
public sealed class BeatmapEditingSession : EditingSession
{
    /// <summary>
    ///     Creates a disk-backed beatmap editing session from serialized lines.
    /// </summary>
    /// <param name="lines">The serialized beatmap lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used by the session.</param>
    public BeatmapEditingSession(List<string> lines, ITextFileStore fileStore)
        : this(lines, fileStore, BeatmapEditingSource.Disk, [])
    {
    }

    /// <summary>
    ///     Creates a beatmap editing session from serialized lines and retains
    ///     their initial state for a later backup.
    /// </summary>
    /// <param name="lines">The serialized beatmap lines to parse.</param>
    /// <param name="fileStore">The persistence implementation used by the session.</param>
    /// <param name="source">Whether the document came only from disk or was overlaid with live state.</param>
    /// <param name="selectedHitObjects">Objects selected in osu! when live state was captured.</param>
    /// <param name="liveReadFailure">
    ///     A diagnostic from a best-effort live read that fell back to disk, or
    ///     <see langword="null" /> when no read failed.
    /// </param>
    /// <param name="liveEditorTime">The editor playhead captured with live state, when available.</param>
    /// <param name="path">The path to the beatmap file.</param>
    public BeatmapEditingSession(
        List<string> lines,
        ITextFileStore fileStore,
        BeatmapEditingSource source,
        IReadOnlyList<HitObject> selectedHitObjects,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null,
        string path = "")
        : base(lines, fileStore)
    {
        ArgumentNullException.ThrowIfNull(selectedHitObjects);

        Path = path;
        Source = source;
        SelectedHitObjects = selectedHitObjects.ToArray();
        LiveReadFailure = liveReadFailure;
        LiveEditorTime = liveEditorTime;
        InitialBeatmapLines = Beatmap.GetLines().ToArray();
    }

    /// <summary>
    ///     Loads a beatmap from a path and retains its initial serialized state
    ///     for a later backup.
    /// </summary>
    /// <param name="path">The beatmap file to load.</param>
    /// <param name="fileStore">The persistence implementation used to load and save.</param>
    /// <param name="source">Whether the document came only from disk or was overlaid with live state.</param>
    /// <param name="selectedHitObjects">Objects selected in osu! when live state was captured.</param>
    /// <param name="liveReadFailure">
    ///     A diagnostic from a best-effort live read that fell back to disk, or
    ///     <see langword="null" /> when no read failed.
    /// </param>
    /// <param name="liveEditorTime">The editor playhead captured with live state, when available.</param>
    public BeatmapEditingSession(
        string path,
        ITextFileStore fileStore,
        BeatmapEditingSource source = BeatmapEditingSource.Disk,
        IReadOnlyList<HitObject>? selectedHitObjects = null,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
        : base(path, fileStore)
    {
        Source = source;
        SelectedHitObjects = (selectedHitObjects ?? []).ToArray();
        LiveReadFailure = liveReadFailure;
        LiveEditorTime = liveEditorTime;
        InitialBeatmapLines = Beatmap.GetLines().ToArray();
    }

    /// <summary>
    ///     Creates a beatmap editing session around an already parsed beatmap.
    /// </summary>
    /// <param name="beatmap">The mutable beatmap owned by the session.</param>
    /// <param name="path">The source or destination path for the beatmap.</param>
    /// <param name="fileStore">The persistence implementation used when saving.</param>
    /// <param name="source">Whether the document came only from disk or was overlaid with live state.</param>
    /// <param name="selectedHitObjects">Objects selected in osu! when live state was captured.</param>
    /// <param name="liveReadFailure">
    ///     A diagnostic from a best-effort live read that fell back to disk, or
    ///     <see langword="null" /> when no read failed.
    /// </param>
    /// <param name="liveEditorTime">The editor playhead captured with live state, when available.</param>
    public BeatmapEditingSession(
        Beatmap beatmap,
        string path,
        ITextFileStore fileStore,
        BeatmapEditingSource source,
        IReadOnlyList<HitObject> selectedHitObjects,
        Exception? liveReadFailure = null,
        double? liveEditorTime = null)
        : base(fileStore)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(selectedHitObjects);

        Path = path;
        TextFile = beatmap;
        Source = source;
        SelectedHitObjects = selectedHitObjects.ToArray();
        LiveReadFailure = liveReadFailure;
        LiveEditorTime = liveEditorTime;
        InitialBeatmapLines = Beatmap.GetLines().ToArray();
    }

    /// <summary>
    ///     Gets the parsed beatmap being transformed and persisted.
    /// </summary>
    public Beatmap Beatmap => (Beatmap)TextFile;

    /// <summary>
    ///     Saves the beatmap and updates its filename from the beatmap metadata.
    /// </summary>
    /// <remarks>This method also updates <see cref="EditingSession.Path" />.</remarks>
    public void SaveFileWithNameUpdate()
    {
        if (FileStore is IAutomaticBeatmapFilenameStore automaticNames)
        {
            Path = automaticNames.WriteBeatmap(Path, Beatmap.GetLines());
            return;
        }
        string parentFolder = GetParentFolder();
        FileStore.Delete(Path);
        Path = FileStore.CombinePath(parentFolder, Beatmap.GetFileName());
        SaveFile();
    }

    /// <summary>
    ///     Gets whether the session reflects only disk or also unsaved editor state.
    /// </summary>
    public BeatmapEditingSource Source { get; }

    /// <summary>
    ///     Gets the same hit-object instances that are marked selected inside
    ///     <see cref="Beatmap" />, preserving identity for mutations.
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
