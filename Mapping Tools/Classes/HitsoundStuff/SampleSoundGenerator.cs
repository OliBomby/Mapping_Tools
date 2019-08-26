using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class SampleSoundGenerator {
        public WaveStream Wave { get; set; }
        public int KeyCorrection { get; set; }
        public float VolumeCorrection { get; set; }
        public double FadeStart { get; set; }
        public double FadeLength { get; set; }

        public SampleSoundGenerator(WaveStream wave) {
            Wave = wave;
            KeyCorrection = -1;
            VolumeCorrection = -1;
            FadeStart = -1;
            FadeLength = -1;
        }

        public SampleSoundGenerator(WaveStream wave, double fadeStart, double fadeLength) {
            Wave = wave;
            KeyCorrection = -1;
            VolumeCorrection = -1;
            FadeStart = fadeStart;
            FadeLength = fadeLength;
        }

        public ISampleProvider GetSampleProvider() {
            Wave.Position = 0;
            ISampleProvider output = WaveToSampleProvider(Wave);

            if (FadeStart != -1 && FadeLength != -1) {
                output = new DelayFadeOutSampleProvider(output);
                (output as DelayFadeOutSampleProvider).BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
            }
            if (KeyCorrection != -1 && KeyCorrection != 0) {
                output = SampleImporter.PitchShift(output, KeyCorrection);
            }
            if (VolumeCorrection != -1 && VolumeCorrection != 1) {
                output = SampleImporter.VolumeChange(output, VolumeCorrection);
            }
            return output;
        }

        private static ISampleProvider WaveToSampleProvider(WaveStream wave) {
            if (wave.WaveFormat.Encoding == WaveFormatEncoding.Pcm) {
                switch (wave.WaveFormat.BitsPerSample) {
                    case 8:
                        return new Pcm8BitToSampleProvider(wave);
                    case 16:
                        return new Pcm16BitToSampleProvider(wave);
                    case 24:
                        return new Pcm24BitToSampleProvider(wave);
                    case 32:
                        return new Pcm32BitToSampleProvider(wave);
                }
            } else if (wave.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat) {
                return new WaveToSampleProvider(wave);
            }
            return null;
        }
    }
}
