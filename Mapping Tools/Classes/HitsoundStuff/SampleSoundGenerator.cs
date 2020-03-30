using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using Mapping_Tools.Classes.HitsoundStuff.Effects;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// TODO: Complete comments.
    /// </summary>
    public class SampleSoundGenerator {
        /// <summary />
        public WaveStream Wave { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int KeyCorrection { get; set; }
        public double VolumeCorrection { get; set; }
        public double FadeStart { get; set; }
        public double FadeLength { get; set; }

        /// <summary>
        /// This means that this is the blank sample. There is some special logic for this.
        /// </summary>
        public bool BlankSample => Wave.TotalTime.Equals(TimeSpan.Zero);

        /// <inheritdoc />
        public SampleSoundGenerator(WaveStream wave) {
            Wave = wave;
            KeyCorrection = 0;
            VolumeCorrection = 1;
            FadeStart = -1;
            FadeLength = -1;
        }

        /// <inheritdoc />
        public SampleSoundGenerator(WaveStream wave, double fadeStart, double fadeLength) {
            Wave = wave;
            KeyCorrection = 0;
            VolumeCorrection = 1;
            FadeStart = fadeStart;
            FadeLength = fadeLength;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISampleProvider GetSampleProvider() {
            Wave.Position = 0;
            ISampleProvider output = WaveToSampleProvider(Wave);

            if (FadeStart != -1 && FadeLength != -1) {
                output = new DelayFadeOutSampleProvider(output);
                ((DelayFadeOutSampleProvider) output).BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
            }
            if (KeyCorrection != 0) {
                output = SampleImporter.PitchShift(output, KeyCorrection);
            }
            if (VolumeCorrection != 1) {
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
