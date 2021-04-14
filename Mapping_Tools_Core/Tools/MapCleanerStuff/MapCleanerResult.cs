namespace Mapping_Tools_Core.Tools.MapCleanerStuff {
    public class MapCleanerResult : IMapCleanerResult {
        public int ObjectsResnapped { get; set; }
        public int SamplesRemoved { get; set; }
        public int TimingPointsRemoved { get; set; }

        public MapCleanerResult() {
            SamplesRemoved = 0;
        }

        public MapCleanerResult(int objectsResnapped, int samplesRemoved) {
            ObjectsResnapped = objectsResnapped;
            SamplesRemoved = 0;
            SamplesRemoved = samplesRemoved;
        }

        public void Add(IMapCleanerResult other) {
            ObjectsResnapped += other.ObjectsResnapped;
            SamplesRemoved += other.SamplesRemoved;
            TimingPointsRemoved += other.TimingPointsRemoved;
        }
    }
}