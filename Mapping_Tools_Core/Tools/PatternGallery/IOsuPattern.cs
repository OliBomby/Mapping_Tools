using System;
using Mapping_Tools_Core.BeatmapHelper;

namespace Mapping_Tools_Core.Tools.PatternGallery {
    public interface IOsuPattern {
        string Name { get; set; }
        DateTime CreationTime { get; set; }
        DateTime LastUsedTime { get; set; }
        int UseCount { get; set; }
        string Filename { get; set; }
        int ObjectCount { get; set; }
        TimeSpan Duration { get; set; }
        double BeatLength { get; set; }
    }
}