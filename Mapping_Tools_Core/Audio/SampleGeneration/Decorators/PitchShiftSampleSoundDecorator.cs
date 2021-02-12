using System;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="ISampleSoundGenerator"/> to add a pitch-shift.
    /// </summary>
    public class PitchShiftSampleSoundDecorator : SampleSoundDecoratorAbstract {
        private readonly int octaves;
        private readonly int semitones;
        private readonly int cents;

        /// <summary>
        /// Constructs a new <see cref="PitchShiftSampleSoundDecorator"/> with pitch-shift.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="octaves">The octaves to pitch-shift</param>
        /// <param name="semitones">The semitones to pitch-shift</param>
        /// <param name="cents">The cents to pitch-shift</param>
        public PitchShiftSampleSoundDecorator(ISampleSoundGenerator baseGenerator, int octaves = 0, int semitones = 0,
            int cents = 0) : base(baseGenerator) {
            this.octaves = octaves;
            this.semitones = semitones;
            this.cents = cents;
        }

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            float factor = (float)Math.Pow(2, ((octaves * 12 + semitones) * 100 + cents) / 1200f);
            return new SmbPitchShiftingSampleProvider(baseSampleProvider, 1024, 4, factor);
        }
    }
}