using System;
using System.Collections.Generic;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.Model {
    public class SampleGeneratingArgs : ISampleGeneratingArgs {
        public ISampleGenerator ImportArgs { get; }

        public double Volume { get; }

        public SampleGeneratingArgs() {
            Volume = 1;
        }

        public SampleGeneratingArgs(ISampleGenerator importArgs, double volume) {
            ImportArgs = importArgs;
            Volume = volume;
        }

        public bool IsValid() {
            return ImportArgs != null && ImportArgs.IsValid();
        }

        public bool IsValid(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            return loadedSamples.ContainsKey(this) && loadedSamples[this] != null;
        }

        public bool HasEffects() {
            return Math.Abs(Volume - 1) > Precision.DOUBLE_EPSILON;
        }

        public ISampleSoundGenerator Import() {
            var baseGenerator = ImportArgs?.Import();

            return ApplyEffects(baseGenerator);
        }

        public ISampleSoundGenerator ApplyEffects(ISampleSoundGenerator baseGenerator) {
            return baseGenerator == null ? null :
                new VolumeSampleSoundDecorator(baseGenerator, Volume);
        }

        public string GetName() {
            if (ImportArgs == null) {
                return (Volume * 100).ToRoundInvariant();
            }

            return Math.Abs(Volume - 1) < Precision.DOUBLE_EPSILON
                ? ImportArgs.GetName()
                : $"{ImportArgs.GetName()}-{(Volume * 100).ToRoundInvariant()}";
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
            return new SampleGeneratingArgs(ImportArgs?.Clone() as ISampleGenerator, Volume);
        }
    }
}