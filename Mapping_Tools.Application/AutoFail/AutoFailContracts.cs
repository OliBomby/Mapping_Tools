using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.AutoFail;

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

/// <summary>Contains the analysis result and retained edit state for a possible fix operation.</summary>
public sealed class AutoFailRun
{
    /// <summary>Creates a detached result that cannot apply fixes to a beatmap.</summary>
    /// <param name="analysis">The detected unloading objects and candidate fixes.</param>
    /// <param name="mapEndTime">The final timeline position in milliseconds.</param>
    public AutoFailRun(AutoFailAnalysis analysis, double mapEndTime)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        MapEndTime = mapEndTime;
    }

    internal AutoFailRun(
        AutoFailAnalysis analysis,
        double mapEndTime,
        BeatmapEditingSession session,
        AutoFailDetectorEngine detector)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        MapEndTime = mapEndTime;
        Session = session;
        Detector = detector;
    }

    /// <summary>Gets the detected unloading objects and candidate fixes.</summary>
    public AutoFailAnalysis Analysis { get; }

    /// <summary>Gets the final beatmap timeline position in milliseconds.</summary>
    public double MapEndTime { get; }
    internal BeatmapEditingSession? Session { get; }
    internal AutoFailDetectorEngine? Detector { get; }
}

/// <summary>Analyzes editable beatmaps for auto-fail conditions and applies selected repairs.</summary>
public interface IAutoFailService
{
    /// <summary>Opens and analyzes one beatmap using the supplied difficulty overrides.</summary>
    /// <param name="options">The path and analysis parameters.</param>
    /// <param name="cancellationToken">Cancels opening or analysis.</param>
    /// <returns>The analysis plus state required by a later repair.</returns>
    Task<AutoFailRun> AnalyzeAsync(
        AutoFailOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>Builds repair choices for a completed analysis.</summary>
    /// <param name="run">The analysis whose unload events should be repaired.</param>
    /// <param name="cancellationToken">Cancels fix-plan generation.</param>
    /// <returns>The available spinner insertion plans.</returns>
    IEnumerable<AutoFailFixPlan> GetFixPlans(
        AutoFailRun run,
        CancellationToken cancellationToken = default);

    /// <summary>Applies a selected repair and persists the edited beatmap.</summary>
    /// <param name="run">The analysis retaining the editable session.</param>
    /// <param name="plan">The repair plan to apply.</param>
    /// <param name="cancellationToken">Cancels repair or saving.</param>
    /// <returns>A task that completes after the repaired map is saved.</returns>
    Task ApplyFixAsync(
        AutoFailRun run,
        AutoFailFixPlan plan,
        CancellationToken cancellationToken = default);
}
