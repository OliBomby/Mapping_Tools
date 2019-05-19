using Mapping_Tools.Classes.BeatmapHelper;
using NAudio.Wave;
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
            string[] samplePaths = Directory.GetFiles(dir, "*.wav", SearchOption.TopDirectoryOnly);
            List<byte[]> audios = new List<byte[]>(samplePaths.Length);
            Dictionary<string, string> dict = new Dictionary<string, string>();

            // Read all samples
            foreach (string samplePath in samplePaths) {
                try {
                    WaveStream wave = new MediaFoundationReader(samplePath);
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
                        dict[samplePaths[i]] = samplePaths[k];
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

                    string filename = GetFileName(sampleSet, hitsound, index);
                    string samplePath = Path.Combine(mapDir, filename);

                    // Simplify path if it doesn't exist
                    if (firstSamples.Keys.Contains(samplePath)) {
                        samplePath = firstSamples[samplePath];
                        filename = Path.GetFileName(samplePath);
                    } else {
                        filename = GetFileName(sampleSet, hitsound, -1);
                        samplePath = Path.Combine(mapDir, filename);
                    }

                    // Find the hitsoundlayer with this path
                    HitsoundLayer layer = hitsoundLayers.Find(o => o.SamplePath == samplePath);

                    if (layer != null) {
                        // Find hitsound layer with this path and add this time
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer newLayer = new HitsoundLayer(filename, path, sample.Item1, sample.Item2, samplePath);
                        newLayer.Times.Add(tlo.Time);
                        hitsoundLayers.Add(newLayer);
                    }
                }
            }
            return hitsoundLayers;
        }

        public static string GetFileName(int sampleSet, int hitsound, int index) {
            if (index == 1) {
                return String.Format("{0}-hit{1}.wav", HitsoundConverter.SampleSets[sampleSet], HitsoundConverter.Hitsounds[hitsound]);
            }
            return String.Format("{0}-hit{1}{2}.wav", HitsoundConverter.SampleSets[sampleSet], HitsoundConverter.Hitsounds[hitsound], index);
        }
    }
}
