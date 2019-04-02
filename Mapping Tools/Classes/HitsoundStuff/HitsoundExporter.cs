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
        public static void ExportHitsounds(string exportFolder, string baseBeatmap, CompleteHitsounds ch) {
            Editor editor = new Editor(baseBeatmap);
            Beatmap beatmap = editor.Beatmap;

            // Resnap all hitsounds
            foreach (Hitsound h in ch.Hitsounds) {
                h.SetTime(Math.Floor(beatmap.BeatmapTiming.Resnap(h.Time, 16, 12)));
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
                TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5);
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
            beatmap.BeatmapTiming.TimingPoints = newTimingPoints;

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
