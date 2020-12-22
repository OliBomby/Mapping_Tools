using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio {
    public class OsuVolumeConverter {
        public static ISampleProvider VolumeChange(ISampleProvider sample, double volume) {
            return new VolumeSampleProvider(sample) { Volume = (float) VolumeToAmplitude(volume) };
        }

        private static double HeightAt005 => 0.995 * Math.Pow(0.05, 1.5) + 0.005;

        public static double VolumeToAmplitude(double volume) {
            if (volume < 0.05) {
                return HeightAt005 / 0.05 * volume;
            }

            // This formula seems to convert osu! volume to amplitude multiplier
            return 0.995 * Math.Pow(volume, 1.5) + 0.005;
        }

        public static double AmplitudeToVolume(double amplitude) {
            if (amplitude < HeightAt005) {
                return 0.05 / HeightAt005 * amplitude;
            }

            return Math.Pow((amplitude - 0.005) / 0.995, 1 / 1.5);
        }
    }
}