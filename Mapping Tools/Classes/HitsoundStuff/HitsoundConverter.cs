using Mapping_Tools.Classes.BeatmapHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class HitsoundConverter {
        public static readonly List<string> SampleSets = new List<string> {"auto", "normal", "soft", "drum"};

        public static readonly List<string> Hitsounds = new List<string> { "normal", "whistle", "finish", "clap" };

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

            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();
            HashSet<string> paths = new HashSet<string>();

            foreach (TimelineObject tlo in timeline.TimeLineObjects) {
                List<Tuple<int, int, int>> samples = tlo.GetPlayingHitsounds();
                
                foreach (Tuple<int, int, int> sample in samples) {
                    int sampleSet = sample.Item1;
                    int hitsound = sample.Item2;
                    int index = sample.Item3;

                    string filename = GetFileName(sampleSet, hitsound, index);
                    string samplePath = Path.Combine(mapDir, filename);

                    // Simplify path if it doesn't exist
                    filename = File.Exists(samplePath) ? filename : GetFileName(sampleSet, hitsound, -1);
                    samplePath = Path.Combine(mapDir, filename);

                    if (paths.Contains(samplePath)) {
                        // Find hitsound layer with this path and add this time
                        HitsoundLayer layer = hitsoundLayers.Find(o => o.SamplePath == samplePath);
                        layer.Times.Add(tlo.Time);
                    } else {
                        // Add new hitsound layer with this path
                        HitsoundLayer layer = new HitsoundLayer(filename, path, sample.Item1, sample.Item2, samplePath);
                        layer.Times.Add(tlo.Time);
                        hitsoundLayers.Add(layer);
                        paths.Add(samplePath);
                    }
                }
            }
            return hitsoundLayers;
        }

        public static string GetFileName(int sampleSet, int hitsound, int index) {
            if (index == 1) {
                return String.Format("{0}-hit{1}.wav", SampleSets[sampleSet], Hitsounds[hitsound]);
            }
            return String.Format("{0}-hit{1}{2}.wav", SampleSets[sampleSet], Hitsounds[hitsound], index);
        }

        public static List<SamplePackage> ZipLayers(List<HitsoundLayer> layers, Sample defaultSample) {
            List<SamplePackage> packages = new List<SamplePackage>();
            foreach (HitsoundLayer hl in layers) {
                foreach (double t in hl.Times) {
                    SamplePackage packageOnTime = packages.Find(o => Math.Abs(o.Time - t) < 5);
                    if (packageOnTime != null) {
                        packageOnTime.Samples.Add(new Sample(hl));
                    } else {
                        packages.Add(new SamplePackage(t, new HashSet<Sample> { new Sample(hl) }));
                    }
                }
            }
            // Packages without a hitnormal sample
            foreach (SamplePackage p in packages.Where(o => !o.Samples.Any(s => s.Hitsound == 0))) {
                p.Samples.Add(defaultSample);
            }
            packages = packages.OrderBy(o => o.Time).ToList();
            return packages;
        }

        public static CompleteHitsounds ConvertPackages(List<SamplePackage> packages) {
            CompleteHitsounds ch = new CompleteHitsounds();

            foreach (SamplePackage p in packages) {
                // Check if package fits in any CustomIndex or if any CustomIndex can be modified to fit the package
                int index = -1;
                CustomIndex pci = p.GetCustomIndex();
                pci.CleanInvalids();

                // Check if the package fits in any customindex "out of the box"
                if (index == -1) {
                    foreach (CustomIndex ci in ch.CustomIndices) {
                        if (ci.CheckSupport(pci)) {
                            index = ci.Index;
                            break;
                        }
                    }
                }

                // Check if the package fits in any customindex after adding some samples
                if (index == -1) {
                    foreach (CustomIndex ci in ch.CustomIndices) {
                        if (ci.CheckCanSupport(pci)) {
                            ci.MergeWith(pci);
                            index = ci.Index;
                            break;
                        }
                    }
                }

                // If the package still didn't fit in any customindex, make a new customindex
                if (index == -1) {
                    CustomIndex ci = new CustomIndex(ch.CustomIndices.Count + 1);
                    ci.MergeWith(pci);
                    index = ci.Index;
                    ch.CustomIndices.Add(ci);
                }
            }

            // Loop again to add hitsounds after adding customindices so greenline usage can be optimized
            int lastIndex = -1;
            foreach (SamplePackage p in packages) {
                int sampleSet = p.GetSampleSet();
                int additions = p.GetAdditions();

                bool whistle = p.Samples.Any(o => o.Hitsound == 1);
                bool finish = p.Samples.Any(o => o.Hitsound == 2);
                bool clap = p.Samples.Any(o => o.Hitsound == 3);

                // Check if package fits in any CustomIndex or if any CustomIndex can be modified to fit the package
                int index = -1;
                CustomIndex pci = p.GetCustomIndex();
                pci.CleanInvalids();

                // Check if the package fits in the previous package's customindex first to reduce greenline usage
                if (lastIndex != -1) {
                    CustomIndex lastCustomIndex = ch.CustomIndices.Find(o => o.Index == lastIndex);
                    if (lastCustomIndex.CheckSupport(pci)) {
                        index = lastCustomIndex.Index;
                    }
                }
                
                // Check if the package fits in any customindex "out of the box"
                if (index == -1) {
                    foreach (CustomIndex ci in ch.CustomIndices) {
                        if (ci.CheckSupport(pci)) {
                            index = ci.Index;
                            break;
                        }
                    }
                }
                
                // Check if the package fits in any customindex after adding some samples
                if (index == -1) {
                    foreach (CustomIndex ci in ch.CustomIndices) {
                        if (ci.CheckCanSupport(pci)) {
                            ci.MergeWith(pci);
                            index = ci.Index;
                            break;
                        }
                    }
                }

                // If the package still didn't fit in any customindex, make a new customindex
                if (index == -1) {
                    CustomIndex ci = new CustomIndex(ch.CustomIndices.Count + 1);
                    ci.MergeWith(pci);
                    index = ci.Index;
                    ch.CustomIndices.Add(ci);
                }

                ch.Hitsounds.Add(new Hitsound(p.Time, sampleSet, additions, index, whistle, finish, clap));
                lastIndex = index;
            }
            return ch;
        }
    }
}
