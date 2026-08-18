using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.TumourGenerator;

/// <summary>Chooses which objects are imported or transformed by Tumour Generator 2.</summary>
public enum TumourImportMode
{
    /// <summary>Uses the objects selected in the live osu! editor.</summary>
    Selected,

    /// <summary>Uses objects covered by saved bookmarks.</summary>
    Bookmarked,

    /// <summary>Uses objects matched by the time-code expression.</summary>
    Time,

    /// <summary>Uses every object in the beatmap.</summary>
    Everything
}

/// <summary>Stores Tumour Generator 2 persistence and preview state.</summary>
public sealed class TumourGeneratorProject : TumourGeneratorOptions
{
    /// <summary>Creates a project with the legacy preview slider and one default layer.</summary>
    public TumourGeneratorProject()
    {
        PreviewHitObject = new HitObject("0,0,0,2,0,L|256:0,1,256");
        ImportModeSetting = TumourImportMode.Selected;
        CurrentLayerIndex = 0;
        CircleSize = 4;
        TumourLayers.Add(TumourLayer.GetDefaultLayer());
    }

    /// <summary>Gets or sets the slider currently displayed in the preview.</summary>
    public HitObject PreviewHitObject { get; set; }

    /// <summary>Gets or sets the object-selection source used by import and run.</summary>
    public TumourImportMode ImportModeSetting { get; set; }

    /// <summary>Gets or sets the time-code expression used in time mode.</summary>
    public string TimeCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected layer index.</summary>
    public int CurrentLayerIndex { get; set; }

    /// <summary>Gets or sets whether advanced graph controls are visible.</summary>
    public bool AdvancedOptions { get; set; }

    /// <summary>Gets or sets the Circle Size used to draw the preview.</summary>
    public double CircleSize { get; set; }
}

/// <summary>Reports imported sliders and the map's Circle Size.</summary>
/// <param name="Sliders">The selected, bookmarked, time-filtered, or complete slider list.</param>
/// <param name="CircleSize">The map difficulty Circle Size used by the preview.</param>
/// <param name="UsedLiveEditor">Whether unsaved editor state supplied the import.</param>
public sealed record TumourImportResult(
    IReadOnlyList<HitObject> Sliders,
    double CircleSize,
    bool UsedLiveEditor);

/// <summary>Reports one completed preview generation.</summary>
/// <param name="HitObject">The independently generated preview slider.</param>
/// <param name="LayerLengths">The lengths observed at active layer boundaries.</param>
public sealed record TumourPreviewResult(
    HitObject HitObject,
    IReadOnlyList<double> LayerLengths);

/// <summary>Reports the number of transformed sliders across all requested maps.</summary>
/// <param name="Paths">The paths saved by the operation.</param>
/// <param name="SlidersTumourated">The number of sliders whose paths changed.</param>
/// <param name="EditorReloaded">Whether a live editor reload was requested.</param>
public sealed record TumourRunResult(
    IReadOnlyList<string> Paths,
    int SlidersTumourated,
    bool EditorReloaded);

/// <summary>Coordinates Tumour Generator 2 import, preview, and destructive runs.</summary>
public interface ITumourGeneratorService
{
    /// <summary>Imports sliders through the shared disk/live beatmap gateway.</summary>
    /// <param name="path">The beatmap path to inspect.</param>
    /// <param name="mode">The import source.</param>
    /// <param name="timeCode">The time query when <paramref name="mode"/> is time-based.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The imported sliders and preview difficulty value.</returns>
    Task<TumourImportResult> ImportAsync(
        string path,
        TumourImportMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default);

    /// <summary>Generates an independent preview without persistence or editor access.</summary>
    /// <param name="previewHitObject">The preview slider to copy and transform.</param>
    /// <param name="options">The framework-neutral tumour settings.</param>
    /// <param name="cancellationToken">Cancels the generation.</param>
    /// <returns>The generated slider and layer-length metadata.</returns>
    Task<TumourPreviewResult> PreviewAsync(
        HitObject previewHitObject,
        TumourGeneratorOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>Runs, backs up, saves, and optionally reloads the requested maps.</summary>
    /// <param name="paths">The beatmaps to transform.</param>
    /// <param name="project">The complete settings snapshot.</param>
    /// <param name="reloadEditor">Whether a live source editor should be reloaded.</param>
    /// <param name="progress">Optional percentage progress receiver.</param>
    /// <param name="cancellationToken">Cancels between objects and save stages.</param>
    /// <returns>The transformed paths and slider count.</returns>
    Task<TumourRunResult> RunAsync(
        IReadOnlyList<string> paths,
        TumourGeneratorProject project,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
