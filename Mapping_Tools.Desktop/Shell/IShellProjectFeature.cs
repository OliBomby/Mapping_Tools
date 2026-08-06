namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Exposes the project operations owned by an active feature without placing
/// feature models or persistence details in the shell.
/// </summary>
public interface IShellProjectFeature
{
    /// <summary>
    /// Saves the feature's current typed project through its configured project service.
    /// </summary>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    Task SaveProjectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a typed project and installs it only after the feature validates the loaded state.
    /// </summary>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    Task OpenProjectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles discard confirmation and replaces current state with feature defaults when accepted.
    /// </summary>
    /// <param name="cancellationToken">Cancels confirmation or project initialization.</param>
    Task NewProjectAsync(CancellationToken cancellationToken = default);
}
