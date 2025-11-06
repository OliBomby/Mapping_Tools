using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mapping_Tools.Core.BeatmapHelper.Sections;

public class SectionEditor {
    public double DistanceSpacing { get; set; } = 0.8;
    public int BeatDivisor { get; set; } = 1;
    public int GridSize { get; set; } = 32;
    public float TimelineZoom { get; set; } = 1f;

    [NotNull]
    public List<double> Bookmarks { get; set; } = new List<double>();
}