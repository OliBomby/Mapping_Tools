using Mapping_Tools.Core.Tools.TimingHelper;

namespace Mapping_Tools.Application.Tools.TimingHelper;

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

