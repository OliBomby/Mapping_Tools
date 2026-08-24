using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.Tools.AutoFail;

/// <summary>Defines the beatmap and difficulty values used for one auto-fail analysis.</summary>
/// <param name="Path">The beatmap file to analyze.</param>
/// <param name="ApproachRateOverride">The approach rate to simulate, or -1 to use the map value.</param>
/// <param name="OverallDifficultyOverride">The overall difficulty to simulate, or -1 to use the map value.</param>
/// <param name="PhysicsUpdateLeniency">The tolerated physics-update delay in milliseconds.</param>
public sealed record AutoFailOptions(
    string Path,
    double ApproachRateOverride = -1,
    double OverallDifficultyOverride = -1,
    int PhysicsUpdateLeniency = 9);

