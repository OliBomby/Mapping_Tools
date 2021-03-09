using System;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Audio {
    public static class Helpers {
        public static WaveStream OpenSample(string path) {
            return Path.GetExtension(path) == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
        }

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

        /// <summary>
        /// Preloads all sample generators efficiently.
        /// </summary>
        /// <param name="sampleGenerators">The samples to import</param>
        public static void PreloadSampleGenerators(IEnumerable<ISampleGenerator> sampleGenerators) {
            // Group the args by path so the SoundFont importer can benefit of caching
            var separatedByPath = new Dictionary<string, HashSet<ISampleGenerator>>();
            var otherGenerators = new HashSet<ISampleGenerator>();

            foreach (var generator in sampleGenerators) {
                if (generator is IFromPathGenerator pathGenerator) {
                    if (separatedByPath.TryGetValue(pathGenerator.Path, out HashSet<ISampleGenerator> value)) {
                        value.Add(generator);
                    } else {
                        separatedByPath.Add(pathGenerator.Path, new HashSet<ISampleGenerator> { generator });
                    }
                } else if (generator != null) {
                    otherGenerators.Add(generator);
                }
            }

            // Import all samples
            foreach (var pair in separatedByPath) {
                PreloadFast(pair.Value);

                if (Path.GetExtension(pair.Key) == ".sf2") {
                    // Collect garbage to clean up big SoundFont object
                    GC.Collect();
                }
            }

            PreloadFast(otherGenerators);
        }

        private static void PreloadFast(IEnumerable<ISampleGenerator> sampleGenerators) {
            foreach (var generator in sampleGenerators) {
                try {
                    if (generator is IPreloadableGenerator preloadableGenerator) {
                        preloadableGenerator.PreloadSample();
                    }
                } catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
    }
}