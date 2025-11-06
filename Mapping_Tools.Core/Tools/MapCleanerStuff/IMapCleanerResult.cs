namespace Mapping_Tools.Core.Tools.MapCleanerStuff;

public interface IMapCleanerResult {
    int ObjectsResnapped { get; set; }
    int TimingPointsRemoved { get; set; }

    void Add(IMapCleanerResult other);
}