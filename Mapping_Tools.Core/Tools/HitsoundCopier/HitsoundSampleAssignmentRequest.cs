using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.HitsoundCopier;

/// <summary>Describes one unmatched hitsound that may become a generated sample.</summary>
/// <param name="SourceFilenames">Canonical source audio paths played by the event.</param>
/// <param name="Role">The generated sample role, such as <c>slidertick</c>.</param>
/// <param name="SampleSet">The source sample family.</param>
/// <param name="StartIndex">The first custom index to consider.</param>
public sealed record HitsoundSampleAssignmentRequest(
    IReadOnlyList<string> SourceFilenames,
    string Role,
    SampleSet SampleSet,
    int StartIndex);

