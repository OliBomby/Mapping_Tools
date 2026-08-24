using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.Sliderator.Models;

namespace Mapping_Tools.Application.Tools.Sliderator.Contracts;

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

