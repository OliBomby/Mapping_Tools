namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public interface IMidiLayerSourceRef : IFileLayerSourceRef {
        int Bank { get; }
        int Patch { get; }
        int Key { get; }
        int Velocity { get; }
        double Length { get; }
        double VelocityRoughness { get; }
        double LengthRoughness { get; }
    }
}