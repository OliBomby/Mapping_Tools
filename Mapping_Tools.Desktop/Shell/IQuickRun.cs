namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Exposes the execution path used when the shell makes a feature the current
///     QuickRun target. The command identity comes from application tool metadata.
/// </summary>
public interface IQuickRun
{
    /// <summary>
    ///     Runs the feature against the beatmap currently open in osu!.
    /// </summary>
    /// <param name="cancellationToken">Cancels beatmap discovery and execution.</param>
    /// <returns>A task that completes when the QuickRun operation reaches a terminal state.</returns>
    Task RunQuickAsync(CancellationToken cancellationToken);
}
