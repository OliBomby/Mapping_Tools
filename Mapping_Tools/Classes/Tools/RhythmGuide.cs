using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.SystemTools;

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

            private string[] paths = new string[0];
            private GameMode outputGameMode = GameMode.Standard;
            private string outputName = "Hitsounds";
            private bool ncEverything;
            private SelectionMode selectionMode = SelectionMode.HitsoundEvents;
            // ReSharper disable once CoVariantArrayConversion
            // ReSharper disable once RedundantArrayCreationExpression
            private IBeatDivisor[] beatDivisors = new RationalBeatDivisor[] {16, 12};

            private ExportMode exportMode = ExportMode.NewMap;
            private string exportPath = Path.Combine(SettingsManager.ExportPath, @"rhythm_guide.osu");

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
            return Core.Tools.RhythmGuide.RhythmGuideGenerator.CreateNewMap(
                beatmaps,
                ToCoreOptions(args));
        }

        private static void PopulateBeatmap(Beatmap beatmap, IEnumerable<Beatmap> beatmaps, RhythmGuideGeneratorArgs args) {
            Core.Tools.RhythmGuide.RhythmGuideGenerator.Append(
                beatmap,
                beatmaps,
                ToCoreOptions(args));
        }

        private static Core.Tools.RhythmGuide.RhythmGuideOptions ToCoreOptions(
            RhythmGuideGeneratorArgs args) {
            return new Core.Tools.RhythmGuide.RhythmGuideOptions {
                Paths = args.Paths,
                OutputGameMode = args.OutputGameMode,
                OutputName = args.OutputName,
                NcEverything = args.NcEverything,
                SelectionMode = (Core.Tools.RhythmGuide.RhythmGuideSelectionMode) args.SelectionMode,
                ExportMode = (Core.Tools.RhythmGuide.RhythmGuideExportMode) args.ExportMode,
                ExportPath = args.ExportPath,
                BeatDivisors = args.BeatDivisors
            };
        }
    }
}
