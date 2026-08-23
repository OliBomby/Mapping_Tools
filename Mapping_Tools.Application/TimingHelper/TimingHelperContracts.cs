using Mapping_Tools.Core.Tools.TimingHelper;

namespace Mapping_Tools.Application.TimingHelper;

/// <summary>
///     Represents the complete Timing Helper project persisted by the shell.
/// </summary>
public sealed class TimingHelperProject : TimingHelperOptions
{
}

/// <summary>Reports the redlines added to each processed beatmap.</summary>
/// <param name="ProcessedPaths">The processed paths in selection order.</param>
/// <param name="RedlinesAdded">The total number of inserted redlines.</param>
public sealed record TimingHelperResult(
    IReadOnlyList<string> ProcessedPaths,
    int RedlinesAdded)
{
    /// <summary>Gets the number of beatmaps written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

/// <summary>
///     Loads beatmaps, applies Timing Helper, and persists every result through the
///     shared backup-safe editing gateway.
/// </summary>
public interface ITimingHelperService
{
    /// <summary>
    ///     Adjusts the supplied beatmaps using the configured marker sources.
    /// </summary>
    /// <param name="paths">The beatmaps to process.</param>
    /// <param name="options">The marker sources and timing rules.</param>
    /// <param name="progress">Receives aggregate completion percentages.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The processed paths and total inserted redlines.</returns>
    Task<TimingHelperResult> AdjustAsync(
        IReadOnlyList<string> paths,
        TimingHelperOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
