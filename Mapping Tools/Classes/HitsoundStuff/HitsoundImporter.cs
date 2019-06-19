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
                    WaveStream wave = Path.GetExtension(samplePath) == ".ogg" ? (WaveStream)new VorbisWaveReader(samplePath) : new MediaFoundationReader(samplePath);
                    byte[] buffer = new byte[2000];
                    wave.Read(buffer, 0, Math.Min((int)wave.Length, 2000));
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
                    extLess = Path.GetFileNameWithoutExtension(filename);

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.SampleArgs.Path == samplePath);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(extLess, "Hitsounds", path, sample.Item1, sample.Item2, samplePath);
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

        public static List<HitsoundLayer> ImportMIDI(string path, bool instruments=true, bool keysounds=true, bool lengths=true, bool velocities=true, string sampleSource="") {
            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            var strictMode = false;
            var mf = new MidiFile(path, strictMode);

            Console.WriteLine("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}",
                mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);

            Dictionary<int, int> channelBanks = new Dictionary<int, int>();
            Dictionary<int, int> channelPatches = new Dictionary<int, int>();

            // Loop through every event of every track
            for (int track = 0; track < mf.Tracks; track++) {
                foreach (var midiEvent in mf.Events[track]) {
                    if (midiEvent is PatchChangeEvent pc) {
                        channelPatches[pc.Channel] = pc.Patch;
                        continue;
                    }
                    else if (midiEvent is ControlChangeEvent co) {
                        if (co.Controller == MidiController.BankSelect) {
                            channelBanks[co.Channel] = (co.ControllerValue * 128) + (channelBanks.ContainsKey(co.Channel) ? (byte)channelBanks[co.Channel] : 0);
                        }
                        else if (co.Controller == MidiController.BankSelectLsb) {
                            channelBanks[co.Channel] = co.ControllerValue + (channelBanks.ContainsKey(co.Channel) ? channelBanks[co.Channel] >> 8 * 128 : 0);
                        }
                    }
                    else if (midiEvent is TempoEvent te) {
                        Console.WriteLine(te.Channel);
                        Console.WriteLine(te.MicrosecondsPerQuarterNote);
                        Console.WriteLine(te.Tempo);
                    }
                    else if (midiEvent is NoteOnEvent on) {
                        bool keys = keysounds || on.Channel == 10;

                        int bank = instruments ? on.Channel == 10 ? 128 : channelBanks.ContainsKey(on.Channel) ? channelBanks[on.Channel] : 0 : -1;
                        int patch = instruments && channelPatches.ContainsKey(on.Channel) ? channelPatches[on.Channel] : -1;
                        int instrument = -1;
                        int key = keys ? on.NoteNumber : -1;
                        int length = lengths ? on.NoteLength : -1;
                        int velocity = velocities ? on.Velocity : -1;

                        string instrumentName = patch >= 0 && patch <= 127 ? PatchChangeEvent.GetPatchName(patch) : on.Channel == 10 ? "Percussion" : "Undefined";
                        string keyName = on.NoteName;

                        string name = instrumentName;
                        if (keysounds)
                            name += "," + keyName;
                        if (lengths)
                            name += "," + length;
                        if (velocities)
                            name += "," + velocity;

                        string filename = Path.GetExtension(sampleSource) == ".sf2" ?
                            sampleSource :
                            Path.Combine(new string[5] { sampleSource, patch.ToString(), key.ToString(), length.ToString(), velocity.ToString() + ".wav" });

                        SampleGeneratingArgs args = new SampleGeneratingArgs(filename, bank, patch, instrument, key, length, velocity);

                        // Find the hitsoundlayer with this path
                        HitsoundLayer layer = hitsoundLayers.Find(o => o.SampleArgs == args);

                        if (layer != null) {
                            // Find hitsound layer with this path and add this time
                            layer.Times.Add(on.AbsoluteTime);
                        } else {
                            // Add new hitsound layer with this path
                            HitsoundLayer newLayer = new HitsoundLayer(name, "MIDI", path, 1, 0, args);
                            hitsoundLayers.Add(newLayer);

                            newLayer.Times.Add(on.AbsoluteTime);
                        }
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
