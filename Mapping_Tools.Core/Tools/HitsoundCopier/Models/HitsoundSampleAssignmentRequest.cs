using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Tools.HitsoundCopier.Models;

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

