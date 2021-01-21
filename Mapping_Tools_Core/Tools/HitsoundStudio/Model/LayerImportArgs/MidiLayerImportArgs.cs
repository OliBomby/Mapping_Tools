namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs {
    public class MidiLayerImportArgs : IMidiLayerImportArgs {
        public MidiLayerImportArgs(string path, double offset, bool discriminateInstruments, bool discriminateKeys, bool discriminateVelocities, bool discriminateLength, double velocityRoughness, double lengthRoughness) {
            Path = path;
            Offset = offset;
            DiscriminateInstruments = discriminateInstruments;
            DiscriminateKeys = discriminateKeys;
            DiscriminateVelocities = discriminateVelocities;
            DiscriminateLength = discriminateLength;
            VelocityRoughness = velocityRoughness;
            LengthRoughness = lengthRoughness;
        }

        public bool Equals(ILayerImportArgs other) {
            return other is IMidiLayerImportArgs o &&
                   Path == o.Path &&
                   Offset == o.Offset &&
                   DiscriminateInstruments == o.DiscriminateInstruments &&
                   DiscriminateKeys == o.DiscriminateKeys &&
                   DiscriminateVelocities == o.DiscriminateVelocities &&
                   DiscriminateLength == o.DiscriminateLength &&
                   VelocityRoughness == o.VelocityRoughness &&
                   LengthRoughness == o.LengthRoughness;
        }

        public string Path { get; }
        public double Offset { get; }
        public bool DiscriminateInstruments { get; }
        public bool DiscriminateKeys { get; }
        public bool DiscriminateVelocities { get; }
        public bool DiscriminateLength { get; }
        public double VelocityRoughness { get; }
        public double LengthRoughness { get; }
    }
}