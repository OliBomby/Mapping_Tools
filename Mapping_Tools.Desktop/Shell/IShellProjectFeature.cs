using Mapping_Tools.Application.Projects;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Exposes the typed project state owned by an active feature to the shared
/// shell project coordinator.
/// </summary>
public interface IShellProjectFeature
{
    /// <summary>
    /// Gets the persistence metadata used for autosave and project pickers.
    /// </summary>
    IProjectDefinition ProjectDefinition { get; }

    /// <summary>
    /// Gets optional feature-owned recovery files that should receive the same
    /// snapshot as the shell autosave, such as a collection-local project file.
    /// </summary>
    IReadOnlyList<string> AdditionalAutoSavePaths => [];

    /// <summary>
    /// Captures the complete current project state for persistence.
    /// </summary>
    /// <returns>A project instance matching <see cref="ProjectDefinition"/>.</returns>
    object Snapshot();

    /// <summary>
    /// Installs a project after the shared coordinator has loaded or created it.
    /// </summary>
    /// <param name="project">A project instance matching <see cref="ProjectDefinition"/>.</param>
    void Install(object project);
}
