﻿using Mapping_Tools_Core.MathUtil;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    /// <summary>
    /// Decorator for <see cref="IAudioSampleGenerator"/> to change the volume.
    /// The volume scaling in this class is roughly equal to how osu! volume works.
    /// </summary>
    public class VolumeSampleSoundDecorator : AudioSampleDecoratorAbstract {
        public double Volume { get; }

        /// <summary>
        /// Constructs a new <see cref="VolumeSampleSoundDecorator"/> with specified volume.
        /// </summary>
        /// <param name="baseGenerator">The generator to decorate</param>
        /// <param name="volume">The volume factor (0-1)</param>
        public VolumeSampleSoundDecorator(IAudioSampleGenerator baseGenerator, double volume = 1) : base(baseGenerator) {
            Volume = volume;
        }

        public override bool Equals(ISampleGenerator other) {
            if (other is VolumeSampleSoundDecorator volumeSampleSoundDecorator)
                return Precision.AlmostEquals(Volume, volumeSampleSoundDecorator.Volume) &&
                       BaseGenerator.Equals(volumeSampleSoundDecorator.BaseGenerator);

            return base.Equals(other);
        }

        public override object Clone() {
            return new VolumeSampleSoundDecorator((IAudioSampleGenerator)BaseAudioGenerator.Clone(), Volume);
        }

        protected override string GetNameExtension() {
            return $"-vol({(Volume * 100).ToRoundInvariant()})";
        }

        public override bool HasEffect() => !Precision.AlmostEquals(Volume, 1);

        protected override ISampleProvider Decorate(ISampleProvider baseSampleProvider) {
            return OsuVolumeConverter.VolumeChange(baseSampleProvider, Volume);
        }
    }
}