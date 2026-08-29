using Mapping_Tools.Application.Projects.Models;

namespace Mapping_Tools.Application.Projects.Contracts;

/// <summary>
///     Coordinates typed project lifecycle operations, autosave targets, and
///     platform file pickers without depending on a view or control instance.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///     Resolves the feature's automatic recovery file beneath application data.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">The feature's persistence metadata.</param>
    /// <returns>The absolute autosave path.</returns>
    string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition);

    /// <summary>
    ///     Resolves the directory offered by the feature's Open and Save As dialogs.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">The feature's persistence metadata.</param>
    /// <returns>The absolute project directory.</returns>
    string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition);

    /// <summary>
    ///     Creates clean feature state after the caller has handled any discard
    ///     confirmation.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">Supplies the factory that establishes feature defaults.</param>
    /// <returns>A new, fully initialized project.</returns>
    /// <exception cref="InvalidOperationException">The project factory returns null.</exception>
    TProject CreateNew<TProject>(ProjectDefinition<TProject> definition);

    /// <summary>
    ///     Writes a project to an explicit path, for example a collection export target.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="path">The destination JSON file.</param>
    /// <param name="project">The complete project snapshot.</param>
    /// <param name="cancellationToken">Cancels the write before it is committed.</param>
    Task SaveAsync<TProject>(
        string path,
        TProject project,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads a typed project from an explicit path without mutating presentation state.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="path">The existing JSON file.</param>
    /// <param name="cancellationToken">Cancels reading before deserialization begins.</param>
    /// <returns>The reconstructed project for the caller to validate and install.</returns>
    Task<TProject> LoadAsync<TProject>(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Writes the primary recovery file followed by any feature-specific
    ///     recovery targets, preserving target order.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">Identifies the project and its primary recovery file.</param>
    /// <param name="project">The snapshot written identically to every target.</param>
    /// <param name="additionalPaths">
    ///     Optional absolute targets such as Pattern Gallery's active collection file.
    ///     Duplicate paths are written only once.
    /// </param>
    /// <param name="cancellationToken">Stops before the next target is written.</param>
    Task AutoSaveAsync<TProject>(
        ProjectDefinition<TProject> definition,
        TProject project,
        IEnumerable<string>? additionalPaths = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Presents a project Save As picker and writes only when the user selected a path.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">Supplies the initial project directory and default filename.</param>
    /// <param name="project">The snapshot to save.</param>
    /// <param name="suggestedFileName">An optional filename proposed by the native dialog.</param>
    /// <param name="cancellationToken">
    ///     Cancels result processing; an already-visible native picker may remain open.
    /// </param>
    /// <returns>The selected path, or <see langword="null" /> when the user cancels.</returns>
    Task<string?> SaveAsAsync<TProject>(
        ProjectDefinition<TProject> definition,
        TProject project,
        string? suggestedFileName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Presents a project Open picker and returns loaded data without installing it
    ///     in a view model.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="definition">Supplies the initial project directory.</param>
    /// <param name="cancellationToken">
    ///     Cancels result processing; an already-visible native picker may remain open.
    /// </param>
    /// <returns>The selected path and typed project, or <see langword="null" /> when cancelled.</returns>
    Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
        ProjectDefinition<TProject> definition,
        CancellationToken cancellationToken = default);
}
