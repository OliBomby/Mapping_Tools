using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Core.Audio.DuplicateDetection;

public class MonolithicDuplicateSampleDetector : IDuplicateSampleDetector {
    public IDuplicateSampleMap AnalyzeSamples(IEnumerable<IBeatmapSetFileInfo> samples, out Exception exception) {
        var extList = GetSupportedExtensions();
        exception = null;

        List<IBeatmapSetFileInfo> samplesFiltered = samples
            .Where(n => extList.Contains(Path.GetExtension(n.Filename), StringComparer.OrdinalIgnoreCase)).ToList();

        Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo> dict = new Dictionary<IBeatmapSetFileInfo, IBeatmapSetFileInfo>();
        bool errorHappened = false;

        const int bufferSize = 2048;
        byte[] thisBuffer = new byte[bufferSize];
        byte[] otherBuffer = new byte[bufferSize];
            
        // Compare all samples to find ones with the same data
        for (int i = 0; i < samplesFiltered.Count; i++) {
            var sample1 = samplesFiltered[i];
            long thisLength = samplesFiltered[i].Size;

            // The first duplicate found defaults to itself
            var duplicate = sample1;

            for (int k = 0; k <= i; k++) {
                var sample2 = samplesFiltered[k];

                // Only have to compare against other samples
                if (sample1.Equals(sample2)) {
                    continue;
                }

                // Compare file size
                long otherLength = sample2.Size;
                if (thisLength != otherLength) {
                    continue;
                }

                // Try comparing the actual audio content
                try {
                    using var thisWave = Helpers.OpenSample(sample1.Filename, sample1.GetData());
                    using var otherWave = Helpers.OpenSample(sample2.Filename, sample2.GetData());

                    if (thisWave.Length != otherWave.Length) {
                        continue;
                    }

                    bool equal = true;
                    while (true) {
                        var bytesRead = thisWave.Read(thisBuffer, 0, bufferSize);
                        otherWave.Read(otherBuffer, 0, bufferSize);

                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        if (!thisBuffer.SequenceEqual(otherBuffer)) {
                            equal = false;
                            break;
                        }
                    }

                    if (!equal) {
                        continue;
                    }
                }
                catch (Exception ex) {
                    // Something went wrong reading the samples. I'll just assume they weren't the same
                    if (!errorHappened) {
                        exception = ex;
                        errorHappened = true;
                    }

                    Console.WriteLine(ex);
                    continue;
                }

                // i and k are make the same sound
                duplicate = samplesFiltered[k];
                break;
            }

            dict[samplesFiltered[i]] = duplicate;
        }

        return new DictionaryDuplicateSampleMap(dict);
    }

    public string[] GetSupportedExtensions() {
        return new[] { ".wav", ".aif", ".aiff", ".ogg", ".mp3" };
    }
}