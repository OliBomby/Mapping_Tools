namespace Mapping_Tools.Core.Tools.MapCleanerStuff;

public class MapCleanerResult : IMapCleanerResult {
    public int ObjectsResnapped { get; set; }
    public int TimingPointsRemoved { get; set; }

    public MapCleanerResult() {
    }

    public MapCleanerResult(int objectsResnapped, int timingPointsRemoved) {
        ObjectsResnapped = objectsResnapped;
        TimingPointsRemoved = timingPointsRemoved;
    }

    public void Add(IMapCleanerResult other) {
        ObjectsResnapped += other.ObjectsResnapped;
        TimingPointsRemoved += other.TimingPointsRemoved;
    }
}