using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class MidiSampleImportArgs : IMidiSampleImportArgs {
        public MidiSampleImportArgs(int bank, int patch, int instrument, int key, int velocity, double length) {
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Velocity = velocity;
            Length = length;
        }

        public bool Equals(ISampleGenerator other) {
            return other is IMidiSampleImportArgs o &&
                   Bank == o.Bank &&
                   Patch == o.Patch &&
                   Instrument == o.Instrument &&
                   Key == o.Key &&
                   Velocity == o.Velocity &&
                   Precision.AlmostEquals(Length, o.Length);
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public bool IsValid() {
            return true;
        }

        public bool IsValid(Dictionary<ISampleGenerator, ISampleSoundGenerator> loadedSamples) {
            return true;
        }

        public ISampleSoundGenerator Import() {
            // This type doesnt generate sound directly, because its meant to be exported as arguments
            return null;
        }

        public string GetName() {
            return $"{Bank}-{Patch}-{Instrument}-{Key}-{Velocity}-{(int)Length}";
        }

        public int Bank { get; }
        public int Patch { get; }
        public int Instrument { get; }
        public int Key { get; }
        public int Velocity { get; }
        public double Length { get; }
    }
}