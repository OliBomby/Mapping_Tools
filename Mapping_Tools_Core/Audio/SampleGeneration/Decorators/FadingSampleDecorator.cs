using Mapping_Tools_Core.Audio.Effects;
using Mapping_Tools_Core.MathUtil;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="IAudioSampleGenerator"/> to add a fade-out.
    /// </summary>
    public class FadingSampleDecorator : AudioSampleDecoratorAbstract {
        public double FadeStart { get; }
        public double FadeLength { get; }

        /// <summary>
        /// Constructs a new <see cref="FadingSampleDecorator"/> with fade-out.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="fadeStart">The time in seconds when to start the fade-out</param>
        /// <param name="fadeLength">The duration in seconds of the fade-out</param>
        public FadingSampleDecorator(IAudioSampleGenerator baseGenerator, double fadeStart, double fadeLength) : base(baseGenerator) {
            FadeStart = fadeStart;
            FadeLength = fadeLength;
        }

        public override bool Equals(ISampleGenerator other) {
            if (other is FadingSampleDecorator fadingSampleSoundDecorator)
                return Precision.AlmostEquals(FadeStart, fadingSampleSoundDecorator.FadeStart) && 
                       Precision.AlmostEquals(FadeLength, fadingSampleSoundDecorator.FadeLength) && 
                       BaseGenerator.Equals(fadingSampleSoundDecorator.BaseGenerator);

            return base.Equals(other);
        }

        public override object Clone() {
            return new FadingSampleDecorator((IAudioSampleGenerator)BaseAudioGenerator.Clone(), FadeStart, FadeLength);
        }

        protected override string GetNameExtension() {
            return $"-fade({(FadeStart * 1000).ToRoundInvariant()}-{(FadeLength * 1000).ToRoundInvariant()})";
        }

        public override bool HasEffect() => true;

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            var output = new DelayFadeOutSampleProvider(baseSampleProvider);
            output.BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
            return output;
        }
    }
}