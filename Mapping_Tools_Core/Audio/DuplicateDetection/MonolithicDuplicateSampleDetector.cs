using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools_Core.Audio.DuplicateDetection {
    public class MonolithicDuplicateSampleDetector : IDuplicateSampleDetector {
        public Dictionary<string, string> AnalyzeSamples(string dir, out Exception exception, bool includeSubdirectories) {
            var extList = GetSupportedExtensions();
            exception = null;

            List<string> samplePaths = Directory.GetFiles(dir, "*.*", 
                    includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(n => extList.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase)).ToList();

            Dictionary<string, string> dict = new Dictionary<string, string>();
            bool errorHappened = false;
            
            // Compare all samples to find ones with the same data
            for (int i = 0; i < samplePaths.Count; i++) {
                long thisLength = new FileInfo(samplePaths[i]).Length;

                for (int k = 0; k <= i; k++) {
                    if (samplePaths[i] != samplePaths[k]) {
                        long otherLength = new FileInfo(samplePaths[k]).Length;

                        if (thisLength != otherLength) {
                            continue;
                        }

                        try {
                            using var thisWave = Helpers.OpenSample(samplePaths[i]);
                            using var otherWave = Helpers.OpenSample(samplePaths[k]);

                            if (thisWave.Length != otherWave.Length) {
                                continue;
                            }

                            byte[] thisBuffer = new byte[thisWave.Length];
                            thisWave.Read(thisBuffer, 0, (int) thisWave.Length);

                            byte[] otherBuffer = new byte[otherWave.Length];
                            otherWave.Read(otherBuffer, 0, (int) otherWave.Length);

                            if (!thisBuffer.SequenceEqual(otherBuffer)) {
                                continue;
                            }
                        } catch (Exception ex) {
                            // Something went wrong reading the samples. I'll just assume they weren't the same
                            if (!errorHappened) {
                                exception = ex;
                                errorHappened = true;
                            }

                            Console.WriteLine(ex);
                            continue;
                        }
                    }

                    string samplePath = samplePaths[i];
                    string fullPathExtLess =
                        Path.Combine(Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                            Path.GetFileNameWithoutExtension(samplePath) ?? throw new InvalidOperationException());
                    dict[fullPathExtLess] = samplePaths[k];
                    break;
                }
            }

            return dict;
        }

        public string[] GetSupportedExtensions() {
            return new[] { ".wav", ".ogg", ".mp3" };
        }
    }
}