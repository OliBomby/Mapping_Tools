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
        public static void ExportHitsounds(string exportFolder, string baseBeatmap, CompleteHitsounds ch) {
            Editor editor = new Editor(baseBeatmap);
            Beatmap beatmap = editor.Beatmap;

            // Resnap all hitsounds
            foreach (Hitsound h in ch.Hitsounds) {
                h.SetTime(beatmap.BeatmapTiming.Resnap(h.Time, 16, 12));
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            List<TimingPoint> redlines = beatmap.BeatmapTiming.GetAllRedlines();
            foreach (TimingPoint tp in redlines) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true));
            }

            // Add hitsound stuff
            foreach (Hitsound h in ch.Hitsounds) {
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
            foreach (Hitsound h in ch.Hitsounds) {
                beatmap.HitObjects.Add(new HitObject(h.Time, h.GetHitsounds(), h.SampleSet, h.Additions));
            }

            // Change version to hitsounds
            beatmap.Metadata["Version"] = new TValue("Hitsounds");

            // Save the file to the export folder
            editor.SaveFile(Path.Combine(exportFolder, beatmap.GetFileName()));

            // Export the sample files
            foreach (CustomIndex ci in ch.CustomIndices) {
                foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    var samples = new List<ISampleProvider>();
                    int soundsAdded = 0;

                    foreach (SampleGeneratingArgs generator in kvp.Value) {
                        try {
                            var wave = SampleImporter.ImportSample(generator);
                            samples.Add(wave);
                            soundsAdded++;
                        } catch (Exception) { }
                    }
                    if (soundsAdded == 0) {
                        continue;
                    }
                    
                    var mixer = new MixingSampleProvider(samples);
                    VolumeSampleProvider volumed = new VolumeSampleProvider(mixer) {
                        Volume = 1 / (float)Math.Sqrt(soundsAdded)
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
            } catch (Exception) { }
        }
    }
}
