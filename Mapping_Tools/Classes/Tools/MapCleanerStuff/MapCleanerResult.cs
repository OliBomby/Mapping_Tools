namespace Mapping_Tools.Classes.Tools.MapCleanerStuff {
    public class MapCleanerResult {
        public int ObjectsResnapped;
        public int SamplesRemoved;
        public int TimingPointsRemoved;

        public MapCleanerResult() {
            SamplesRemoved = 0;
        }

        public MapCleanerResult(int objectsResnapped, int samplesRemoved) {
            ObjectsResnapped = objectsResnapped;
            SamplesRemoved = 0;
            SamplesRemoved = samplesRemoved;
        }

        public void Add(MapCleanerResult other) {
            ObjectsResnapped += other.ObjectsResnapped;
            SamplesRemoved += other.SamplesRemoved;
            TimingPointsRemoved += other.TimingPointsRemoved;
        }
    }
}