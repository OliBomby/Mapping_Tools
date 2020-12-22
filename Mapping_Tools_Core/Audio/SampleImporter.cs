using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools_Core.Audio {
    public class SampleImporter {
        public static bool ValidateSampleArgs(ISampleImportArgs args, bool validateSampleFile = true) {
            return !validateSampleFile || args.IsValid();
        }

        public static bool ValidateSampleArgs(ISampleImportArgs args, Dictionary<ISampleImportArgs, ISampleSoundGenerator> loadedSamples, bool validateSampleFile = true) {
            if (loadedSamples == null)
                return ValidateSampleArgs(args, validateSampleFile);
            return !validateSampleFile || loadedSamples.ContainsKey(args) && loadedSamples[args] != null;
        }
        
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
        public static Dictionary<ISampleImportArgs, ISampleSoundGenerator> ImportSamples(
            IEnumerable<ISampleImportArgs> argsList,
            IEqualityComparer<ISampleImportArgs> comparer = null) {

            if (comparer == null) {
                // Get the default comparer so it uses IEquatable
                comparer = EqualityComparer<ISampleImportArgs>.Default;
            }

            var samples = new Dictionary<ISampleImportArgs, ISampleSoundGenerator>(comparer);

            // Group the args by path so the SoundFont importer can benefit of caching
            var separatedByPath = new Dictionary<string, HashSet<ISampleImportArgs>>();
            var otherArgs = new HashSet<ISampleImportArgs>();

            foreach (var args in argsList) {
                if (args is IPathSampleImportArgs pathArgs) {
                    if (separatedByPath.TryGetValue(pathArgs.Path, out HashSet<ISampleImportArgs> value)) {
                        value.Add(args);
                    } else {
                        separatedByPath.Add(pathArgs.Path, new HashSet<ISampleImportArgs>(comparer) {args});
                    }
                } else {
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
