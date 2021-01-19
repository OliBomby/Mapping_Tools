using System;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public class StoryboardLayerSourceRef : IStoryboardLayerSourceRef {
        public StoryboardLayerSourceRef(string path, string samplePath, double volume, bool discriminateVolumes, bool detectDuplicateSamples) {
            Path = path;
            SamplePath = samplePath;
            Volume = volume;
            DiscriminateVolumes = discriminateVolumes;
            DetectDuplicateSamples = detectDuplicateSamples;
        }

        public bool Equals(ILayerSourceRef other) {
            return other is IStoryboardLayerSourceRef o &&
                   Path == o.Path &&
                   SamplePath == o.SamplePath &&
                   Math.Abs(Volume - o.Volume) < Precision.DOUBLE_EPSILON &&
                   DiscriminateVolumes == o.DiscriminateVolumes &&
                   DetectDuplicateSamples == o.DetectDuplicateSamples;
        }

        public ILayerImportArgs GetLayerImportArgs() {
            return new StoryboardLayerImportArgs(Path, DiscriminateVolumes, DetectDuplicateSamples);
        }

        public bool ReloadCompatible(ILayerSourceRef other) {
            return other is IStoryboardLayerSourceRef o  && 
                   Path == o.Path && 
                   SamplePath == o.SamplePath && 
                   (!DiscriminateVolumes || Math.Abs(Volume - o.Volume) < Precision.DOUBLE_EPSILON);
        }

        public string Path { get; }
        public string SamplePath { get; }
        public double Volume { get; }
        public bool DiscriminateVolumes { get; }
        public bool DetectDuplicateSamples { get; }
    }
}