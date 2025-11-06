namespace Mapping_Tools.Core.Tools.HitsoundStudio.Model.LayerSourceRef;

public interface IStackLayerSourceRef : IFileLayerSourceRef {
    double X { get; }
    double Y { get; }
    double Leniency { get; }
}