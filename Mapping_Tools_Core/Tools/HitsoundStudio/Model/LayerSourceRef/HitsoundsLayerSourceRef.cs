using System;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public class HitsoundsLayerSourceRef : IHitsoundsLayerSourceRef {
        public HitsoundsLayerSourceRef(string path, string samplePath, double volume, bool discriminateVolumes, bool detectDuplicateSamples) {
            Path = path;
            SamplePath = samplePath;
            Volume = volume;
            DiscriminateVolumes = discriminateVolumes;
            DetectDuplicateSamples = detectDuplicateSamples;
        }

        public bool Equals(ILayerSourceRef other) {
            return other is IHitsoundsLayerSourceRef o &&
                   Path == o.Path &&
                   SamplePath == o.SamplePath &&
                   Math.Abs(Volume - o.Volume) < Precision.DOUBLE_EPSILON &&
                   DiscriminateVolumes == o.DiscriminateVolumes &&
                   DetectDuplicateSamples == o.DetectDuplicateSamples;
        }

        public ILayerImportArgs GetLayerImportArgs() {
            return new HitsoundsLayerImportArgs(Path, DiscriminateVolumes, DetectDuplicateSamples);
        }

        public bool ReloadCompatible(ILayerSourceRef other) {
            return other is IHitsoundsLayerSourceRef o  && 
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