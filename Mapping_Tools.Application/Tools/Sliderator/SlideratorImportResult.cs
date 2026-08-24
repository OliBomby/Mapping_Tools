using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.Sliderator;

namespace Mapping_Tools.Application.Tools.Sliderator;

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

