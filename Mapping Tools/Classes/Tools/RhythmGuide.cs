using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools {

    /// <summary>
    /// 
    /// </summary>
    public class RhythmGuide {

        /// <summary>
        /// 
        /// </summary>
        public class RhythmGuideGeneratorArgs : BindableBase {

            #region private_members

            private string[] _paths = new string[0];
            private GameMode _outputGameMode = GameMode.Standard;
            private string _outputName = "Hitsounds";
            private bool _ncEverything;

            private ExportMode _exportMode = ExportMode.NewMap;
            private string _exportPath = Path.Combine(MainWindow.ExportPath, @"rhythm_guide.osu");

            #endregion

            /// <summary>
            /// A string of paths to import from.
            /// </summary>
            public string[] Paths {
                get => _paths;
                set => Set(ref _paths, value);
            }

            /// <summary>
            /// The Selected output game mode
            /// </summary>
            public GameMode OutputGameMode {
                get => _outputGameMode;
                set => Set(ref _outputGameMode, value);
            }

            /// <summary>
            /// The difficulty name of the output
            /// </summary>
            public string OutputName {
                get => _outputName;
                set => Set(ref _outputName, value);
            }

            /// <summary>
            /// If each object should have a new combo.
            /// </summary>
            public bool NcEverything {
                get => _ncEverything;
                set => Set(ref _ncEverything, value);
            }

            /// <summary>
            /// 
            /// </summary>
            public ExportMode ExportMode {
                get => _exportMode;
                set => Set(ref _exportMode, value);
            }

            /// <summary>
            /// 
            /// </summary>
            public string ExportPath {
                get => _exportPath;
                set => Set(ref _exportPath, value);
            }


            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return $@"{Paths}, {ExportPath}, {ExportMode}, {OutputGameMode}, {OutputName}, {NcEverything}";
            }
        } 

        /// <summary>
        /// 
        /// </summary>
        public enum ExportMode {
            /// <summary>
            /// 
            /// </summary>
            NewMap,

            /// <summary>
            /// 
            /// </summary>
            AddToMap,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void GenerateRhythmGuide(RhythmGuideGeneratorArgs args) {
            if (args.ExportPath == null) {
                throw new ArgumentException("Export path can not be null.");
            }
            switch (args.ExportMode) {
                case ExportMode.NewMap:
                    var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);
                    var beatmap = MergeBeatmaps(args.Paths.Select(o => editorRead ? EditorReaderStuff.GetNewestVersion(o, reader) : new BeatmapEditor(o)).Select(o => o.Beatmap).ToArray(),
                        args.OutputGameMode, args.OutputName, args.NcEverything);

                    var editor = new Editor() {TextFile = beatmap, Path = args.ExportPath};
                    editor.SaveFile();
                    System.Diagnostics.Process.Start(Path.GetDirectoryName(args.ExportPath) ??
                                                     throw new ArgumentException("Export path must be a file."));
                    break;
                case ExportMode.AddToMap:
                    var editor2 = EditorReaderStuff.GetNewestVersion(args.ExportPath);
                    PopulateBeatmap(editor2.Beatmap,
                        args.Paths.Select(o => new BeatmapEditor(o)).Select(o => o.Beatmap).ToArray(),
                        args.NcEverything);

                    editor2.SaveFile();
                    break;
                default:
                    return;
            }
        }

        private static Beatmap MergeBeatmaps(Beatmap[] beatmaps, GameMode outputGameMode, string outputName, bool ncEverything) {
            if (beatmaps.Length == 0) {
                throw new ArgumentException("There must be at least one beatmap.");
            }

            // Scuffed beatmap copy
            var newBeatmap = new Beatmap(beatmaps[0].GetLines());

            // Remove all greenlines
            newBeatmap.BeatmapTiming.TimingPoints.RemoveAll(o => !o.Inherited);

            // Remove all hitobjects
            newBeatmap.HitObjects.Clear();

            // Change some parameters;
            newBeatmap.General["StackLeniency"] = new TValue("0.0");
            newBeatmap.General["Mode"] = new TValue(((int)outputGameMode).ToString());
            newBeatmap.Metadata["Version"] = new TValue(outputName);
            newBeatmap.Difficulty["CircleSize"] = new TValue("4");

            // Add hitobjects
            PopulateBeatmap(newBeatmap, beatmaps, ncEverything);

            return newBeatmap;
        }

        private static void PopulateBeatmap(Beatmap beatmap, IEnumerable<Beatmap> beatmaps, bool ncEverything) {
            // Get the times from all beatmaps
            var times = new HashSet<double>();
            foreach (var b in beatmaps) {
                foreach (var hitObject in b.HitObjects) {
                    // Add all repeats
                    for (var i = 0; i <= hitObject.Repeat; i++) {
                        var time = hitObject.Time + hitObject.TemporalLength * i;
                        times.Add(b.BeatmapTiming.Resnap(time, 16, 12));
                    }
                }
            }

            // Generate hitcircles at those times
            foreach (var ho in times.Select(time => new HitObject(time, 0, SampleSet.Auto, SampleSet.Auto))) {
                ho.NewCombo = ncEverything;
                beatmap.HitObjects.Add(ho);
            }
        }
    }
}