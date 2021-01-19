namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public class StoryboardLayerImportArgs : IStoryboardLayerImportArgs {
        public StoryboardLayerImportArgs(string path, bool discriminateVolumes, bool detectDuplicateSamples) {
            Path = path;
            DiscriminateVolumes = discriminateVolumes;
            DetectDuplicateSamples = detectDuplicateSamples;
        }

        public bool Equals(ILayerImportArgs other) {
            return other is IStoryboardLayerImportArgs o &&
                   Path == o.Path &&
                   DiscriminateVolumes == o.DiscriminateVolumes &&
                   DetectDuplicateSamples == o.DetectDuplicateSamples;
        }

        public string Path { get; }
        public bool DiscriminateVolumes { get; }
        public bool DetectDuplicateSamples { get; }
    }
}