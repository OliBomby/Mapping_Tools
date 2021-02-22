using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="IAudioSampleGenerator"/> to add a pitch-shift.
    /// </summary>
    public class PitchShiftSampleDecorator : AudioSampleDecoratorAbstract {
        public int Octaves { get; }
        public int Semitones { get; }
        public int Cents { get; }

        /// <summary>
        /// Constructs a new <see cref="PitchShiftSampleDecorator"/> with pitch-shift.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="octaves">The Octaves to pitch-shift</param>
        /// <param name="semitones">The Semitones to pitch-shift</param>
        /// <param name="cents">The Cents to pitch-shift</param>
        public PitchShiftSampleDecorator(IAudioSampleGenerator baseGenerator, int octaves = 0, int semitones = 0,
            int cents = 0) : base(baseGenerator) {
            Octaves = octaves;
            Semitones = semitones;
            Cents = cents;
        }

        public override bool Equals(ISampleGenerator other) {
            if (other is PitchShiftSampleDecorator psssd)
                return Precision.AlmostEquals((Octaves * 12 + Semitones) * 100 + Cents,
                           (psssd.Octaves * 12 + psssd.Semitones) * 100 + psssd.Cents) &&
                       BaseGenerator.Equals(psssd.BaseGenerator);

            return base.Equals(other);
        }

        public override object Clone() {
            return new PitchShiftSampleDecorator((IAudioSampleGenerator)BaseAudioGenerator.Clone(), Octaves, Semitones, Cents);
        }

        protected override string GetNameExtension() {
            return $"-pitch({Octaves.ToInvariant()}-{Semitones.ToInvariant()}-{Cents.ToInvariant()})";
        }

        public override bool HasEffect() => Octaves != 0 || Semitones != 0 || Cents != 0;

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            float factor = (float)Math.Pow(2, ((Octaves * 12 + Semitones) * 100 + Cents) / 1200f);
            return new SmbPitchShiftingSampleProvider(baseSampleProvider, 1024, 4, factor);
        }
    }
}