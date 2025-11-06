using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.Tools;

/// <summary>
/// 
/// </summary>
public class RhythmGuide {

    /// <summary>
    /// 
    /// </summary>
    public class RhythmGuideGeneratorArgs : BindableBase {

        #region private_members

        private string[] paths = new string[0];
        private GameMode outputGameMode = GameMode.Standard;
        private string outputName = "Hitsounds";
        private bool ncEverything;
        private SelectionMode selectionMode = SelectionMode.HitsoundEvents;
        // ReSharper disable once CoVariantArrayConversion
        // ReSharper disable once RedundantArrayCreationExpression
        private IBeatDivisor[] beatDivisors = new RationalBeatDivisor[] {16, 12};

        private ExportMode exportMode = ExportMode.NewMap;
        private string exportPath = Path.Combine(MainWindow.ExportPath, @"rhythm_guide.osu");

        #endregion

        /// <summary>
        /// A string of paths to import from.
        /// </summary>
        public string[] Paths {
            get => paths;
            set => Set(ref paths, value);
        }

        /// <summary>
        /// The Selected output game mode
        /// </summary>
        public GameMode OutputGameMode {
            get => outputGameMode;
            set => Set(ref outputGameMode, value);
        }

        /// <summary>
        /// The difficulty name of the output
        /// </summary>
        public string OutputName {
            get => outputName;
            set => Set(ref outputName, value);
        }

        /// <summary>
        /// If each object should have a new combo.
        /// </summary>
        public bool NcEverything {
            get => ncEverything;
            set => Set(ref ncEverything, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public SelectionMode SelectionMode {
            get => selectionMode;
            set => Set(ref selectionMode, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public ExportMode ExportMode {
            get => exportMode;
            set => Set(ref exportMode, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string ExportPath {
            get => exportPath;
            set => Set(ref exportPath, value);
        }

        public IBeatDivisor[] BeatDivisors {
            get => beatDivisors;
            set => Set(ref beatDivisors, value);
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

    public enum SelectionMode {
        AllEvents,
        HitsoundEvents,
        AllEventSeparated,
        LongNotes
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args"></param>
    public static void GenerateRhythmGuide(RhythmGuideGeneratorArgs args) {
        if (args.ExportPath == null) {
            throw new ArgumentNullException(nameof(args.ExportPath));
        }

        var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

        switch (args.ExportMode) {
            case ExportMode.NewMap:
                var beatmap = MergeBeatmaps(args.Paths.Select(o => EditorReaderStuff.GetNewestVersionOrNot(o, reader).Beatmap).ToArray(),
                    args);

                var editor = new Editor {TextFile = beatmap, Path = args.ExportPath};
                editor.SaveFile();
                System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(args.ExportPath) ??
                                                                 throw new ArgumentException("Export path must be a file."));
                break;
            case ExportMode.AddToMap:
                var editor2 = EditorReaderStuff.GetNewestVersionOrNot(args.ExportPath, reader);

                PopulateBeatmap(editor2.Beatmap,
                    args.Paths.Select(o => EditorReaderStuff.GetNewestVersionOrNot(o, reader).Beatmap),
                    args);

                editor2.SaveFile();
                break;
            default:
                return;
        }
    }

    private static Beatmap MergeBeatmaps(Beatmap[] beatmaps, RhythmGuideGeneratorArgs args) {
        if (beatmaps.Length == 0) {
            throw new ArgumentException("There must be at least one beatmap.");
        }

        // Scuffed beatmap copy
        var newBeatmap = new Beatmap(beatmaps[0].GetLines());

        // Remove all greenlines
        newBeatmap.BeatmapTiming.RemoveAll(o => !o.Uninherited);

        // Remove all hitobjects
        newBeatmap.HitObjects.Clear();

        // Change some parameters;
        newBeatmap.General["StackLeniency"] = new TValue("0.0");
        newBeatmap.General["Mode"] = new TValue(((int)args.OutputGameMode).ToString());
        newBeatmap.Metadata["Version"] = new TValue(args.OutputName);
        newBeatmap.Difficulty["CircleSize"] = new TValue("4");

        // Add hitobjects
        PopulateBeatmap(newBeatmap, beatmaps, args);

        return newBeatmap;
    }

    private static void PopulateBeatmap(Beatmap beatmap, IEnumerable<Beatmap> beatmaps, RhythmGuideGeneratorArgs args) {
        foreach (var b in beatmaps) {
            var timeline = b.GetTimeline();
            foreach (var timelineObject in timeline.TimelineObjects) {
                // Handle different selection modes
                switch (args.SelectionMode) {
                    case SelectionMode.AllEvents:
                        addHitObject(timelineObject.Time);
                        break;
                    case SelectionMode.HitsoundEvents:
                        if (timelineObject.HasHitsound) {
                            addHitObject(timelineObject.Time);
                        }

                        break;
                    case SelectionMode.AllEventSeparated:
                        var active = timelineObject.IsHoldnoteHead || timelineObject.IsCircle || timelineObject.IsSliderHead;
                        var pos = active ? new Vector2(0, 192) : new Vector2(512, 192);

                        addHitObject(timelineObject.Time, pos);
                        break;
                    case SelectionMode.LongNotes:
                        var isStart = timelineObject.IsHoldnoteHead || timelineObject.IsCircle ||
                                      timelineObject.IsSliderHead || timelineObject.IsSpinnerHead;

                        if (isStart) {
                            addHitObject(timelineObject.Time);
                        } else if (beatmap.HitObjects.Count > 0) {
                            // Extend last object
                            var last = beatmap.HitObjects[^1];
                            last.IsCircle = false;
                            last.IsHoldNote = true;
                            last.EndTime = timelineObject.Time;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        void addHitObject(double time, Vector2? pos = null) {
            var t = beatmap.BeatmapTiming.Resnap(time, args.BeatDivisors);
            var ho = new HitObject(time, 0, SampleSet.None, SampleSet.None) { NewCombo = args.NcEverything };
            if (pos.HasValue)
                ho.Pos = pos.Value;

            beatmap.HitObjects.Add(ho);
        }
    }
}