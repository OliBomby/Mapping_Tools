using Mapping_Tools.Application.Updates.Models;

namespace Mapping_Tools.Application.Updates.Contracts;

/// <summary>
///     Coordinates update-channel policy, persisted skip-version behavior, and
///     the staged-package lifecycle without referencing a UI or platform API.
/// </summary>
public interface IUpdateService : IDisposable
{
    /// <summary>Gets the most recent check result, or <see langword="null" /> before a check.</summary>
    UpdateCheckResult? LastCheck { get; }

    /// <summary>Gets the current preparation task when a download is active or has completed.</summary>
    Task? ActiveDownloadTask { get; }

    /// <summary>Fires whenever package preparation reports a new progress value.</summary>
    event EventHandler<UpdateProgressChangedEventArgs>? ProgressChanged;

    /// <summary>
    ///     Checks the release channel and applies the persisted skipped-version policy.
    /// </summary>
    /// <param name="allowSkippedVersion">Suppresses versions at or below the persisted skip version when true.</param>
    /// <param name="cancellationToken">Cancels the check and metadata request.</param>
    /// <returns>The typed check result used by the updater UI.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool allowSkippedVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Starts preparing the version from the last successful available check.
    ///     Reuses an already-running preparation task so Wait and shutdown share one download.
    /// </summary>
    /// <param name="cancellationToken">Cancels the package preparation.</param>
    /// <returns>The shared preparation task.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled while preparing the package.</exception>
    Task PrepareUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>Stores the currently offered version as the user's skipped version.</summary>
    void SkipCurrentVersion();

    /// <summary>
    ///     Starts the staged external updater and clears the wait-after-close state.
    /// </summary>
    /// <param name="restartAfterUpdate">Restarts the application after replacement when true.</param>
    void StartUpdateProcess(bool restartAfterUpdate);

    /// <summary>
    ///     Discards the current check and preparation state without changing persisted settings.
    /// </summary>
    void AbandonUpdate();
}

