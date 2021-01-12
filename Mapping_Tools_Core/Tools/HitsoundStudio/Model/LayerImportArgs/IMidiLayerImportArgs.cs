namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public interface IMidiLayerImportArgs : IFileLayerImportArgs {
        double Offset { get; }
        bool DiscriminateInstruments { get; }
        bool DiscriminateKeys { get; }
        bool DiscriminateVelocities { get; }
        bool DiscriminateLength { get; }
        double VelocityRoughness { get; }
        double LengthRoughness { get; }
    }
}