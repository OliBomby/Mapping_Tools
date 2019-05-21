using Mapping_Tools.Classes.BeatmapHelper;
using NAudio.Wave;
using NAudio.Vorbis;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundImporter {
        public static List<double> TimesFromStack(string path, double x, double y) {
            List<double> times = new List<double>();
            Editor editor = new Editor(path);

            bool xIgnore = x == -1;
            bool yIgnore = y == -1;

            foreach (HitObject ho in editor.Beatmap.HitObjects) {
                if ((Math.Abs(ho.Pos.X - x) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - y) < 3 || yIgnore)) {
                    times.Add(ho.Time);
                }
            }
            return times;
        }

        public static Dictionary<string, string> AnalyzeSamples(string dir) {
            var extList = new string[] { ".wav", ".ogg" };
            List<string> samplePaths = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(n => extList.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase))
                        .ToList();
            List<byte[]> audios = new List<byte[]>(samplePaths.Count);
            Dictionary<string, string> dict = new Dictionary<string, string>();

            // Read all samples
            foreach (string samplePath in samplePaths) {
                try {
                    WaveStream wave = new VorbisWaveReader(samplePath);
                    byte[] buffer = new byte[20000];
                    wave.Read(buffer, 0, Math.Min((int)wave.Length, 20000));
                    audios.Add(buffer);
                } catch (Exception) {
                    audios.Add(Encoding.UTF8.GetBytes(samplePath));
                }
            }

            for (int i = 0; i < audios.Count; i++) {
                for (int k = 0; k < audios.Count; k++) {
                    if (audios[i].SequenceEqual(audios[k])) {
                        dict[Path.GetFileNameWithoutExtension(samplePaths[i])] = Path.GetFileName(samplePaths[k]);
                        break;
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// Extract every used sample in a beatmap and return them as hitsound layers
        /// </summary>
        /// <param name="path">The path to the beatmap</param>
        /// <returns>The hitsound layers</returns>
        public static List<HitsoundLayer> LayersFromHitsounds(string path) {
            Editor editor = new Editor(path);
            Beatmap beatmap = editor.Beatmap;
            Timeline timeline = beatmap.GetTimeline();

            int mode = beatmap.General["Mode"].Value;
            string mapDir = editor.GetBeatmapFolder();
            Dictionary<string, string> firstSamples = AnalyzeSamples(mapDir);

            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            foreach (TimelineObject tlo in timeline.TimeLineObjects) {
                if (!tlo.HasHitsound) { continue; }

                List<Tuple<int, int, int>> samples = tlo.GetPlayingHitsounds();

                foreach (Tuple<int, int, int> sample in samples) {
                    int sampleSet = sample.Item1;
                    int hitsound = sample.Item2;
                    int index = sample.Item3;

                    string filename = tlo.Filename ?? GetFileName(sampleSet, hitsound, index);
                    string extLess = Path.GetFileNameWithoutExtension(filename);

                    // Simplify path if it doesn't exist
                    if (firstSamples.Keys.Contains(extLess)) {
                        filename = firstSamples[extLess];
                    } else {
                        filename = GetFileName(sampleSet, hitsound, -1);
                    }
                    string samplePath = Path.Combine(mapDir, filename);

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.SamplePath == samplePath);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(filename, "Hitsounds", path, sample.Item1, sample.Item2, samplePath);
                        newLayer.Times.Add(tlo.Time);
                        hitsoundLayers.Add(newLayer);
                    }
                }
            }

            // Sort layers by name
            hitsoundLayers = hitsoundLayers.OrderBy(o => o.Name).ToList();

            return hitsoundLayers;
        }

        public static string GetFileName(int sampleSet, int hitsound, int index) {
            if (index == 1) {
                return String.Format("{0}-hit{1}.wav", HitsoundConverter.SampleSets[sampleSet], HitsoundConverter.Hitsounds[hitsound]);
            }
            return String.Format("{0}-hit{1}{2}.wav", HitsoundConverter.SampleSets[sampleSet], HitsoundConverter.Hitsounds[hitsound], index);
        }

        public static List<HitsoundLayer> ImportMIDI(string path, bool keysounds, string sampleFolder="") {
            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            var strictMode = false;
            var mf = new MidiFile(path, strictMode);
            Dictionary<int, int> channelInstruments = new Dictionary<int, int>();

            for (int n = 0; n < mf.Tracks; n++) {
                foreach (var midiEvent in mf.Events[n]) {
                    if (midiEvent.CommandCode == MidiCommandCode.PatchChange) {
                        PatchChangeEvent patchChange = (PatchChangeEvent)midiEvent;
                        channelInstruments[patchChange.Channel] = patchChange.Patch;
                        continue;
                    }
                    else if (!MidiEvent.IsNoteOn(midiEvent)) {
                        continue;
                    }
                    
                    NoteOnEvent on = (NoteOnEvent)midiEvent;
                    
                    string instrument = on.Channel == 10 ? "Percussion" : PatchChangeEvent.GetPatchName(channelInstruments[on.Channel]);

                    string name = keysounds || on.Channel == 10 ? String.Format("{0}, {1}", instrument, on.NoteName) : instrument;
                    string filename = keysounds || on.Channel == 10 ? String.Format("{0}\\{1}.wav", instrument, on.NoteName) : String.Format("{0}.wav", instrument);

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.Name == name);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(on.AbsoluteTime);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(name, "MIDI", path, 1, 0, Path.Combine(sampleFolder, filename)) {
                            Keysound = keysounds
                        };
                        newLayer.Times.Add(on.AbsoluteTime);
                        hitsoundLayers.Add(newLayer);
                    }
                }
            }

            // Sort the times
            hitsoundLayers.ForEach(o => o.Times = o.Times.OrderBy(t => t).ToList());

            // Sort layers by name
            hitsoundLayers = hitsoundLayers.OrderBy(o => o.Name).ToList();

            return hitsoundLayers;
        }
    }
}
