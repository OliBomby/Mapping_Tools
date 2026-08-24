using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>Describes the custom index and schema entry assigned to an unmatched hitsound.</summary>
/// <param name="Index">The custom sample index assigned to the event.</param>
/// <param name="SampleSet">The sample family used by the assignment.</param>
/// <param name="Schema">Only the newly added sample entries, if any.</param>
public sealed record HitsoundSampleAssignment(
    int Index,
    SampleSet SampleSet,
    SampleSchema Schema);

