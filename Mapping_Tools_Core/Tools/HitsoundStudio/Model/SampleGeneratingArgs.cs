using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.Audio.SampleSoundGeneration.Decorators;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class SampleGeneratingArgs : ISampleGeneratingArgs {
        public ISampleImportArgs ImportArgs { get; }

        public double Volume { get; }

        public SampleGeneratingArgs() { }

        public SampleGeneratingArgs(ISampleImportArgs importArgs, double volume) {
            ImportArgs = importArgs;
            Volume = volume;
        }

        public bool IsValid() {
            return ImportArgs != null && ImportArgs.IsValid();
        }

        public bool IsValid(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            return loadedSamples.ContainsKey(this) && loadedSamples[this] != null;
        }

        public ISampleSoundGenerator Import() {
            var baseGenerator = ImportArgs?.Import();

            return baseGenerator == null ? null : 
                new VolumeSampleSoundDecorator(baseGenerator, Volume);
        }

        public bool Equals(ISampleGeneratingArgs other) {
            if (other == null) return false;
            if (ImportArgs == null) {
                if (other.ImportArgs != null) return false;
            } else {
                if (!ImportArgs.Equals(other.ImportArgs)) return false;
            }
            return Volume.Equals(other.Volume);
        }

        public object Clone() {
            return new SampleGeneratingArgs(ImportArgs?.Clone() as ISampleImportArgs, Volume);
        }
    }
}