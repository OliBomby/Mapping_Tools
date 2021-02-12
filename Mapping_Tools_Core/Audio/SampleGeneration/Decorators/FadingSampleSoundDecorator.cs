using Mapping_Tools_Core.Audio.Effects;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="ISampleSoundGenerator"/> to add a fade-out.
    /// </summary>
    public class FadingSampleSoundDecorator : SampleSoundDecoratorAbstract {
        private readonly double fadeStart;
        private readonly double fadeLength;

        /// <summary>
        /// Constructs a new <see cref="FadingSampleSoundDecorator"/> with fade-out.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="fadeStart">The time in seconds when to start the fade-out</param>
        /// <param name="fadeLength">The duration in seconds of the fade-out</param>
        public FadingSampleSoundDecorator(ISampleSoundGenerator baseGenerator, double fadeStart, double fadeLength) : base(baseGenerator) {
            this.fadeStart = fadeStart;
            this.fadeLength = fadeLength;
        }

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            var output = new DelayFadeOutSampleProvider(baseSampleProvider);
            output.BeginFadeOut(fadeStart * 1000, fadeLength * 1000);
            return output;
        }
    }
}