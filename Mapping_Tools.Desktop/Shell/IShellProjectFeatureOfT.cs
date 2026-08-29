using Mapping_Tools.Application.Projects.Models;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Exposes strongly typed project state and persistence metadata for one
///     shell feature.
/// </summary>
/// <typeparam name="TProject">The complete project state owned by the feature.</typeparam>
public interface IShellProjectFeature<TProject> : IShellProjectFeature
{
    /// <summary>Gets the persistence metadata for the feature's project type.</summary>
    ProjectDefinition<TProject> ProjectDefinition { get; }

    /// <summary>Captures the complete current project state.</summary>
    /// <returns>A project snapshot that matches <see cref="ProjectDefinition" />.</returns>
    TProject Snapshot();

    /// <summary>
    ///     Installs a loaded or newly created project after the shell has completed
    ///     the relevant confirmation and persistence operation.
    /// </summary>
    /// <param name="project">The project instance to install.</param>
    void Install(TProject project);

    Task IShellProjectFeature.ExecuteProjectOperationAsync(
        IProjectFeatureOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation.ExecuteAsync(this, cancellationToken);
    }
}
