using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing.Models;

/// <summary>
///     Carries the editor-facing parts of an unsaved osu! beatmap without exposing
///     the third-party Editor Reader library beyond the infrastructure boundary.
/// </summary>
public sealed record LiveBeatmapSnapshot
{
    /// <summary>
    ///     Creates an immutable description of one successful read from osu!'s editor.
    /// </summary>
    /// <param name="path">The full path of the beatmap whose memory was read.</param>
    /// <param name="bookmarks">Editor bookmark times in milliseconds.</param>
    /// <param name="timingPoints">The redlines and greenlines currently held by the editor.</param>
    /// <param name="hitObjects">The editor's plain hit objects.</param>
    /// <param name="selectedHitObjects">The editor-selected objects, kept separately from object data.</param>
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
        double? editorTime = null,
        IReadOnlyList<HitObject>? selectedHitObjects = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(bookmarks);
        ArgumentNullException.ThrowIfNull(timingPoints);
        ArgumentNullException.ThrowIfNull(hitObjects);

        Path = path;
        Bookmarks = bookmarks.ToArray();
        TimingPoints = timingPoints.ToArray();
        HitObjects = hitObjects.ToArray();
        SelectedHitObjects = (selectedHitObjects ?? []).ToArray();
        PreviewTime = previewTime;
        SliderMultiplier = sliderMultiplier;
        SliderTickRate = sliderTickRate;
        EditorTime = editorTime;
    }

    /// <summary>
    ///     Gets the full path reconstructed from osu!'s Songs directory and the
    ///     folder and filename reported by the editor.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Gets the bookmark times that have not necessarily been saved to disk yet.
    /// </summary>
    public IReadOnlyList<double> Bookmarks { get; }

    /// <summary>
    ///     Gets the complete live timing section after Editor Reader validation.
    /// </summary>
    public IReadOnlyList<TimingPoint> TimingPoints { get; }

    /// <summary>
    ///     Gets the complete live hit-object section without editor-only selection state.
    /// </summary>
    public IReadOnlyList<HitObject> HitObjects { get; }

    /// <summary>Gets the separate live selection snapshot captured from the editor.</summary>
    public IReadOnlyList<HitObject> SelectedHitObjects { get; }

    /// <summary>
    ///     Gets the live preview timestamp in milliseconds.
    /// </summary>
    public int PreviewTime { get; }

    /// <summary>
    ///     Gets the live base slider velocity.
    /// </summary>
    public double SliderMultiplier { get; }

    /// <summary>
    ///     Gets the live number of slider ticks per beat.
    /// </summary>
    public double SliderTickRate { get; }

    /// <summary>
    ///     Gets the live editor playhead in milliseconds, when the platform reader
    ///     can supply it.
    /// </summary>
    public double? EditorTime { get; }
}

