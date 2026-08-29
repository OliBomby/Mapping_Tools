namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Exposes a project-bearing feature to the heterogeneous desktop shell
///     without erasing the feature's project type.
/// </summary>
public interface IShellProjectFeature
{
    /// <summary>
    ///     Dispatches an operation to the feature's concrete project type.
    /// </summary>
    /// <param name="operation">The strongly typed operation to execute.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes after the operation has finished.</returns>
    Task ExecuteProjectOperationAsync(
        IProjectFeatureOperation operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets optional feature-owned recovery files that receive the same
    ///     snapshot as the primary shell autosave.
    /// </summary>
    IReadOnlyList<string> AdditionalAutoSavePaths => [];
}
