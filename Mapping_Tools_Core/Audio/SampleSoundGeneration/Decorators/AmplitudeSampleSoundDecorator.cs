using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio.SampleSoundGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="ISampleSoundGenerator"/> to change the amplitude.
    /// The amplitude scaling in this class uses <see cref="VolumeSampleProvider"/>.
    /// </summary>
    public class AmplitudeSampleSoundDecorator : SampleSoundDecoratorAbstract {
        private readonly float volume;

        /// <summary>
        /// Constructs a new <see cref="AmplitudeSampleSoundDecorator"/> with specified amplitude factor.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="volume">The amplitude factor (0-1)</param>
        public AmplitudeSampleSoundDecorator(ISampleSoundGenerator baseGenerator, float volume = 1) : base(baseGenerator) {
            this.volume = volume;
        }

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            return new VolumeSampleProvider(baseSampleProvider) {Volume = volume};
        }
    }
}