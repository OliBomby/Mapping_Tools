using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundExporter {
        public static void ExportCompleteHitsounds(string exportFolder, string baseBeatmap, CompleteHitsounds ch, Dictionary<SampleGeneratingArgs, ISampleProvider> loadedSamples = null) {
            // Export the beatmap with all hitsounds
            ExportHitsounds(ch.Hitsounds, baseBeatmap, exportFolder);

            // Export the sample files
            ExportCustomIndices(ch.CustomIndices, exportFolder, loadedSamples);
        }

        public static void ExportHitsounds(List<Hitsound> hitsounds, string baseBeatmap, string exportFolder) {
            Editor editor = new Editor(baseBeatmap);
            Beatmap beatmap = editor.Beatmap;

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            List<TimingPoint> redlines = beatmap.BeatmapTiming.GetAllRedlines();
            foreach (TimingPoint tp in redlines) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true));
            }

            // Add hitsound stuff
            foreach (Hitsound h in hitsounds) {
                TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                tp.Offset = h.Time;
                tp.SampleIndex = h.CustomIndex;
                timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
            }

            // Replace the old timingpoints
            beatmap.BeatmapTiming.TimingPoints.Clear();
            TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

            // Replace all hitobjects with the hitsounds
            beatmap.HitObjects.Clear();
            foreach (Hitsound h in hitsounds) {
                beatmap.HitObjects.Add(new HitObject(h.Time, h.GetHitsounds(), h.SampleSet, h.Additions));
            }

            // Change version to hitsounds
            beatmap.Metadata["Version"] = new TValue("Hitsounds");

            // Save the file to the export folder
            editor.SaveFile(Path.Combine(exportFolder, beatmap.GetFileName()));
        }

        public static void ExportCustomIndices(List<CustomIndex> customIndices, string exportFolder, Dictionary<SampleGeneratingArgs, ISampleProvider> loadedSamples=null) {
            foreach (CustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    var samples = new List<ISampleProvider>();
                    int soundsAdded = 0;

                    if (loadedSamples != null) {
                        foreach (SampleGeneratingArgs args in kvp.Value) {
                            if (SampleImporter.ValidateSampleArgs(args, loadedSamples)) {
                                samples.Add(loadedSamples[args]);
                                soundsAdded++;
                            }
                        }
                    } else {
                        foreach (SampleGeneratingArgs args in kvp.Value) {
                            try {
                                samples.Add(SampleImporter.ImportSample(args));
                                soundsAdded++;
                            } catch (Exception) { }
                        }

                    }
                    if (soundsAdded == 0) {
                        continue;
                    }

                    int maxSampleRate = samples.Max(o => o.WaveFormat.SampleRate);
                    int maxChannels = samples.Max(o => o.WaveFormat.Channels);
                    IEnumerable<ISampleProvider> sameFormatSamples = samples.Select(o => (ISampleProvider)new WdlResamplingSampleProvider(SampleImporter.SetChannels(o, maxChannels), maxSampleRate));

                    var mixer = new MixingSampleProvider(sameFormatSamples);

                    VolumeSampleProvider volumed = new VolumeSampleProvider(mixer) {
                        Volume = samples.Count > 1 ? 0.8f : 1f
                    };

                    string filename = ci.Index == 1 ? kvp.Key + ".wav" : kvp.Key + ci.Index + ".wav";
                    CreateWaveFile(Path.Combine(exportFolder, filename), volumed.ToWaveProvider16());
                }
            }
        }

        private static void CreateWaveFile(string filename, IWaveProvider sourceProvider) {
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
            } catch (IndexOutOfRangeException) { }
        }
    }
}
