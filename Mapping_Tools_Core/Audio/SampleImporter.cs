using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools_Core.Audio.SampleGeneration;

namespace Mapping_Tools_Core.Audio {
    public class SampleImporter {
        public static WaveStream OpenSample(string path) {
            return Path.GetExtension(path) == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
        }

        /// <summary>
        /// Imports all samples specified by <see cref="ISampleSoundGenerator"/> and returns a dictionary which maps the
        /// <see cref="ISampleSoundGenerator"/> to their <see cref="ISampleSoundGenerator"/>.
        /// If a sample couldn't be imported then it has a null instead.
        /// </summary>
        /// <param name="argsList">The samples to import</param>
        /// <param name="comparer">The equality comparer for the import args. If null, Default will be used</param>
        /// <returns></returns>
        public static Dictionary<ISampleGenerator, ISampleSoundGenerator> ImportSamples(
            IEnumerable<ISampleGenerator> argsList,
            IEqualityComparer<ISampleGenerator> comparer = null) {

            if (comparer == null) {
                // Get the default comparer so it uses IEquatable
                comparer = EqualityComparer<ISampleGenerator>.Default;
            }

            var samples = new Dictionary<ISampleGenerator, ISampleSoundGenerator>(comparer);

            // Group the args by path so the SoundFont importer can benefit of caching
            var separatedByPath = new Dictionary<string, HashSet<ISampleGenerator>>();
            var otherArgs = new HashSet<ISampleGenerator>();

            foreach (var args in argsList) {
                if (args is IPathSampleGenerator pathArgs) {
                    if (separatedByPath.TryGetValue(pathArgs.Path, out HashSet<ISampleGenerator> value)) {
                        value.Add(args);
                    } else {
                        separatedByPath.Add(pathArgs.Path, new HashSet<ISampleGenerator>(comparer) {args});
                    }
                } else if (args != null) {
                    otherArgs.Add(args);
                }
            }

            // Import all samples
            foreach (var pair in separatedByPath) {
                foreach (var args in pair.Value) {
                    try {
                        samples.Add(args, args.Import());
                    } catch (Exception e) {
                        Console.WriteLine(e);
                        samples.Add(args, null);
                    }
                }

                if (Path.GetExtension(pair.Key) == ".sf2") {
                    // Collect garbage to clean up big SoundFont object
                    GC.Collect();
                }
            }

            foreach (var args in otherArgs) {
                try {
                    samples.Add(args, args.Import());
                } catch (Exception e) {
                    Console.WriteLine(e);
                    samples.Add(args, null);
                }
            }

            return samples;
        }
    }
}
