using Mapping_Tools_Core.Audio.Midi;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public class MidiLayerSourceRef : IMidiLayerSourceRef {
        public string Path { get; }
        public IMidiNote Note { get; }
        public double Offset { get; }
        public double VelocityRoughness { get; }
        public double LengthRoughness { get; }

        public MidiLayerSourceRef(string path, IMidiNote note, double offset, double velocityRoughness, double lengthRoughness) {
            Path = path;
            Note = note;
            Offset = offset;
            VelocityRoughness = velocityRoughness;
            LengthRoughness = lengthRoughness;
        }

        public bool Equals(ILayerSourceRef other) {
            return other is IMidiLayerSourceRef o &&
                   Path == o.Path &&
                   Note.Equals(o.Note) &&
                   Offset == o.Offset &&
                   VelocityRoughness == o.VelocityRoughness &&
                   LengthRoughness == o.LengthRoughness;
        }

        public ILayerImportArgs GetLayerImportArgs() {
            return new MidiLayerImportArgs(
                Path,
                Offset,
                true,
                true,
                true,
                true,
                VelocityRoughness, 
                LengthRoughness);
        }

        public bool ReloadCompatible(ILayerSourceRef other) {
            return other is MidiLayerSourceRef o &&
                   Path == o.Path &&
                   Offset == o.Offset &&
                   (Note.Bank == -1 || Note.Bank == o.Note.Bank) && 
                   (Note.Patch == -1 || Note.Patch == o.Note.Patch) && 
                   (Note.Key == -1 || Note.Key == o.Note.Key) && 
                   (Note.Length == -1 || Note.Length == o.Note.Length) && 
                   (Note.Velocity == -1 || Note.Velocity == o.Note.Velocity);
        }
    }
}