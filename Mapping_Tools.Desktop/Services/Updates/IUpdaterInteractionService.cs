namespace Mapping_Tools.Desktop.Services.Updates;

/// <summary>
///     Bridges the Application updater lifecycle to owner-modal and modeless
///     Avalonia windows without exposing an Avalonia type to Application.
/// </summary>
public interface IUpdaterInteractionService : IDisposable
{
    /// <summary>
    ///     Gets whether the shell must finish a wait-after-close update before it exits.
    /// </summary>
    bool ShouldUpdateOnClose { get; }

    /// <summary>
    ///     Checks the release channel and shows the legacy decision window for an available update.
    /// </summary>
    /// <param name="allowSkippedVersion">Suppresses the persisted skipped version for startup checks when true.</param>
    /// <param name="notifyUser">Shows no-update and skipped-version messages for a manual check.</param>
    /// <param name="cancellationToken">Cancels network and release-metadata work.</param>
    Task CheckForUpdatesAsync(
        bool allowSkippedVersion,
        bool notifyUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Completes a wait-after-close download, showing the legacy progress dialog
    ///     when preparation is not already finished, then launches the updater.
    /// </summary>
    /// <param name="cancellationToken">Cancels the shutdown wait and package preparation.</param>
    /// <returns><see langword="true" /> when the owner may close; otherwise the update remains pending.</returns>
    Task<bool> CompleteUpdateOnCloseAsync(CancellationToken cancellationToken = default);
}
