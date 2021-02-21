using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Audio.Midi {
    public class MidiNote : IMidiNote {
        public int Bank { get; }
        public int Patch { get; }
        public int Key { get; }
        public int Velocity { get; }
        public double Length { get; }

        public MidiNote(int bank, int patch, int key, int velocity, double length) {
            Bank = bank;
            Patch = patch;
            Key = key;
            Velocity = velocity;
            Length = length;
        }

        public bool Equals(IMidiNote other) {
            if (!(other is MidiNote o)) return false;

            return Bank == o.Bank && 
                   Patch == o.Patch && 
                   Key == o.Key && 
                   Velocity == o.Velocity && 
                   Precision.AlmostEquals(Length, o.Length);
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            return $"{Bank}-{Patch}-{Key}-{Velocity}-{(Length * 1000).ToRoundInvariant()}";
        }
    }
}