namespace Mapping_Tools_Core.Tools.MapCleanerStuff {
    public interface IMapCleanerResult {
        int ObjectsResnapped { get; set; }
        int SamplesRemoved { get; set; }
        int TimingPointsRemoved { get; set; }

        void Add(IMapCleanerResult other);
    }
}