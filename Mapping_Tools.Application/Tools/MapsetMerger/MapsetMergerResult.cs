using Mapping_Tools.Core.Tools.MapsetMerger;

namespace Mapping_Tools.Application.Tools.MapsetMerger;

/// <summary>Reports the files emitted by one successful merge.</summary>
/// <param name="MapsetsMerged">Number of source mapsets processed.</param>
/// <param name="BeatmapsWritten">Number of merged <c>.osu</c> files written.</param>
/// <param name="StoryboardsWritten">Number of external <c>.osb</c> files written.</param>
/// <param name="AssetsCopied">Number of binary asset files copied.</param>
public sealed record MapsetMergerResult(
    int MapsetsMerged,
    int BeatmapsWritten,
    int StoryboardsWritten,
    int AssetsCopied);

