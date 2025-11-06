namespace Mapping_Tools.Core.Tools.HitsoundStudio.Model.LayerImportArgs;

public interface IStackLayerImportArgs : IFileLayerImportArgs {
    double X { get; }
    double Y { get; }
    double Leniency { get; }
}