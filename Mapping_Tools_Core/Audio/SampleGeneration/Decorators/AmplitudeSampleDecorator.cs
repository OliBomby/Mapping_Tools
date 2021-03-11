using Mapping_Tools_Core.MathUtil;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="IAudioSampleGenerator"/> to change the amplitude.
    /// </summary>
    public class AmplitudeSampleDecorator : AudioSampleDecoratorAbstract {
        public float Volume { get; }

        /// <summary>
        /// Constructs a new <see cref="AmplitudeSampleDecorator"/> with specified amplitude factor.
        /// </summary>
        /// <param name="baseAudioGenerator">The generator to decorate</param>
        /// <param name="volume">The amplitude factor (0-1)</param>
        public AmplitudeSampleDecorator(IAudioSampleGenerator baseAudioGenerator, float volume = 1) : base(baseAudioGenerator) {
            Volume = volume;
        }

        public override bool Equals(ISampleGenerator other) {
            if (other is AmplitudeSampleDecorator amplitudeSampleDecorator)
                return Precision.AlmostEquals(Volume, amplitudeSampleDecorator.Volume) &&
                       BaseGenerator.Equals(amplitudeSampleDecorator.BaseGenerator);

            return base.Equals(other);
        }

        public override object Clone() {
            return new AmplitudeSampleDecorator((IAudioSampleGenerator)BaseAudioGenerator.Clone(), Volume);
        }

        protected override string GetNameExtension() {
            return $"-amp({(Volume * 100).ToRoundInvariant()})";
        }

        public override bool HasEffect() => !Precision.AlmostEquals(Volume, 1);

        protected override bool HasClippingPossible() {
            return GetAmplitudeFactor() > 1;
        }

        protected override ISampleProvider Decorate(ISampleProvider sampleProvider) {
            return new VolumeSampleProvider(sampleProvider) { Volume = Volume };
        }

        public override double GetAmplitudeFactor() {
            return BaseAudioGenerator.GetAmplitudeFactor() * Volume;
        }
    }
}