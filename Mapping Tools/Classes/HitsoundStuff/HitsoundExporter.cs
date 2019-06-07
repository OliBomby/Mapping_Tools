using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Wave;
using NAudio.Vorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundExporter {
        public static readonly string[] ValidSamplePathExtensions = new string[] { ".wav", ".ogg", ".mp3" };

        public static bool ValidateSamplePath(string path) {
            if (path == "")
                return false;

            string[] split = path.Split('?');
            string first = split[0];

            if (!File.Exists(first))
                return false;

            if (Path.GetExtension(first) == ".sf2") {
                if (split.Length < 2)
                    return false;
                
                if (!Regex.IsMatch(split[1], @"[0-9]+(\\[0-9]+){3}"))
                    return false;
            } else if (split.Length > 1)
                return false;
            else if (!ValidSamplePathExtensions.Contains(Path.GetExtension(first)))
                return false;

            return true;
        }

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
                foreach (KeyValuePair<string, HashSet<string>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    var mixer = new WaveMixerStream32 { AutoStop = true };
                    var waveChannels = new List<WaveChannel32>();
                    int soundsAdded = 0;

                    foreach (string path in kvp.Value) {
                        try {
                            string p = path.Split('?')[0];
                            WaveStream wave = Path.GetExtension(path) == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
                            waveChannels.Add(new WaveChannel32(wave));
                            soundsAdded++;
                        } catch (Exception) { }
                    }
                    if (soundsAdded == 0) {
                        continue;
                    }

                    foreach (var waveChannel in waveChannels) {
                        waveChannel.Volume = (float)(1 / Math.Sqrt(soundsAdded));
                        mixer.AddInputStream(waveChannel);
                    }

                    if (ci.Index == 1) {
                        CreateWaveFile(Path.Combine(exportFolder, kvp.Key + ".wav"), new Wave32To16Stream(mixer));
                    } else {
                        CreateWaveFile(Path.Combine(exportFolder, kvp.Key + ci.Index + ".wav"), new Wave32To16Stream(mixer));
                    }
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
