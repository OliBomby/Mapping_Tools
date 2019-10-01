using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;

namespace Mapping_Tools.Classes.Tools {
    public class RhythmGuide {
        public class RhythmGuideGeneratorArgs {
            public string[] Paths;
            public GameMode OutputGameMode;
            public string OutputName;
            public bool NcEverything;

            public ExportMode ExportMode;
            public string ExportPath;
        } 

        public enum ExportMode {
            NewMap,
            AddToMap,
        }

        public static void GenerateRhythmGuide(RhythmGuideGeneratorArgs args) {
            switch (args.ExportMode) {
                case ExportMode.NewMap:
                    var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);
                    var beatmap = MergeBeatmaps(args.Paths.Select(o => editorRead ? EditorReaderStuff.GetNewestVersion(o, reader) : new BeatmapEditor(o)).Select(o => o.Beatmap).ToArray(),
                        args.OutputGameMode, args.OutputName, args.NcEverything);

                    var editor = new Editor() {TextFile = beatmap, Path = args.ExportPath};
                    editor.SaveFile();
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

        private static void PopulateBeatmap(Beatmap beatmap, Beatmap[] beatmaps, bool ncEverything) {
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