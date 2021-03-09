using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Events;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools_Core.Tools.HitsoundStudio {
    public class HitsoundExporter {
        public enum SampleExportFormat {
            Default,
            WaveIeeeFloat,
            WavePcm,
            OggVorbis,
            MidiChords
        }

        public static void ExportHitsounds(ICollection<IHitsoundEvent> hitsounds, 
            string baseBeatmap, 
            string exportFolder, 
            string exportMapName, 
            GameMode exportGameMode, 
            bool useGreenlines, 
            bool useStoryboard,
            IReadWriteEditor<Beatmap> editor = null) {

            editor = editor ?? new BeatmapEditor();
            editor.Path = baseBeatmap;
            Beatmap beatmap = editor.ReadFile();

            if (useStoryboard) {
                beatmap.StoryboardSoundSamples.Clear();
                foreach (var h in hitsounds.Where(h => !String.IsNullOrEmpty(h.Filename))) {
                    beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample((int) Math.Round(h.Time), 0, h.Filename, h.Volume * 100));
                }
            } else {
                // Make new timing points
                // Add red lines
                var redlines = beatmap.BeatmapTiming.Redlines;
                List<TimingPointsChange> timingPointsChanges = redlines.Select(tp =>
                        new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true))
                    .ToList();

                // Add hitsound stuff
                // Replace all hitobjects with the hitsounds
                beatmap.HitObjects.Clear();
                foreach (IHitsoundEvent h in hitsounds) {
                    var index = h.CustomIndex;
                    var volume = h.Volume;

                    if (useGreenlines) {
                        TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                        tp.Offset = h.Time;
                        tp.SampleIndex = h.CustomIndex;
                        index = 0; // Set it to default value because it gets handled by greenlines now
                        tp.Volume = Math.Round(tp.Volume * h.Volume);
                        volume = 0; // Set it to default value because it gets handled by greenlines now
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
                    }

                    beatmap.HitObjects.Add(new HitObject(h.Pos, h.Time, 5, h.GetHitsounds(), h.SampleSet, h.Additions,
                        index, volume * 100, h.Filename));
                }

                // Replace the old timingpoints
                beatmap.BeatmapTiming.Clear();
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }

            // Change version to hitsounds
            beatmap.General["StackLeniency"] = new TValue("0.0");
            beatmap.General["Mode"] = new TValue(((int) exportGameMode).ToInvariant());
            beatmap.Metadata["Version"] = new TValue(exportMapName);

            if (exportGameMode == GameMode.Mania) {
                // Count the number of distinct X positions
                int numXPositions =hitsounds.Select(h => h.Pos.X).Distinct().Count();
                int numKeys = MathHelper.Clamp(numXPositions, 1, 18);

                beatmap.Difficulty["CircleSize"] = new TValue(numKeys.ToInvariant());
            } else {
                beatmap.Difficulty["CircleSize"] = new TValue("4");
            }

            // Save the file to the export folder
            editor.Path = Path.Combine(exportFolder, beatmap.GetFileName());
            editor.WriteFile(beatmap);
        }

        public static void ExportLoadedSamples(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples,
            string exportFolder, Dictionary<ISampleGeneratingArgs, string> names = null, 
            SampleExportFormat format=SampleExportFormat.Default) {
            if (names == null) {
                names = GenerateSampleNames(loadedSamples.Keys, loadedSamples);
            }

            foreach (var sample in loadedSamples.Keys.Where(sample => sample.IsValid(loadedSamples))) {
                ExportSample(sample, names[sample], exportFolder, loadedSamples, format);
            }
        }

        /// <summary>
        /// Exports all samples for a collection of custom indices.
        /// </summary>
        /// <param name="customIndices"></param>
        /// <param name="exportFolder"></param>
        /// <param name="loadedSamples"></param>
        /// <param name="format"></param>
        /// <param name="mixedFormat"></param>
        public static void ExportCustomIndices(List<ICustomIndex> customIndices, string exportFolder, 
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default, SampleExportFormat mixedFormat=SampleExportFormat.Default) {
            foreach (ICustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<ISampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    
                    string filename = ci.Index == 1 ? kvp.Key : kvp.Key + ci.Index;
                    ExportMixedSample(kvp.Value, filename, exportFolder, loadedSamples, format, mixedFormat);
                }
            }
        }

        public static void ExportSampleSchema(ISampleSchema sampleSchema, string exportFolder,
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples = null,
            SampleExportFormat format = SampleExportFormat.Default, SampleExportFormat mixedFormat = SampleExportFormat.Default) {
            foreach (var kvp in sampleSchema) {
                ExportMixedSample(kvp.Value, kvp.Key, exportFolder, loadedSamples, format, mixedFormat);
            }
        }

        private static bool CreateWaveFile(string filename, IWaveProvider sourceProvider) {
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

        public static Dictionary<ISampleGeneratingArgs, string> GenerateSampleNames(IEnumerable<ISampleGeneratingArgs> samples, 
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            var usedNames = new HashSet<string>();
            var sampleNames = new Dictionary<ISampleGeneratingArgs, string>();
            foreach (var sample in samples) {
                if (!sample.IsValid(loadedSamples)) {
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

        public static void AddNewSampleName(Dictionary<ISampleGeneratingArgs, string> sampleNames, ISampleGeneratingArgs sample,
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            if (!sample.IsValid(loadedSamples)) {
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

        public static Dictionary<ISampleGeneratingArgs, Vector2> GenerateHitsoundPositions(IEnumerable<ISampleGeneratingArgs> samples) {
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

            var positions = new Dictionary<ISampleGeneratingArgs, Vector2>();
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

        public static Dictionary<ISampleGeneratingArgs, Vector2> GenerateManiaHitsoundPositions(IEnumerable<ISampleGeneratingArgs> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // One key per unique sample but clamped between 1 and 18
            int numKeys = MathHelper.Clamp(sampleCount, 1, 18);

            var positions = new Dictionary<ISampleGeneratingArgs, Vector2>();
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
