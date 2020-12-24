namespace Mapping_Tools_Core.Tools.HitsoundStudio.DataTypes.LayerImportArgs {
    public interface ISampleMapLayerImportArgs : IFileLayerImportArgs {
        bool DiscriminateVolumes { get; }
        bool DetectDuplicateSamples { get; }
    }
}