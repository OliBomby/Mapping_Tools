using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>Reports the deterministic changes made by one target-map copy.</summary>
/// <param name="MatchedHitsoundCount">The number of source events matched to target events.</param>
/// <param name="GeneratedSampleCount">The number of new sample entries created for unmatched events.</param>
/// <param name="MutedEdgeCount">The number of target edge events muted by the filter.</param>
/// <param name="SampleSchema">The newly added sample requirements.</param>
public sealed record HitsoundCopierApplyResult(
    int MatchedHitsoundCount,
    int GeneratedSampleCount,
    int MutedEdgeCount,
    SampleSchema SampleSchema);

