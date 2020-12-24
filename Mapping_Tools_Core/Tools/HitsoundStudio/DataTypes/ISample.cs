using System;
using Mapping_Tools_Core.BeatmapHelper.Enums;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes {
    /// <summary>
    /// Represents a hitsound sample in osu!
    /// with the addition of an outside volume parameter.
    /// </summary>
    public interface ISample : ICloneable {
        ISampleGeneratingArgs SampleGeneratingArgs { get; }
        int Priority { get; }
        double OutsideVolume { get; set; }
        SampleSet SampleSet { get; }
        Hitsound Hitsound { get; }
    }
}