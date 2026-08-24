using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>Reports imported sliders and the map's Circle Size.</summary>
/// <param name="Sliders">The selected, bookmarked, time-filtered, or complete slider list.</param>
/// <param name="CircleSize">The map difficulty Circle Size used by the preview.</param>
/// <param name="UsedLiveEditor">Whether unsaved editor state supplied the import.</param>
public sealed record TumourImportResult(
    IReadOnlyList<HitObject> Sliders,
    double CircleSize,
    bool UsedLiveEditor);

