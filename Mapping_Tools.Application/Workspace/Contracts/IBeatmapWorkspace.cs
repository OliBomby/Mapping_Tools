using Mapping_Tools.Application.Workspace.Models;

namespace Mapping_Tools.Application.Workspace.Contracts;

/// <summary>
///     Owns the ordered beatmap selection and recent-map history shared by tools,
///     independently of the application window.
/// </summary>
public interface IBeatmapWorkspace
{
    /// <summary>
    ///     Gets a snapshot of the selected local paths in caller-supplied order.
    /// </summary>
    IReadOnlyList<string> SelectedPaths { get; }

    /// <summary>
    ///     Gets the persisted history ordered from most to least recently selected.
    /// </summary>
    IReadOnlyList<RecentBeatmap> RecentMaps { get; }

    /// <summary>
    ///     Notifies consumers after every explicit selection or clear operation,
    ///     including a selection whose paths equal the previous value.
    /// </summary>
    event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    ///     Restores the newest legacy recent entry at startup and promotes it with
    ///     the current timestamp, matching the former WPF startup behavior.
    /// </summary>
    /// <returns>
    ///     <see langword="true" /> when a non-blank recent path was restored;
    ///     otherwise <see langword="false" />.
    /// </returns>
    bool RestoreMostRecent();

    /// <summary>
    ///     Replaces the ordered selection, records every non-blank path in recent
    ///     history, and publishes a change notification.
    /// </summary>
    /// <param name="paths">
    ///     Local paths in tool-consumption order. Blank elements are discarded;
    ///     an empty result clears the selection.
    /// </param>
    /// <param name="source">The user or platform action responsible for the change.</param>
    void SetSelection(
        IEnumerable<string> paths,
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic);

    /// <summary>
    ///     Clears selected paths without deleting recent-map history.
    /// </summary>
    /// <param name="source">The action responsible for clearing the selection.</param>
    void ClearSelection(
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic);

    /// <summary>
    ///     Removes every recent entry whose path exactly matches the supplied path.
    /// </summary>
    /// <param name="path">The recorded path to forget.</param>
    /// <returns>Whether at least one entry was removed.</returns>
    bool RemoveRecent(string path);

    /// <summary>
    ///     Returns selected paths that no longer resolve to local files without
    ///     changing the selection.
    /// </summary>
    /// <returns>Missing paths in selection order, including repeated entries.</returns>
    IReadOnlyList<string> GetMissingSelectedPaths();

    /// <summary>
    ///     Presents the shared osu!/storyboard picker and installs its result.
    /// </summary>
    /// <param name="allowMultiple">Whether more than one file may be selected.</param>
    /// <param name="cancellationToken">
    ///     Cancels picker result processing; an already-visible native picker may remain open.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> when files were selected; <see langword="false" />
    ///     when the user cancelled.
    /// </returns>
    Task<bool> PickBeatmapsAsync(
        bool allowMultiple,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Attempts to replace the selection with the beatmap reported by osu!.
    /// </summary>
    /// <param name="cancellationToken">Cancels live beatmap discovery.</param>
    /// <returns>A status distinguishing selection, unavailable lookup, and a stale path.</returns>
    Task<CurrentBeatmapSelectionResult> SelectCurrentBeatmapAsync(
        CancellationToken cancellationToken = default);
}

