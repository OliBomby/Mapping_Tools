namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public interface ISampleMapLayerSourceRef : IFileLayerSourceRef {
        string SamplePath { get; }
        double Volume { get; }
        bool DiscriminateVolumes { get; }
        bool DetectDuplicateSamples { get; }
    }
}