namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public interface ISampleMapLayerImportArgs : IFileLayerImportArgs {
        bool DiscriminateVolumes { get; }
        bool DetectDuplicateSamples { get; }
    }
}