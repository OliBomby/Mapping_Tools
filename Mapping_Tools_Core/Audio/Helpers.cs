using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio {
    public static class Helpers {
        public static ISampleProvider SetChannels(ISampleProvider sampleProvider, int channels) {
            return channels == 1 ? ToMono(sampleProvider) : ToStereo(sampleProvider);
        }

        private static ISampleProvider ToStereo(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 1 ? new MonoToStereoSampleProvider(sampleProvider) : sampleProvider;
        }

        private static ISampleProvider ToMono(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 2 ? new StereoToMonoSampleProvider(sampleProvider) : sampleProvider;
        }

        /// <summary>
        /// Creates a new wave file at the given location using the wave audio.
        /// </summary>
        /// <param name="filename">The file path</param>
        /// <param name="sourceProvider">The audio to write</param>
        /// <returns>Whether the write was a success</returns>
        public static bool CreateWaveFile(string filename, IWaveProvider sourceProvider) {
            try {
                using (var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat)) {
                    var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                    while (true) {
                        int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        // Write will throw exception if WAV file becomes too large
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                return true;
            } catch (IndexOutOfRangeException) {
                return false;
            }
        }

        /// <summary>
        /// Writes a new wave file into the stream using the wave audio.
        /// </summary>
        /// <param name="outStream">The stream to write to</param>
        /// <param name="sourceProvider">The audio to write</param>
        /// <returns>Whether the write was a success</returns>
        public static bool CreateWaveFile(Stream outStream, IWaveProvider sourceProvider) {
            try {
                using (var writer = new WaveFileWriter(outStream, sourceProvider.WaveFormat)) {
                    var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                    while (true) {
                        int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        // Write will throw exception if WAV file becomes too large
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                return true;
            } catch (IndexOutOfRangeException) {
                return false;
            }
        }
    }
}