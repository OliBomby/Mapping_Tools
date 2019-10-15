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
        public static void ExportCompleteHitsounds(string exportFolder, string baseBeatmap, CompleteHitsounds ch, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null) {
            // Export the beatmap with all hitsounds
            ExportHitsounds(ch.Hitsounds, baseBeatmap, exportFolder);

            // Export the sample files
            ExportCustomIndices(ch.CustomIndices, exportFolder, loadedSamples);
        }

        public static void ExportHitsounds(List<HitsoundEvent> hitsounds, string baseBeatmap, string exportFolder) {
            BeatmapEditor editor = EditorReaderStuff.GetNewestVersion(baseBeatmap);
            Beatmap beatmap = editor.Beatmap;

            // Make new timing points
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add red lines
            List<TimingPoint> timingPoints = beatmap.BeatmapTiming.GetAllRedlines();
            foreach (TimingPoint tp in timingPoints) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true));
            }

            // Add hitsound stuff
            foreach (HitsoundEvent h in hitsounds) {
                TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                tp.Offset = h.Time;
                tp.SampleIndex = h.CustomIndex;
                tp.Volume = Math.Round(tp.Volume * h.Volume);
                timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
            }

            // Replace the old timingpoints
            beatmap.BeatmapTiming.TimingPoints.Clear();
            TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);

            // Replace all hitobjects with the hitsounds
            beatmap.HitObjects.Clear();
            foreach (HitsoundEvent h in hitsounds) {
                beatmap.HitObjects.Add(new HitObject(h.Time, h.GetHitsounds(), h.SampleSet, h.Additions));
            }

            // Change version to hitsounds
            beatmap.General["StackLeniency"] = new TValue("0.0");
            beatmap.General["Mode"] = new TValue("0");
            beatmap.Metadata["Version"] = new TValue("Hitsounds");
            beatmap.Difficulty["CircleSize"] = new TValue("4");

            // Save the file to the export folder
            editor.SaveFile(Path.Combine(exportFolder, beatmap.GetFileName()));
        }

        public static void ExportCustomIndices(List<CustomIndex> customIndices, string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null) {
            foreach (CustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    var samples = new List<ISampleProvider>();
                    var volumes = new List<float>();
                    int soundsAdded = 0;
                    
                    if (loadedSamples != null) {
                        foreach (SampleGeneratingArgs args in kvp.Value) {
                            if (SampleImporter.ValidateSampleArgs(args, loadedSamples)) {
                                var sample = loadedSamples[args];
                                samples.Add(sample.GetSampleProvider());
                                volumes.Add(sample.VolumeCorrection != -1 ? sample.VolumeCorrection : 1f);
                                soundsAdded++;
                            }
                        }
                    } else {
                        foreach (SampleGeneratingArgs args in kvp.Value) {
                            try {
                                var sample = SampleImporter.ImportSample(args);
                                samples.Add(sample.GetSampleProvider());
                                volumes.Add(sample.VolumeCorrection != -1 ? sample.VolumeCorrection : 1f);
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
                        Volume = 1 / (float)Math.Sqrt(soundsAdded * volumes.Average())
                    };

                    // TODO: Allow mp3, ogg and aif export.
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
