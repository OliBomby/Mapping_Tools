using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef {
    public class MidiLayerSourceRef : IMidiLayerSourceRef {
        public MidiLayerSourceRef(string path, int bank, int patch, int key, int velocity, double length, double offset, double velocityRoughness, double lengthRoughness) {
            Path = path;
            Bank = bank;
            Patch = patch;
            Key = key;
            Velocity = velocity;
            Length = length;
            Offset = offset;
            VelocityRoughness = velocityRoughness;
            LengthRoughness = lengthRoughness;
        }

        public bool Equals(ILayerSourceRef other) {
            return other is IMidiLayerSourceRef o &&
                   Path == o.Path &&
                   Bank == o.Bank &&
                   Patch == o.Patch &&
                   Key == o.Key &&
                   Velocity == o.Velocity &&
                   Length == o.Length &&
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
                   (Bank == -1 || Bank == o.Bank) && 
                   (Patch == -1 || Patch == o.Patch) && 
                   (Key == -1 || Key == o.Key) && 
                   (Length == -1 || Length == o.Length) && 
                   (Velocity == -1 || Velocity == o.Velocity);
        }

        public string Path { get; }
        public int Bank { get; }
        public int Patch { get; }
        public int Key { get; }
        public int Velocity { get; }
        public double Length { get; }
        public double Offset { get; }
        public double VelocityRoughness { get; }
        public double LengthRoughness { get; }
    }
}