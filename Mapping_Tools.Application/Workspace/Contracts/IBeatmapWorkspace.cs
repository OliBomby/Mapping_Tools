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
    ///     Publishes the legacy missing-file warning when one or more selected
    ///     paths no longer resolve to local files.
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
    ///     Gets the initial directory for a default beatmap picker. When the
    ///     current-beatmap preference is enabled, this is the first selected
    ///     beatmap's parent directory, or the configured Songs directory when
    ///     no selected beatmap directory is available. When the preference is
    ///     disabled, the supplied current picker directory is retained.
    /// </summary>
    /// <param name="currentDirectory">
    ///     The directory represented by the picker's current value, or
    ///     <see langword="null" /> when it has no current directory.
    /// </param>
    /// <returns>The shared default picker directory, or <see langword="null" />.</returns>
    string? GetBeatmapPickerStartLocation(string? currentDirectory = null);

    /// <summary>
    ///     Resolves the beatmap used by legacy-compatible QuickRun. The beatmap
    ///     currently open in osu! wins when it can be resolved to an existing
    ///     file; otherwise the first shell-selected beatmap is returned.
    /// </summary>
    /// <param name="updateSelection">
    ///     Whether a resolved current beatmap should replace the shell selection.
    /// </param>
    /// <param name="cancellationToken">Cancels live beatmap discovery.</param>
    /// <returns>
    ///     The current editor beatmap, the first selected beatmap as fallback, or
    ///     an empty string when neither is available.
    /// </returns>
    Task<string> ResolveQuickRunBeatmapAsync(
        bool updateSelection = true,
        CancellationToken cancellationToken = default);
}
