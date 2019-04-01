using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundExporter {
        public static void ExportHitsounds(string exportFolder, Beatmap baseBeatmap, CompleteHitsounds ch) {
            // Resnap all hitsounds
            foreach (Hitsound h in ch.Hitsounds) {
                h.SetTime(Math.Floor(baseBeatmap.BeatmapTiming.Resnap(h.Time, 16, 12)));
            }

            // Make new timingpoints
            List<TimingPointsChange> timingPointsChanges = new List<TimingPointsChange>();

            // Add redlines
            List<TimingPoint> redlines = baseBeatmap.BeatmapTiming.GetAllRedlines();
            foreach (TimingPoint tp in redlines) {
                timingPointsChanges.Add(new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true));
            }

            // Add hitsound stuff
            foreach (Hitsound h in ch.Hitsounds) {
                TimingPoint tp = baseBeatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5);
                tp.Offset = h.Time;
                tp.SampleIndex = h.CustomIndex;
                timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
            }

            // Replace the old timingpoints
            timingPointsChanges = timingPointsChanges.OrderBy(o => o.MyTP.Offset).ToList();
            List<TimingPoint> newTimingPoints = new List<TimingPoint>();
            foreach (TimingPointsChange c in timingPointsChanges) {
                c.AddChange(newTimingPoints);
            }
            baseBeatmap.BeatmapTiming.TimingPoints = newTimingPoints;

            // Replace all hitobjects with the hitsounds
            baseBeatmap.HitObjects.Clear();
            foreach (Hitsound h in ch.Hitsounds) {
                baseBeatmap.HitObjects.Add(new HitObject(h.Time, h.GetHitsounds(), h.SampleSet, h.Additions));
            }

            // Change version to hitsounds
            baseBeatmap.Metadata["Version"] = new TValue("Hitsounds");

            // Save the file to the export folder
            Editor.SaveFile(Path.Combine(exportFolder, baseBeatmap.GetFileName()), baseBeatmap.GetLines());

            // Export the sample files
            foreach (CustomIndex ci in ch.CustomIndices) {
                foreach (KeyValuePair<string, HashSet<string>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    var mixer = new WaveMixerStream32 { AutoStop = true };

                    foreach (string path in kvp.Value) {
                        try {
                            var wav = new WaveFileReader(path);
                            mixer.AddInputStream(new WaveChannel32(wav));
                        } catch (Exception) { }
                    }

                    if (ci.Index == 1) {
                        WaveFileWriter.CreateWaveFile(Path.Combine(exportFolder, kvp.Key + ".wav"), new Wave32To16Stream(mixer));
                    } else {
                        WaveFileWriter.CreateWaveFile(Path.Combine(exportFolder, kvp.Key + ci.Index + ".wav"), new Wave32To16Stream(mixer));
                    }
                }
            }
        }
    }
}
