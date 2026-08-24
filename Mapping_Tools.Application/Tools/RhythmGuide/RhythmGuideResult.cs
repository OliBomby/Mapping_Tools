using Mapping_Tools.Core.Tools.RhythmGuide;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Reports the destination and number of guide objects produced by one run.</summary>
/// <param name="ExportPath">The generated or modified beatmap path.</param>
/// <param name="AddedObjectCount">The number of guide objects added.</param>
/// <param name="ExportMode">Whether the operation created or extended a beatmap.</param>
public sealed record RhythmGuideResult(
    string ExportPath,
    int AddedObjectCount,
    RhythmGuideExportMode ExportMode);

