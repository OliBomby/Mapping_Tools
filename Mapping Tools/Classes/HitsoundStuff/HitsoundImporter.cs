using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundImporter {
        public static List<double> TimesFromStack(string path, double x, double y) {
            List<double> times = new List<double>();
            var editor = EditorReaderStuff.GetNewestVersionOrNot(path);

            bool xIgnore = x == -1;
            bool yIgnore = y == -1;

            foreach (HitObject ho in editor.Beatmap.HitObjects) {
                if ((Math.Abs(ho.Pos.X - x) < 3 || xIgnore) && (Math.Abs(ho.Pos.Y - y) < 3 || yIgnore)) {
                    times.Add(ho.Time);
                }
            }
            return times;
        }

        public static HitsoundLayer ImportStack(string path, double x, double y) {
            HitsoundLayer layer = new HitsoundLayer
            {
                ImportArgs = {ImportType = ImportType.Stack, Path = path, X = x, Y = y},
                Times = TimesFromStack(path, x, y)
            };
            return layer;
        }

        /// <summary>
        /// Analyses all sound samples in a folder and generates a mapping from a full path without extension to the full path of the first sample which makes the same sound.
        /// Use this to detect duplicate samples.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="extended"></param>
        /// <param name="detectDuplicateSamples"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AnalyzeSamples(string dir, bool extended=false, bool detectDuplicateSamples=true) {
            var extList = new[] { ".wav", ".ogg", ".mp3" };
            List<string> samplePaths = Directory.GetFiles(dir, "*.*", extended ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Where(n => extList.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase)).ToList();

            Dictionary<string, string> dict = new Dictionary<string, string>();
            bool error = false;
            
            // Compare all samples to find ones with the same data
            if (detectDuplicateSamples) {
                for (int i = 0; i < samplePaths.Count; i++) {
                    long thisLength = new FileInfo(samplePaths[i]).Length;

                    for (int k = 0; k <= i; k++) {
                        if (samplePaths[i] != samplePaths[k]) {
                            long otherLength = new FileInfo(samplePaths[k]).Length;

                            if (thisLength != otherLength) {
                                continue;
                            }

                            try {
                                using (var thisWave = SampleImporter.OpenSample(samplePaths[i])) {
                                    using (var otherWave = SampleImporter.OpenSample(samplePaths[k])) {
                                        if (thisWave.Length != otherWave.Length) {
                                            continue;
                                        }

                                        byte[] thisBuffer = new byte[thisWave.Length];
                                        thisWave.Read(thisBuffer, 0, (int) thisWave.Length);

                                        byte[] otherBuffer = new byte[otherWave.Length];
                                        otherWave.Read(otherBuffer, 0, (int) otherWave.Length);

                                        if (!thisBuffer.SequenceEqual(otherBuffer)) {
                                            continue;
                                        }
                                    }
                                }
                            } catch (Exception ex) {
                                // Something went wrong reading the samples. I'll just assume they weren't the same
                                if (!error) {
                                    MessageBox.Show($"Exception '{ex.Message}' while trying to analyze samples.",
                                        "Warning");
                                    ex.Show();
                                    error = true;
                                }

                                continue;
                            }
                        }

                        string samplePath = samplePaths[i];
                        string fullPathExtLess =
                            Path.Combine(Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                                Path.GetFileNameWithoutExtension(samplePath));
                        dict[fullPathExtLess] = samplePaths[k];
                        break;
                    }
                }
            } else {
                foreach (var samplePath in samplePaths) {
                    string fullPathExtLess =
                        Path.Combine(Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                            Path.GetFileNameWithoutExtension(samplePath));
                    dict[fullPathExtLess] = samplePath;
                }
            }

            return dict;
        }

        /// <summary>
        /// Extract every used sample in a beatmap and return them as hitsound layers.
        /// </summary>
        /// <param name="path">The path to the beatmap.</param>
        /// <param name="volumes">Taking the volumes from the map and making different layers for different volumes.</param>
        /// <param name="detectDuplicateSamples">Detect duplicate samples and optimise hitsound layer count with that.</param>
        /// <param name="removeDuplicates">Removes duplicate sounds at the same millisecond.</param>
        /// <param name="includeStoryboard">Also imports storyboarded samples.</param>
        /// <returns>The hitsound layers</returns>
        public static List<HitsoundLayer> ImportHitsounds(string path, bool volumes, bool detectDuplicateSamples, bool removeDuplicates, bool includeStoryboard) {
            var editor = EditorReaderStuff.GetNewestVersionOrNot(path);
            Beatmap beatmap = editor.Beatmap;
            Timeline timeline = beatmap.GetTimeline();

            GameMode mode = (GameMode)beatmap.General["Mode"].IntValue;
            string mapDir = editor.GetParentFolder();
            Dictionary<string, string> firstSamples = AnalyzeSamples(mapDir, false, detectDuplicateSamples);

            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            foreach (TimelineObject tlo in timeline.TimelineObjects) {
                if (!tlo.HasHitsound) { continue; }

                double volume = volumes ? tlo.FenoSampleVolume / 100 : 1;

                List<string> samples = tlo.GetPlayingFilenames(mode);

                foreach (string filename in samples) {
                    bool isFilename = tlo.UsesFilename;

                    SampleSet sampleSet = isFilename ? tlo.FenoSampleSet : GetSamplesetFromFilename(filename);
                    Hitsound hitsound = isFilename ? tlo.GetHitsound() : GetHitsoundFromFilename(filename);

                    string samplePath = Path.Combine(mapDir, filename);
                    string fullPathExtLess = Path.Combine(
                        Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                        Path.GetFileNameWithoutExtension(samplePath));

                    // Get the first occurence of this sound to not get duplicated
                    if (firstSamples.Keys.Contains(fullPathExtLess)) {
                        samplePath = firstSamples[fullPathExtLess];
                    } else {
                        // Sample doesn't exist
                        if (!isFilename) {
                            samplePath = Path.Combine(
                                Path.GetDirectoryName(samplePath) ?? throw new InvalidOperationException(),
                                $"{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}-1.wav");
                        }
                    }
                    
                    string extLessFilename = Path.GetFileNameWithoutExtension(samplePath);
                    var importArgs = new LayerImportArgs(ImportType.Hitsounds) { Path = path, SamplePath = samplePath,
                        Volume = volume, DetectDuplicateSamples = detectDuplicateSamples, DiscriminateVolumes = volumes, RemoveDuplicates = removeDuplicates};

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.ImportArgs == importArgs);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(extLessFilename,
                            sampleSet,
                            hitsound,
                            new SampleGeneratingArgs(samplePath) {Volume = volume},
                            importArgs);
                        newLayer.Times.Add(tlo.Time);

                        hitsoundLayers.Add(newLayer);
                    }
                }
            }

            if (includeStoryboard) {
                hitsoundLayers.AddRange(ImportStoryboard(path, volumes, removeDuplicates, beatmap, mapDir, "SB: "));
            }

            // Sort layers by name
            hitsoundLayers = hitsoundLayers.OrderBy(o => o.Name).ToList();

            if (removeDuplicates) {
                foreach (var hitsoundLayer in hitsoundLayers) {
                    hitsoundLayer.Times.Sort();
                    hitsoundLayer.RemoveDuplicates();
                }
            }

            return hitsoundLayers;
        }

        private static List<HitsoundLayer> ImportStoryboard(string path, bool volumes, bool removeDuplicates, Beatmap beatmap, string mapDir, string prefix=null) {
            var hitsoundLayers = new List<HitsoundLayer>();
            prefix = prefix ?? string.Empty;

            foreach (var sbSample in beatmap.StoryboardSoundSamples) {
                var filepath = sbSample.FilePath;
                string samplePath = Path.Combine(mapDir, filepath);
                var filename = Path.GetFileNameWithoutExtension(filepath);

                var volume = volumes ? sbSample.Volume : 1;

                SampleSet sampleSet = GetSamplesetFromFilename(filename);
                Hitsound hitsound = GetHitsoundFromFilename(filename);

                var importArgs = new LayerImportArgs(ImportType.Storyboard)
                    {Path = path, SamplePath = samplePath, Volume = volume, DiscriminateVolumes = volumes, RemoveDuplicates = removeDuplicates};

                // Find the hitsoundlayer with this path
                HitsoundLayer layer = hitsoundLayers.Find(o => o.ImportArgs == importArgs);

                if (layer != null) {
                    // Find hitsound layer with this path and add this time
                    layer.Times.Add(sbSample.StartTime);
                } else {
                    // Add new hitsound layer with this path
                    HitsoundLayer newLayer = new HitsoundLayer(prefix + filename,
                        sampleSet,
                        hitsound,
                        new SampleGeneratingArgs(samplePath) {Volume = volume},
                        importArgs);

                    newLayer.Times.Add(sbSample.StartTime);

                    hitsoundLayers.Add(newLayer);
                }
            }

            if (removeDuplicates) {
                foreach (var hitsoundLayer in hitsoundLayers) {
                    hitsoundLayer.Times.Sort();
                    hitsoundLayer.RemoveDuplicates();
                }
            }

            return hitsoundLayers;
        }

        public static List<HitsoundLayer> ImportStoryboard(string path, bool volumes, bool removeDuplicates) {
            var editor = EditorReaderStuff.GetNewestVersionOrNot(path);
            Beatmap beatmap = editor.Beatmap;
            string mapDir = editor.GetParentFolder();

            var hitsoundLayers = ImportStoryboard(path, volumes, removeDuplicates, beatmap, mapDir);

            return hitsoundLayers;
        }

        public static SampleSet GetSamplesetFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 1)
                return SampleSet.Soft;
            string sampleset = split[0];
            switch (sampleset) {
                case "auto":
                    return SampleSet.Auto;
                case "normal":
                    return SampleSet.Normal;
                case "soft":
                    return SampleSet.Soft;
                case "drum":
                    return SampleSet.Drum;
                default:
                    return SampleSet.Soft;
            }
        }

        public static Hitsound GetHitsoundFromFilename(string filename) {
            string[] split = filename.Split('-');
            if (split.Length < 2)
                return Hitsound.Normal;
            string hitsound = split[1];
            if (hitsound.Contains("hitnormal"))
                return Hitsound.Normal;
            if (hitsound.Contains("hitwhistle"))
                return Hitsound.Whistle;
            if (hitsound.Contains("hitfinish"))
                return Hitsound.Finish;
            if (hitsound.Contains("hitclap"))
                return Hitsound.Clap;
            return Hitsound.Normal;
        }

        public static int GetIndexFromFilename(string filename) {
            var match = Regex.Match(filename, "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)");

            var remainder = filename.Substring(match.Index + match.Length);
            int index = 0;
            if (!string.IsNullOrEmpty(remainder)) {
                FileFormatHelper.TryParseInt(remainder, out index);
            }

            return index;
        }

        public static List<HitsoundLayer> ImportMidi(string path, double offset=0, bool instruments=true, bool keysounds=true, bool lengths=true, double lengthRoughness=1, bool velocities=true, double velocityRoughness=1) {
            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            var mf = new MidiFile(path, false);

            Console.WriteLine(
                $@"Format {mf.FileFormat}, " +
                $@"Tracks {mf.Tracks}, " +
                $@"Delta Ticks Per Quarter Note {mf.DeltaTicksPerQuarterNote}");

            List<TempoEvent> tempos = new List<TempoEvent>();
            foreach (var track in mf.Events) {
                tempos.AddRange(track.OfType<TempoEvent>());
            }
            tempos = tempos.OrderBy(o => o.AbsoluteTime).ToList();

            List<double> cumulativeTime = CalculateCumulativeTime(tempos, mf.DeltaTicksPerQuarterNote);

            Dictionary<int, int> channelBanks = new Dictionary<int, int>();
            Dictionary<int, int> channelPatches = new Dictionary<int, int>();

            // Loop through every event of every track
            for (int track = 0; track < mf.Tracks; track++) {
                foreach (var midiEvent in mf.Events[track]) {
                    if (midiEvent is PatchChangeEvent pc) {
                        channelPatches[pc.Channel] = pc.Patch;
                    }
                    else if (midiEvent is ControlChangeEvent co) {
                        if (co.Controller == MidiController.BankSelect) {
                            channelBanks[co.Channel] = (co.ControllerValue * 128) + (channelBanks.ContainsKey(co.Channel) ? (byte)channelBanks[co.Channel] : 0);
                        }
                        else if (co.Controller == MidiController.BankSelectLsb) {
                            channelBanks[co.Channel] = co.ControllerValue + (channelBanks.ContainsKey(co.Channel) ? (channelBanks[co.Channel] >> 8) * 128 : 0);
                        }
                    }
                    else if (MidiEvent.IsNoteOn(midiEvent)) {
                        var on = midiEvent as NoteOnEvent;

                        double time = CalculateTime(on.AbsoluteTime, tempos, cumulativeTime, mf.DeltaTicksPerQuarterNote);
                        double length = on.OffEvent != null
                            ? CalculateTime(on.OffEvent.AbsoluteTime,
                                  tempos,
                                  cumulativeTime,
                                  mf.DeltaTicksPerQuarterNote) -
                              time
                            : -1;
                        length = RoundLength(length, lengthRoughness);

                        bool keys = keysounds || on.Channel == 10;

                        int bank = instruments
                            ? on.Channel == 10 ? 128 :
                            channelBanks.ContainsKey(on.Channel) ? channelBanks[on.Channel] : 0
                            : -1;
                        int patch = instruments && channelPatches.ContainsKey(on.Channel)
                            ? channelPatches[on.Channel]
                            : -1;
                        int instrument = -1;
                        int key = keys ? on.NoteNumber : -1;
                        length = lengths ? length : -1;
                        int velocity = velocities ? on.Velocity : -1;
                        velocity = (int)RoundVelocity(velocity, velocityRoughness);

                        string lengthString = Math.Round(length).ToString(CultureInfo.InvariantCulture);

                        string instrumentName = on.Channel == 10 ? "Percussion" :
                            patch >= 0 && patch <= 127 ? PatchChangeEvent.GetPatchName(patch) : "Undefined";
                        string keyName = on.NoteName;

                        string name = instrumentName;
                        if (keysounds)
                            name += "," + keyName;
                        if (lengths)
                            name += "," + lengthString;
                        if (velocities)
                            name += "," + velocity;


                        var sampleArgs = new SampleGeneratingArgs(string.Empty, bank, patch, instrument, key, length, velocity);
                        var importArgs = new LayerImportArgs(ImportType.MIDI) {
                            Path = path,
                            Bank = bank,
                            Patch = patch,
                            Key = key,
                            Length = length,
                            LengthRoughness = lengthRoughness,
                            Velocity = velocity,
                            VelocityRoughness = velocityRoughness
                        };

                        // Find the hitsoundlayer with this path
                        HitsoundLayer layer = hitsoundLayers.Find(o => o.ImportArgs == importArgs);

                        if (layer != null) {
                            // Find hitsound layer with this path and add this time
                            layer.Times.Add(time + offset);
                        } else {
                            // Add new hitsound layer with this path
                            HitsoundLayer newLayer = new HitsoundLayer(name, SampleSet.Normal, Hitsound.Normal, sampleArgs, importArgs);
                            newLayer.Times.Add(time + offset);

                            hitsoundLayers.Add(newLayer);
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

        public static List<HitsoundLayer> ImportReloading(ImportReloadingArgs reloadingArgs) {
            switch (reloadingArgs.ImportType) {
                case ImportType.Stack:
                    return new List<HitsoundLayer>
                        {ImportStack(reloadingArgs.Path, reloadingArgs.X, reloadingArgs.Y)};
                case ImportType.Hitsounds:
                    return ImportHitsounds(reloadingArgs.Path, reloadingArgs.DiscriminateVolumes, reloadingArgs.DetectDuplicateSamples, reloadingArgs.RemoveDuplicates, false);
                case ImportType.Storyboard:
                    return ImportStoryboard(reloadingArgs.Path, reloadingArgs.DiscriminateVolumes, reloadingArgs.RemoveDuplicates);
                case ImportType.MIDI:
                    return ImportMidi(reloadingArgs.Path,
                        lengthRoughness: reloadingArgs.LengthRoughness,
                        velocityRoughness: reloadingArgs.VelocityRoughness);
                default:
                    return new List<HitsoundLayer>();
            }
        }

        private static double RoundVelocity(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var multi = length / roughness;
            var round = Math.Round(multi);
            return round * roughness;
        }

        private static double RoundLength(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var pow = Math.Pow(length, 1 / roughness);
            var round = Math.Ceiling(pow);
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
            if (prev == null) {
                return absoluteTime;
            }

            double deltaTime = prev.MicrosecondsPerQuarterNote / 1000d * (absoluteTime - prev.AbsoluteTime) / dtpq;
            return prevTime + deltaTime;
        }

        private static List<double> CalculateCumulativeTime(List<TempoEvent> tempos, int deltaTicksPerQuarter) {
            // Time is in miliseconds
            List<double> times = new List<double>(tempos.Count);

            TempoEvent last = null;
            foreach (TempoEvent te in tempos) {
                if (last == null) {
                    times.Add(te.AbsoluteTime);
                } else {
                    long deltaTicks = te.AbsoluteTime - last.AbsoluteTime;
                    double deltaTime = last.MicrosecondsPerQuarterNote / 1000d * deltaTicks / deltaTicksPerQuarter;

                    times.Add(times.Last() + deltaTime);
                }

                last = te;
            }
            return times;
        }
    }
}
