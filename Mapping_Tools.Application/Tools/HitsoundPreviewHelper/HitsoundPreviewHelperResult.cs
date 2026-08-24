using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

namespace Mapping_Tools.Application.Tools.HitsoundPreviewHelper;

/// <summary>Reports the maps and timeline events changed by one preview run.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="UpdatedEventCount">The total number of timeline events updated.</param>
public sealed record HitsoundPreviewHelperResult(
    IReadOnlyList<string> ProcessedPaths,
    int UpdatedEventCount);

