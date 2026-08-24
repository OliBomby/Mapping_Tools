namespace Mapping_Tools.Application.Projects.Contracts;

/// <summary>
///     Persists typed project documents without exposing a JSON library or
///     frontend-owned state container to application use cases.
/// </summary>
public interface IProjectStore
{
    /// <summary>
    ///     Ensures a picker start location exists before a native dialog is presented.
    /// </summary>
    /// <param name="path">The local directory to create when absent.</param>
    void EnsureDirectoryExists(string path);

    /// <summary>
    ///     Atomically replaces a local project file with a serialized snapshot.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model.</typeparam>
    /// <param name="path">The destination JSON file.</param>
    /// <param name="project">The complete project snapshot.</param>
    /// <param name="cancellationToken">
    ///     Cancels serialization or writing before the replacement is committed.
    /// </param>
    /// <returns>A task that completes after the destination contains the full document.</returns>
    Task SaveAsync<TProject>(
        string path,
        TProject project,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads and deserializes a typed project from a local file.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project model expected by the caller.</typeparam>
    /// <param name="path">The existing JSON file.</param>
    /// <param name="cancellationToken">Cancels reading before deserialization begins.</param>
    /// <returns>The reconstructed project.</returns>
    Task<TProject> LoadAsync<TProject>(
        string path,
        CancellationToken cancellationToken = default);
}
