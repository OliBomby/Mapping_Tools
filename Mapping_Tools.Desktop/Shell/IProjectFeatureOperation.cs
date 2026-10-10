namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Performs one project lifecycle operation against a feature while retaining
///     that feature's concrete project type.
/// </summary>
public interface IProjectFeatureOperation
{
    /// <summary>
    ///     Executes the operation for a feature whose project type is known by the
    ///     compile-time generic method context.
    /// </summary>
    /// <typeparam name="TProject">The feature's complete project state.</typeparam>
    /// <param name="feature">The typed project feature receiving the operation.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task that completes after the operation has finished.</returns>
    Task ExecuteAsync<TProject>(
        IShellProjectFeature<TProject> feature,
        CancellationToken cancellationToken = default);
}
