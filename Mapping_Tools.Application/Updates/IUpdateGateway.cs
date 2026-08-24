using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Updates;

/// <summary>
///     Provides the network, archive, staging, and process boundary used by the
///     application updater use case.
/// </summary>
public interface IUpdateGateway : IDisposable
{
    /// <summary>
    ///     Checks the configured release channel and selects the process-architecture package.
    /// </summary>
    /// <param name="cancellationToken">Cancels network and release-metadata work.</param>
    /// <returns>The running version, newest available version, release metadata, and asset name.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task<UpdatePackageInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads, validates, extracts, and stages one previously discovered package.
    /// </summary>
    /// <param name="version">The version returned by the last successful check.</param>
    /// <param name="progress">Receives normalized package-preparation progress.</param>
    /// <param name="cancellationToken">Cancels download, extraction, or staging before launch.</param>
    /// <exception cref="InvalidOperationException">The package cannot be prepared in the current updater state.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
    Task PrepareUpdateAsync(
        Version version,
        IProgress<double> progress,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Launches the external updater that applies the staged package after this process exits.
    /// </summary>
    /// <param name="version">The version prepared by <see cref="PrepareUpdateAsync" />.</param>
    /// <param name="restartAfterUpdate">Restarts the application with its original arguments after replacement.</param>
    /// <exception cref="InvalidOperationException">The package was not prepared or launch was requested twice.</exception>
    /// <exception cref="PlatformNotSupportedException">The selected deployment platform has no supported updater process.</exception>
    void LaunchUpdater(Version version, bool restartAfterUpdate);
}

