using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.Audio.SampleImporters;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Audio.SampleImportArgs {
    public class SoundFontSampleImportArgs : ISoundFontSampleImportArgs {
        private string Extension => System.IO.Path.GetExtension(Path);

        public SoundFontSampleImportArgs(string path, int bank, int patch, int instrument, int key, int velocity, double length) {
            Path = path;
            Bank = bank;
            Patch = patch;
            Instrument = instrument;
            Key = key;
            Velocity = velocity;
            Length = length;
        }

        public bool Equals(ISampleImportArgs other) {
            return other is ISoundFontSampleImportArgs o &&
                   Path == o.Path &&
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
            return File.Exists(Path) && Extension == ".sf2";
        }

        public bool IsValid(Dictionary<ISampleImportArgs, ISampleSoundGenerator> loadedSamples) {
            return loadedSamples.ContainsKey(this) && loadedSamples[this] != null;
        }

        public ISampleSoundGenerator Import() {
            return SoundFontSampleImporter.GetInstance().Import(this);
        }

        public string Path { get; }
        public int Bank { get; }
        public int Patch { get; }
        public int Instrument { get; }
        public int Key { get; }
        public int Velocity { get; }
        public double Length { get; }
    }
}