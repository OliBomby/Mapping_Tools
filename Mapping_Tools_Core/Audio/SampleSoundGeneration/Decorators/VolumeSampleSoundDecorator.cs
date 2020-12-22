using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleSoundGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="ISampleSoundGenerator"/> to change the volume.
    /// The volume scaling in this class is roughly equal to how osu! volume works.
    /// </summary>
    public class VolumeSampleSoundDecorator : SampleSoundDecoratorAbstract {
        private readonly double volume;

        /// <summary>
        /// Constructs a new <see cref="VolumeSampleSoundDecorator"/> with specified volume.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="volume">The volume factor (0-1)</param>
        public VolumeSampleSoundDecorator(ISampleSoundGenerator baseGenerator, double volume = 1) : base(baseGenerator) {
            this.volume = volume;
        }

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            return OsuVolumeConverter.VolumeChange(baseSampleProvider, volume);
        }
    }
}