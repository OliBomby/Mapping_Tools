using Mapping_Tools_Core.Audio.Exporting;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.Tools.HitsoundStudio {
    public class HitsoundExporter {
        public static void ExportSample(ISampleGenerator sample, ISampleExporter exporter, string name = null) {
            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                pathAudioSampleExporter.ExportName = name ?? sample.GetName();
            }

            sample.ToExporter(exporter);
            exporter.Flush();
        }

        public static void ExportMixedSample(IEnumerable<ISampleGenerator> samples, ISampleExporter exporter, string name) {
            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                pathAudioSampleExporter.ExportName = name;
            }

            foreach (var sample in samples) {
                sample.ToExporter(exporter);
            }

            exporter.Flush();
        }

        public static void ExportSamples(ICollection<ISampleGenerator> samples, ISampleExporter exporter, Dictionary<ISampleGenerator, string> names = null) {
            if (names == null) {
                names = GenerateSampleNames(samples);
            }

            foreach (var sample in samples.Where(sample => sample.IsValid())) {
                ExportSample(sample, exporter, names[sample]);
            }
        }

        /// <summary>
        /// Exports all samples for a collection of custom indices.
        /// </summary>
        /// <param name="customIndices"></param>
        /// <param name="exporter"></param>
        public static void ExportCustomIndices(List<ICustomIndex> customIndices, ISampleExporter exporter) {
            foreach (ICustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<ISampleGenerator>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    
                    string filename = ci.Index == 1 ? kvp.Key : kvp.Key + ci.Index;
                    ExportMixedSample(kvp.Value, exporter, filename);
                }
            }
        }

        public static void ExportSampleSchema(ISampleSchema sampleSchema, ISampleExporter exporter) {
            foreach (var kvp in sampleSchema) {
                ExportMixedSample(kvp.Value, exporter, kvp.Key);
            }
        }

        public static Dictionary<ISampleGenerator, string> GenerateSampleNames(IEnumerable<ISampleGenerator> samples) {
            var usedNames = new HashSet<string>();
            var sampleNames = new Dictionary<ISampleGenerator, string>();
            foreach (var sample in samples) {
                if (!sample.IsValid()) {
                    sampleNames[sample] = string.Empty;
                    continue;
                }
                
                var baseName = sample.GetName();
                var name = baseName;
                int i = 1;

                while (usedNames.Contains(name)) {
                    name = baseName + "-" + ++i; 
                }

                usedNames.Add(name);
                sampleNames[sample] = name;
            }

            return sampleNames;
        }

        public static void AddNewSampleName(Dictionary<ISampleGenerator, string> sampleNames, ISampleGenerator sample) {
            if (!sample.IsValid()) {
                sampleNames[sample] = string.Empty;
                return;
            }
            
            var baseName = sample.GetName();
            var name = baseName;
            int i = 1;

            while (sampleNames.ContainsValue(name)) {
                name = baseName + "-" + ++i; 
            }

            sampleNames[sample] = name;
        }

        public static Dictionary<ISample, Vector2> GenerateHitsoundPositions(IEnumerable<ISample> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // Find the biggest spacing that will still fit all the samples
            int spacingX = 128;
            int spacingY = 128;
            bool reduceX = false;
            while ((int) (512d / spacingX + 1) * (int) (384d / spacingY + 1) < sampleCount && spacingX > 1) {
                reduceX = !reduceX;
                if (reduceX)
                    spacingX /= 2;
                else
                    spacingY /= 2;
            }

            var positions = new Dictionary<ISample, Vector2>();
            int x = 0;
            int y = 0;
            foreach (var sample in sampleArray) {
                positions.Add(sample, new Vector2(x, y));

                x += spacingX;
                if (x > 512) {
                    x = 0;
                    y += spacingY;

                    if (y > 384) {
                        y = 0;
                    }
                }
            }

            return positions;
        }

        public static Dictionary<ISample, Vector2> GenerateManiaHitsoundPositions(IEnumerable<ISample> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // One key per unique sample but clamped between 1 and 18
            int numKeys = MathHelper.Clamp(sampleCount, 1, 18);

            var positions = new Dictionary<ISample, Vector2>();
            double x = 256d / numKeys;
            foreach (var sample in sampleArray) {
                positions.Add(sample, new Vector2(Math.Round(x), 192));

                x += 512d / numKeys;
                if (x > 512) {
                    x = 256d / numKeys;
                }
            }

            return positions;
        }
    }
}
