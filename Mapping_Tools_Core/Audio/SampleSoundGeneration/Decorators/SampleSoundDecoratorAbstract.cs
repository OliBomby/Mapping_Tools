using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleSoundGeneration.Decorators {
    public abstract class SampleSoundDecoratorAbstract : ISampleSoundGenerator {
        private readonly ISampleSoundGenerator baseGenerator;

        /// <summary>
        /// Constructs a new <see cref="SampleSoundDecoratorAbstract"/>.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        protected SampleSoundDecoratorAbstract(ISampleSoundGenerator baseGenerator) {
            this.baseGenerator = baseGenerator;
        }

        public ISampleProvider GetSampleProvider() {
            return Decorate(baseGenerator.GetSampleProvider());
        }

        public virtual bool IsBlank() {
            return baseGenerator.IsBlank();
        }

        public WaveFormat GetSourceWaveFormat() {
            return baseGenerator.GetSourceWaveFormat();
        }

        protected abstract ISampleProvider Decorate(ISampleProvider baseSampleProvider);
    }
}