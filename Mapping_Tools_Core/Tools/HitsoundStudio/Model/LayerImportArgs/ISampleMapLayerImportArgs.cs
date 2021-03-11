namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public interface ISampleMapLayerImportArgs : IFileLayerImportArgs {
        /// <summary>
        /// Taking the volumes from the file and making different layers for each different volumes.
        /// </summary>
        bool DiscriminateVolumes { get; }

        /// <summary>
        /// Detect duplicate samples and optimise hitsound layer count with that.
        /// </summary>
        bool DetectDuplicateSamples { get; }
    }
}