using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Reports the target maps and deterministic hitsound changes from a run.</summary>
/// <param name="ProcessedPaths">Target map paths successfully saved.</param>
/// <param name="MatchedHitsoundCount">The number of source events matched to target events.</param>
/// <param name="GeneratedSampleCount">The number of new sample entries created.</param>
/// <param name="MutedEdgeCount">The number of target edge events muted by the filter.</param>
/// <param name="SampleSchema">The generated sample requirements.</param>
public sealed record HitsoundCopierResult(
    IReadOnlyList<string> ProcessedPaths,
    int MatchedHitsoundCount,
    int GeneratedSampleCount,
    int MutedEdgeCount,
    SampleSchema SampleSchema)
{
    /// <summary>Gets the number of target maps written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

