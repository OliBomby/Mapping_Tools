using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;

namespace Mapping_Tools.Application.Tools.AutoFail;

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
