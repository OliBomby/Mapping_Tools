using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Coordinates loading, saving, and optional osu! reloads without coupling
///     feature code to either the physical filesystem or Editor Reader.
/// </summary>
public interface IBeatmapEditingGateway
{
    /// <summary>
    ///     Opens an osu! beatmap and optionally overlays matching unsaved editor state.
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
    ///     Opens an on-disk storyboard; osu! does not expose storyboard state
    ///     through the live beatmap reader.
    /// </summary>
    /// <param name="path">The storyboard file to parse.</param>
    /// <param name="cancellationToken">Cancels before the disk read begins.</param>
    /// <returns>A mutable storyboard editor backed by the configured file store.</returns>
    Task<StoryboardEditor> OpenStoryboardAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Persists the editor's current document only after a mandatory safety
    ///     backup succeeds, then optionally asks osu! to reload it.
    /// </summary>
    /// <param name="editor">The beatmap or storyboard editor to save.</param>
    /// <param name="reloadEditor">Whether an active osu! editor should be refreshed after the write.</param>
    /// <param name="cancellationToken">Cancels before saving or before requesting the reload.</param>
    /// <returns>A task that completes after backup, persistence, and any requested reload.</returns>
    /// <exception cref="IOException">
    ///     The backup or document write fails; a backup failure leaves the source untouched.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     Cancellation occurs before backup, save, or a requested reload completes.
    /// </exception>
    Task SaveAsync(
        Editor editor,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Persists a beatmap session using the session's original live state for
    ///     the optional companion backup, then optionally reloads osu!.
    /// </summary>
    /// <param name="session">The opened beatmap session to save.</param>
    /// <param name="reloadEditor">Whether an active osu! editor should be refreshed after the write.</param>
    /// <param name="cancellationToken">Cancels before saving or before requesting the reload.</param>
    /// <returns>A task that completes after backup, persistence, and any requested reload.</returns>
    /// <exception cref="IOException">
    ///     The backup or document write fails; a backup failure leaves the source untouched.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    ///     Cancellation occurs before backup, save, or a requested reload completes.
    /// </exception>
    Task SaveAsync(
        BeatmapEditingSession session,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default);
}

