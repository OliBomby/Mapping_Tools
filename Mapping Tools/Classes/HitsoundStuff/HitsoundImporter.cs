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
using System.Globalization;

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

        public static Dictionary<string, string> AnalyzeSamples(string dir, bool extended=false) {
            var extList = new string[] { ".wav", ".ogg", ".mp3" };
            List<string> samplePaths = Directory.GetFiles(dir, "*.*", extended ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
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
                        string samplePath = samplePaths[i];
                        string fullPathExtLess = Path.Combine(Path.GetDirectoryName(samplePath), Path.GetFileNameWithoutExtension(samplePath));
                        dict[fullPathExtLess] = samplePaths[k];
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

                List<string> samples = tlo.GetPlayingFilenames();

                foreach (string filename in samples) {
                    string samplePath = Path.Combine(mapDir, filename);
                    string fullPathExtLess = Path.Combine(Path.GetDirectoryName(samplePath), Path.GetFileNameWithoutExtension(samplePath));

                    // Get the first occurence of this sound to not get duplicated
                    if (firstSamples.Keys.Contains(fullPathExtLess)) {
                        samplePath = firstSamples[fullPathExtLess];
                    }
                    
                    string extLessFilename = Path.GetFileNameWithoutExtension(samplePath);

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.SampleArgs.Path == samplePath);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(extLessFilename, ImportType.Hitsounds, path, GetSamplesetFromFilename(filename), GetHitsoundFromFilename(filename), samplePath);
                        newLayer.Times.Add(tlo.Time);
                        hitsoundLayers.Add(newLayer);
                    }
                }
            }

            // Sort layers by name
            hitsoundLayers = hitsoundLayers.OrderBy(o => o.Name).ToList();

            return hitsoundLayers;
        }

        public static int GetSamplesetFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 1)
                return 0;
            string sampleset = split[0];
            switch (sampleset) {
                case "auto":
                    return 0;
                case "normal":
                    return 1;
                case "soft":
                    return 2;
                case "drum":
                    return 3;
                default:
                    return 0;
            }
        }

        public static int GetHitsoundFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 2)
                return 0;
            string hitsound = split[1];
            if (hitsound.Contains("hitnormal"))
                return 0;
            if (hitsound.Contains("hitwhistle"))
                return 1;
            if (hitsound.Contains("hitfinish"))
                return 2;
            if (hitsound.Contains("hitclap"))
                return 3;
            return 0;
        }

        public static List<HitsoundLayer> ImportMIDI(string path, bool instruments=true, bool keysounds=true, bool lengths=true, double lengthRoughness=1, bool velocities=true, double velocityRoughness=1) {
            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            var strictMode = false;
            var mf = new MidiFile(path, strictMode);

            Console.WriteLine("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}",
                mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);

            List<TempoEvent> tempos = mf.Events[0].OfType<TempoEvent>().ToList();
            List<double> cumulativeTime = CalculateCumulativeTime(tempos, mf.DeltaTicksPerQuarterNote);

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
                    else if (MidiEvent.IsNoteOn(midiEvent)) {
                        var on = midiEvent as NoteOnEvent;

                        double time = CalculateTime(on.AbsoluteTime, tempos, cumulativeTime, mf.DeltaTicksPerQuarterNote);
                        double length = on.OffEvent != null ? CalculateTime(on.OffEvent.AbsoluteTime, tempos, cumulativeTime, mf.DeltaTicksPerQuarterNote) - time : -1;
                        length = RoundLength(length, lengthRoughness);

                        bool keys = keysounds || on.Channel == 10;

                        int bank = instruments ? on.Channel == 10 ? 128 : channelBanks.ContainsKey(on.Channel) ? channelBanks[on.Channel] : 0 : -1;
                        int patch = instruments && channelPatches.ContainsKey(on.Channel) ? channelPatches[on.Channel] : -1;
                        int instrument = -1;
                        int key = keys ? on.NoteNumber : -1;
                        length = lengths ? length : -1;
                        int velocity = velocities ? on.Velocity : -1;
                        velocity = (int)RoundVelocity(velocity, velocityRoughness);

                        string lengthString = Math.Round(length).ToString(CultureInfo.InvariantCulture);
                        string filename = String.Format("{0}\\{1}\\{2}\\{3}\\{4}\\{5}.wav", bank, patch, instrument, key, lengthString, velocity);

                        string instrumentName = on.Channel == 10 ? "Percussion" : patch >= 0 && patch <= 127 ? PatchChangeEvent.GetPatchName(patch) : "Undefined";
                        string keyName = on.NoteName;

                        string name = instrumentName;
                        if (keysounds)
                            name += "," + keyName;
                        if (lengths)
                            name += "," + lengthString;
                        if (velocities)
                            name += "," + velocity;


                        SampleGeneratingArgs args = new SampleGeneratingArgs(filename, bank, patch, instrument, key, length, velocity);

                        // Find the hitsoundlayer with this path
                        HitsoundLayer layer = hitsoundLayers.Find(o => o.SampleArgs.ExactlyEquals(args));

                        if (layer != null) {
                            // Find hitsound layer with this path and add this time
                            layer.Times.Add(time);
                        } else {
                            // Add new hitsound layer with this path
                            HitsoundLayer newLayer = new HitsoundLayer(name, ImportType.MIDI, path, 1, 0, args);
                            hitsoundLayers.Add(newLayer);

                            newLayer.Times.Add(time);
                        }
                    }
                }
            }
            // Stretch the velocities to reach 127
            int maxVelocity = hitsoundLayers.Max(o => o.SampleArgs.Velocity);
            foreach (var hsl in hitsoundLayers) {
                hsl.SampleArgs.Velocity = (int)Math.Round(hsl.SampleArgs.Velocity / (float)maxVelocity * 127);
            }

            // Sort the times
            hitsoundLayers.ForEach(o => o.Times = o.Times.OrderBy(t => t).ToList());

            // Sort layers by name
            hitsoundLayers = hitsoundLayers.OrderBy(o => o.Name).ToList();

            return hitsoundLayers;
        }

        private static double RoundVelocity(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var mult = length / roughness;
            var round = Math.Round(mult);
            return round * roughness;
        }

        private static double RoundLength(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var mult = Math.Pow(length, 1 / roughness);
            var round = Math.Ceiling(mult);
            return Math.Pow(round, roughness);
        }

        private static double CalculateTime(long absoluteTime, List<TempoEvent> tempos, List<double> cumulativeTime, int dtpq) {
            TempoEvent prev = null;
            double prevTime = 0;
            for (int i = 0; i < tempos.Count; i++) {
                if (tempos[i].AbsoluteTime <= absoluteTime) {
                    prev = tempos[i];
                    prevTime = cumulativeTime[i];
                } else {
                    break;
                }
            }

            double deltaTime = prev.MicrosecondsPerQuarterNote / 1000d * (absoluteTime - prev.AbsoluteTime) / dtpq;
            return prevTime + deltaTime;
        }

        private static List<double> CalculateCumulativeTime(List<TempoEvent> tempos, int dtpq) {
            // Time is in miliseconds
            List<double> times = new List<double>(tempos.Count);

            TempoEvent last = null;
            foreach (TempoEvent te in tempos) {
                if (last == null) {
                    times.Add(te.AbsoluteTime);
                } else {
                    long deltaTicks = te.AbsoluteTime - last.AbsoluteTime;
                    double deltaTime = last.MicrosecondsPerQuarterNote / 1000d * deltaTicks / dtpq;

                    times.Add(times.Last() + deltaTime);
                }

                last = te;
                //Console.WriteLine(String.Format("{0},359.842,4,2,1,100,1,0", (int)times.Last(), (last.MicrosecondsPerQuarterNote / 1000f).ToString(CultureInfo.InvariantCulture)));
            }
            return times;
        }
    }
}
