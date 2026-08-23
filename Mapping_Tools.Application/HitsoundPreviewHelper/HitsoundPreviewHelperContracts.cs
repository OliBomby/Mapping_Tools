using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;

namespace Mapping_Tools.Application.HitsoundPreviewHelper;

/// <summary>
///     Persists the complete hitsound-preview form while retaining the legacy
///     <c>Items</c> zone property used by WPF project files.
/// </summary>
public sealed class HitsoundPreviewHelperProject : HitsoundPreviewHelperOptions
{
}

/// <summary>Reports the maps and timeline events changed by one preview run.</summary>
/// <param name="ProcessedPaths">The input paths that were opened and saved.</param>
/// <param name="UpdatedEventCount">The total number of timeline events updated.</param>
public sealed record HitsoundPreviewHelperResult(
    IReadOnlyList<string> ProcessedPaths,
    int UpdatedEventCount);

/// <summary>Coordinates hitsound preview editing through the shared gateway.</summary>
public interface IHitsoundPreviewHelperService
{
    /// <summary>
    ///     Applies positional rules to each input map and saves every changed map
    ///     through the backup-aware editor gateway.
    /// </summary>
    /// <param name="paths">Beatmap paths in the shell's selected order.</param>
    /// <param name="options">The persisted object-selection and zone settings.</param>
    /// <param name="progress">Optional percentage progress receiver.</param>
    /// <param name="cancellationToken">Cancels loading, mutation, or persistence.</param>
    /// <returns>The processed paths and total updated event count.</returns>
    Task<HitsoundPreviewHelperResult> ApplyAsync(
        IReadOnlyList<string> paths,
        HitsoundPreviewHelperOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Reads selected live-editor object positions for the Shift-add workflow.
    /// </summary>
    /// <param name="path">The beatmap currently open in the editor.</param>
    /// <param name="cancellationToken">Cancels the live-state read.</param>
    /// <returns>Distinct selected positions, with Y set to -1 for mania maps.</returns>
    Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
        string path,
        CancellationToken cancellationToken = default);
}
