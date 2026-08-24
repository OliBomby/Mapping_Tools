namespace Mapping_Tools.Application.Projects;

/// <summary>
///     Supplies the type-erased persistence operations needed by the desktop shell
///     for one typed project definition.
/// </summary>
public interface IProjectDefinition
{
    /// <summary>Gets the legacy-compatible automatic recovery filename.</summary>
    string AutoSaveFileName { get; }

    /// <summary>Gets the application-data folder used by project pickers.</summary>
    string ProjectFolderName { get; }

    /// <summary>Gets the filename proposed by Save As, or <see langword="null" />.</summary>
    string? SuggestedFileName { get; }

    /// <summary>Creates a new project through the typed definition factory.</summary>
    /// <returns>A fully initialized project instance.</returns>
    object CreateProject();

    /// <summary>Persists an object after checking that it matches the definition's type.</summary>
    /// <param name="store">The typed project store used for serialization.</param>
    /// <param name="path">The destination JSON file.</param>
    /// <param name="project">The project instance to persist.</param>
    /// <param name="cancellationToken">Cancels serialization or writing.</param>
    /// <returns>A task that completes after the document is written.</returns>
    Task SaveAsync(
        IProjectStore store,
        string path,
        object project,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a project using the definition's concrete project type.</summary>
    /// <param name="store">The typed project store used for deserialization.</param>
    /// <param name="path">The existing JSON file.</param>
    /// <param name="cancellationToken">Cancels reading or deserialization.</param>
    /// <returns>A task containing the reconstructed project.</returns>
    Task<object> LoadAsync(
        IProjectStore store,
        string path,
        CancellationToken cancellationToken = default);
}

