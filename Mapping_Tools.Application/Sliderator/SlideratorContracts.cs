using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.Sliderator;

namespace Mapping_Tools.Application.Sliderator;

/// <summary>Stores Sliderator's persisted generation settings.</summary>
public sealed class SlideratorProject : SlideratorOptions
{
}

/// <summary>Reports the imported slider candidates and their map multiplier.</summary>
/// <param name="Sliders">The selected slider objects in editor order.</param>
/// <param name="GlobalSv">The map's base slider multiplier.</param>
/// <param name="UsedLiveEditor">Whether the imported source came from a live editor overlay.</param>
/// <param name="PreferLiveEditor">Whether the next run should refresh from the live editor when available.</param>
public sealed record SlideratorImportResult(
    IReadOnlyList<HitObject> Sliders,
    double GlobalSv,
    bool UsedLiveEditor,
    bool PreferLiveEditor);

/// <summary>Reports the written Sliderator beatmap and generated dimensions.</summary>
/// <param name="Path">The beatmap path written by the operation.</param>
/// <param name="Applied">The Core generation result.</param>
/// <param name="EditorReloaded">Whether the live editor was requested to reload.</param>
public sealed record SlideratorResult(
    string Path,
    SlideratorApplyResult Applied,
    bool EditorReloaded);

/// <summary>Runs Sliderator through the shared editor, backup, and persistence ports.</summary>
public interface ISlideratorService
{
    /// <summary>Imports sliders using the selected editor/bookmark/time mode.</summary>
    /// <param name="path">The beatmap path to inspect.</param>
    /// <param name="mode">The source-object selection mode.</param>
    /// <param name="timeCode">The time-code query for time mode.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The selected sliders and map multiplier.</returns>
    Task<SlideratorImportResult> ImportAsync(
        string path,
        SlideratorImportMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default);

    /// <summary>Generates one output from the imported source slider and saves it safely.</summary>
    /// <param name="path">The target beatmap path.</param>
    /// <param name="project">The complete run settings.</param>
    /// <param name="sourceSlider">The selected source slider geometry.</param>
    /// <param name="reloadEditor">Whether a live editor should be refreshed after saving.</param>
    /// <param name="progress">Optional percentage progress receiver.</param>
    /// <param name="cancellationToken">Cancels generation or persistence.</param>
    /// <param name="preferLiveEditor">Whether the application should prefer unsaved editor state for this run.</param>
    /// <returns>The written path and Core output result.</returns>
    Task<SlideratorResult> RunAsync(
        string path,
        SlideratorProject project,
        HitObject sourceSlider,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        bool preferLiveEditor = true);
}

/// <summary>
///     Provides the small frontend-owned interaction needed by Shift navigation:
///     run the current slider through the editor and complete when placement ends.
/// </summary>
public interface ISlideratorInteraction
{
    /// <summary>Runs the current Sliderator placement and waits for its terminal result.</summary>
    /// <param name="cancellationToken">Cancels the placement wait.</param>
    /// <returns><see langword="true" /> when placement completed successfully; otherwise, <see langword="false" />.</returns>
    Task<bool> RunFastAsync(CancellationToken cancellationToken = default);
}
