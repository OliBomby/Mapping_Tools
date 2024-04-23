using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;
using Mapping_Tools.Classes.HitsoundStuff.Effects;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff {
    /// <summary>
    /// Generates sample providers from a cached wave stream, and applies effects to them.
    /// </summary>
    public class SampleSoundGenerator {
        /// <summary>
        /// The wave stream to generate samples from.
        /// </summary>
        public WaveStream Wave { get; }
        /// <summary>
        /// Multiple generators to mix and play at the same time.
        /// </summary>
        public SampleSoundGenerator[] Generators { get; }

        public double VolumeCorrection { get; set; } = 1;
        public double Panning { get; set; }
        public double PitchShift { get; set; }
        public double FadeStart { get; set; } = -1;
        public double FadeLength { get; set; } = -1;
        public int SampleRate { get; set; } = -1;
        public int Channels { get; set; } = -1;

        /// <summary>
        /// This means that this is the blank sample. There is some special logic for this.
        /// </summary>
        public bool BlankSample => Wave.TotalTime.Equals(TimeSpan.Zero);

        public SampleSoundGenerator(WaveStream wave) {
            Wave = wave;
        }

        public SampleSoundGenerator(SampleSoundGenerator[] generators) {
            Generators = generators;
        }

        /// <summary>
        /// Gets the sample provider with all the effects applied.
        /// </summary>
        /// <returns></returns>
        public ISampleProvider GetSampleProvider() {
            ISampleProvider output;
            if (Wave is not null) {
                Wave.Position = 0;
                output = WaveToSampleProvider(Wave);
            } else {
                output = new MixingSampleProvider(Generators.Select(g => g.GetSampleProvider()));
            }

            if (!Precision.AlmostEquals(FadeStart, -1) && !Precision.AlmostEquals(FadeLength, -1)) {
                output = new DelayFadeOutSampleProvider(output);
                ((DelayFadeOutSampleProvider) output).BeginFadeOut(FadeStart * 1000, FadeLength * 1000);
            }
            if (!Precision.AlmostEquals(VolumeCorrection, 1)) {
                output = SampleImporter.VolumeChange(output, VolumeCorrection);
            }
            if (!Precision.AlmostEquals(Panning, 0)) {
                output = SampleImporter.SetChannels(output, 1);
                output = new PanningSampleProvider(output) { Pan = (float)Panning };
            }
            if (!Precision.AlmostEquals(PitchShift, 0)) {
                output = SampleImporter.PitchShift(output, PitchShift);
            }
            if (SampleRate != -1) {
                output = new WdlResamplingSampleProvider(output, SampleRate);
            }
            if (Channels != -1) {
                output = SampleImporter.SetChannels(output, Channels);
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
