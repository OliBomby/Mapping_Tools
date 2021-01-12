namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public interface IStackLayerImportArgs : IFileLayerImportArgs {
        double X { get; }
        double Y { get; }
        double Leniency { get; }
    }
}