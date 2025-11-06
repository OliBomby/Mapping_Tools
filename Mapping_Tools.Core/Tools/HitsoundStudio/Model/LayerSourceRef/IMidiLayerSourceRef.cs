using Mapping_Tools.Core.Audio.Midi;

namespace Mapping_Tools.Core.Tools.HitsoundStudio.Model.LayerSourceRef;

public interface IMidiLayerSourceRef : IFileLayerSourceRef {
    IMidiNote Note { get; }
    double Offset { get; }
    double VelocityRoughness { get; }
    double LengthRoughness { get; }
}