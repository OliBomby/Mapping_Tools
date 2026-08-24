using Mapping_Tools.Core.Tools.SliderMerger;

namespace Mapping_Tools.Application.Tools.SliderMerger;

/// <summary>Reports processed maps and source objects incorporated into merged sliders.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="ObjectsMerged">The number of source objects incorporated into merged sliders.</param>
public sealed record SliderMergerResult(
    IReadOnlyList<string> ProcessedPaths,
    int ObjectsMerged);

