using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace Mapping_Tools_Core.Audio.SampleSoundGeneration {
    public class WaveStreamSampleSoundGenerator : ISampleSoundGenerator {
        /// <summary>
        /// The <see cref="WaveStream"/> to generate sound from.
        /// </summary>
        public WaveStream Wave { get; set; }

        /// <inheritdoc />
        public WaveStreamSampleSoundGenerator(WaveStream wave) {
            Wave = wave;
        }

        public ISampleProvider GetSampleProvider() {
            Wave.Position = 0;
            ISampleProvider output = WaveToSampleProvider(Wave);

            return output;
        }

        public bool IsBlank() {
            return Wave.TotalTime.Equals(TimeSpan.Zero);
        }

        private static ISampleProvider WaveToSampleProvider(IWaveProvider wave) {
            switch (wave.WaveFormat.Encoding) {
                case WaveFormatEncoding.Pcm:
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

                    break;
                case WaveFormatEncoding.IeeeFloat:
                    return new WaveToSampleProvider(wave);
            }

            return null;
        }
    }
}