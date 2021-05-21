using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Editor_Reader;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Exceptions;
using Mapping_Tools.Classes.SystemTools;
using Process.NET;
using Process.NET.Memory;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Classes.ToolHelpers {

    public static class EditorReaderStuff
    {
        private static readonly EditorReader editorReader = new EditorReader();
        public static string DontCoolSaveWhenMD5EqualsThisString = "";

        /// <summary>
        /// Don't use this unless you know what you're doing
        /// </summary>
        /// <returns></returns>
        public static EditorReader GetEditorReader()
        {
            return editorReader;
        }

        /// <summary>
        /// Determines whether the editor is open in the osu! client by checking if the window title ends with ".osu".
        /// </summary>
        /// <returns></returns>
        public static bool IsEditorOpen() {
            var process = System.Diagnostics.Process.GetProcessesByName("osu!").FirstOrDefault();
            var processSharp = new ProcessSharp(process, MemoryType.Remote);
            var osuWindow = processSharp.WindowFactory.MainWindow;
            return osuWindow.Title.EndsWith(@".osu");
        }

        /// <summary>
        /// Gets the instance of EditorReader with FetchAll. Throws an exception if the editor is not open or anything is wrong.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="EditorReaderDisabledException"></exception>
        /// <exception cref="InvalidEditorReaderStateException"></exception>
        public static EditorReader GetFullEditorReader() {
            if (!SettingsManager.Settings.UseEditorReader) {
                throw new EditorReaderDisabledException();
            }

            if (!IsEditorOpen()) {
                throw new Exception("No active editor detected.");
            }

            /*editorReader.FetchEditor();
            editorReader.SetHOM();
            editorReader.ReadHOM();
            editorReader.FetchBeatmap();
            editorReader.FetchControlPoints();
            editorReader.SetObjects();
            Console.WriteLine(editorReader.numObjects);
            editorReader.ReadObjects();
            editorReader.FetchBookmarks();*/

            editorReader.FetchAll();

            var removed = FixFullReader(editorReader);
            if (removed > 1) {
                LogEditorReader(editorReader);
                throw new InvalidEditorReaderStateException();
            }

            var valid = ValidateFullReader(editorReader);
            if (!valid) {
                LogEditorReader(editorReader);
                throw new InvalidEditorReaderStateException();
            }

            return editorReader;
        }

        public static EditorReader GetFullEditorReaderOrNot() {
            return GetFullEditorReaderOrNot(out _);
        }

        public static EditorReader GetFullEditorReaderOrNot(out Exception exception) {
            exception = null;
            try {
                return GetFullEditorReader();
            } catch (Exception ex) {
                exception = ex;
                return null;
            }
        }

        /// <summary>
        /// Removes all invalid hit objects from the reader object
        /// </summary>
        /// <param name="reader">The fully fetched editor reader</param>
        private static int FixFullReader(EditorReader reader)
        {
            return reader.hitObjects.RemoveAll(readerHitObject =>
                readerHitObject.SegmentCount > 9000 || readerHitObject.Type == 0 || readerHitObject.SampleSet > 1000 ||
                readerHitObject.SampleSetAdditions > 1000 || readerHitObject.SampleVolume > 1000);
        }

        /// <summary>
        /// Checks for any insane values in the reader which indicate the editor has been incorrectly read
        /// </summary>
        /// <param name="reader">The fully fetched editor reader</param>
        /// <returns>A boolean whether the reader is valid</returns>
        private static bool ValidateFullReader(EditorReader reader)
        {
            bool result = !reader.hitObjects.Any(readerHitObject => readerHitObject.SegmentCount > 9000 || 
                                                                    readerHitObject.Type == 0 || 
                                                                    readerHitObject.SampleSet > 1000 || 
                                                                    readerHitObject.SampleSetAdditions > 1000 || 
                                                                    readerHitObject.SampleVolume > 1000)
                && reader.numControlPoints > 0 && 
                reader.controlPoints != null && reader.hitObjects != null && 
                reader.numControlPoints == reader.controlPoints.Count && reader.numObjects == reader.hitObjects.Count;
            
            if (!result)
            {
                // Save error log
                LogEditorReader(reader);
                //MessageBox.Show("A problem has been encountered with editor reader. An error log has been saved to editor_reader_error.txt", "Warning");
            }

            return result;
        }

        private static void LogEditorReader(EditorReader reader) {
            var path = Path.Combine(MainWindow.AppDataPath, "editor_reader_error.txt");

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }

            var lines = new List<string> {
                @"ContainingFolder: " + reader.ContainingFolder,
                @"Filename: " + reader.Filename,
                @"ApproachRate: " + reader.ApproachRate,
                @"CircleSize: " + reader.CircleSize,
                @"HPDrainRate: " + reader.HPDrainRate,
                @"OverallDifficulty: " + reader.OverallDifficulty,
                @"PreviewTime: " + reader.PreviewTime,
                @"SliderMultiplier: " + reader.SliderMultiplier,
                @"SliderTickRate: " + reader.SliderTickRate,
                @"StackLeniency: " + reader.StackLeniency,
                @"TimelineZoom: " + reader.TimelineZoom,
                @"numBookmarks: " + reader.numBookmarks,
                @"numClipboard: " + reader.numClipboard,
                @"numControlPoints: " + reader.numControlPoints,
                @"numObjects: " + reader.numObjects,
                @"numSelected: " + reader.numSelected,
                @"EditorTime: " + reader.EditorTime(),
                @"ProcessTitle: " + reader.ProcessTitle(),
                @"[HitObjects]",
            };
            lines.AddRange(reader.hitObjects.Select(readerHitObject => readerHitObject.ToString()));
            lines.Add(@"[TimingPoints]");
            lines.AddRange(reader.controlPoints.Select(readerControlPoint => readerControlPoint.ToString()));

            File.WriteAllLines(path, lines);
        }

        public static int GetEditorTime() {
            var reader = GetEditorReader();
            if (reader.ProcessNeedsReload() || reader.EditorNeedsReload())
                reader.FetchEditor();
            return reader.EditorTime();
        }

        /// <summary>
        /// Saves current beatmap with the newest version from memory and rounded coordinates
        /// </summary>
        /// <exception cref="EditorReaderDisabledException"></exception>
        /// <exception cref="InvalidEditorReaderStateException"></exception>
        public static void BetterSave() {
            var reader = GetFullEditorReader();

            var path = GetCurrentBeatmap(reader);
            var editor = GetNewestVersion(path, reader);

            BackupManager.SaveMapBackup(path);
            editor.SaveFile();

            Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully saved current beatmap!"));
        }


        /// <summary>
        /// Computes MD5 hash of file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetMD5FromPath(string path)
        {
            using (var md5 = MD5.Create())
            {
                using (var fileStream = File.OpenRead(path))
                {
                    var hash = md5.ComputeHash(fileStream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Tries to get the newest version if a valid reader is provided. Otherwise returns default save.
        /// Use this if you don't care about it failing.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fullReader"></param>
        /// <returns></returns>
        public static BeatmapEditor GetNewestVersionOrNot(string path, EditorReader fullReader) {
            return GetNewestVersionOrNot(path, fullReader, out _, out _);
        }

        /// <summary>
        /// Tries to get the newest version if a valid reader is provided. Otherwise returns default save.
        /// Use this if you don't care about it failing.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fullReader"></param>
        /// <param name="selected"></param>
        /// <param name="exception">Any exception that may occur, null otherwise</param>
        /// <returns></returns>
        public static BeatmapEditor GetNewestVersionOrNot(string path, EditorReader fullReader, out List<HitObject> selected, out Exception exception) {
            exception = null;

            if (fullReader != null) {
                try {
                    return GetNewestVersion(path, fullReader, out selected);
                } catch (Exception ex) {
                    exception = ex;
                }
            }
            
            selected = new List<HitObject>();
            return new BeatmapEditor(path);
        }

        /// <summary>
        /// Tries to get the newest version. Otherwise returns default save.
        /// Use this if you don't care about it failing.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static BeatmapEditor GetNewestVersionOrNot(string path) {
            return GetNewestVersionOrNot(path, out _, out _);
        }

        /// <summary>
        /// Tries to get the newest version. Otherwise returns default save.
        /// Use this if you don't care about it failing.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="selected"></param>
        /// <param name="exception">Any exception that may occur, null otherwise</param>
        /// <returns></returns>
        public static BeatmapEditor GetNewestVersionOrNot(string path, out List<HitObject> selected, out Exception exception) {
            exception = null;

            try {
                var fullReader = GetFullEditorReader();
                return GetNewestVersion(path, fullReader, out selected);
            } catch (Exception ex) {
                exception = ex;
            }
            
            selected = new List<HitObject>();
            return new BeatmapEditor(path);
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>The editor with the newest version</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static BeatmapEditor GetNewestVersion(string path, EditorReader fullReader)
        {
            return GetNewestVersion(path, fullReader, out _);
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="selected">List of selected hit objects</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>The editor with the newest version</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static BeatmapEditor GetNewestVersion(string path, EditorReader fullReader, out List<HitObject> selected) {
            if (fullReader == null) {
                throw new ArgumentNullException(nameof(fullReader));
            }

            BeatmapEditor editor = new BeatmapEditor(path);
            selected = new List<HitObject>();
            
            // Get the path from the beatmap in memory
            string memoryPath = GetCurrentBeatmap(fullReader);

            // Check whether the beatmap in the editor is the same as the beatmap you want
            if (memoryPath == path) {
                // Update the beatmap with memory values
                selected = UpdateBeatmap(editor.Beatmap, fullReader);
            }

            return editor;
        }

        /// <summary>
        /// Returns an editor for the beatmap which is currently open in the editor. Returns null if there is no beatmap open in the editor.
        /// </summary>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <param name="selected">List of selected hit objects</param>
        /// <returns>An editor for the beatmap</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static BeatmapEditor GetBeatmapEditor(EditorReader fullReader, out List<HitObject> selected) {
            if (fullReader == null) {
                throw new ArgumentNullException(nameof(fullReader));
            }

            // Get the path from the beatmap in memory
            string memoryPath = GetCurrentBeatmap(fullReader);

            // Update the beatmap with memory values
            var editor = new BeatmapEditor(memoryPath);
            selected = UpdateBeatmap(editor.Beatmap, fullReader);

            return editor;
        }

        /// <summary>
        /// Gets the path to the beatmap currently open in the <see cref="EditorReader"/> instance.
        /// </summary>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns></returns>
        public static string GetCurrentBeatmap(EditorReader fullReader) {
            string songs = SettingsManager.GetSongsPath();
            string folder = fullReader.ContainingFolder;
            string filename = fullReader.Filename;
            return Path.Combine(songs, folder, filename);
        }

        /// <summary>
        /// Replaces hit objects and timing points with the values in the editor reader
        /// </summary>
        /// <param name="beatmap">Beatmap to replace values in</param>
        /// <param name="reader">Reader that contains the values from memory</param>
        /// <returns>A list of selected hit objects which originate from the beatmap.</returns>
        public static List<HitObject> UpdateBeatmap(Beatmap beatmap, EditorReader reader)
        {
            beatmap.SetBookmarks(reader.bookmarks.Select<int, double>(o => o).ToList());

            beatmap.BeatmapTiming.SetTimingPoints(reader.controlPoints.Select(o => (TimingPoint)o).ToList());

            List<HitObject> selected = new List<HitObject>();
            beatmap.HitObjects = reader.hitObjects.Select(o => {
                var nho = (HitObject) o;
                if (o.IsSelected) selected.Add(nho); 
                return nho;
            }).ToList();

            beatmap.General["PreviewTime"] = new TValue(reader.PreviewTime.ToString(CultureInfo.InvariantCulture));
            beatmap.Difficulty["SliderMultiplier"] = new TValue(reader.SliderMultiplier.ToString(CultureInfo.InvariantCulture));
            beatmap.Difficulty["SliderTickRate"] = new TValue(reader.SliderTickRate.ToString(CultureInfo.InvariantCulture));

            // Update all the other stuff based on these values
            beatmap.BeatmapTiming.SliderMultiplier = reader.SliderMultiplier;

            // Sort the stuff
            beatmap.HitObjects = beatmap.HitObjects.OrderBy(o => o.Time).ToList();

            beatmap.CalculateHitObjectComboStuff();
            beatmap.CalculateSliderEndTimes();
            beatmap.GiveObjectsGreenlines();

            return selected;
        }

        /// <summary>
        /// Gets the hit objects out of an editor reader and converts them to better type
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<HitObject> GetHitObjects(EditorReader reader) {
            return reader.hitObjects.Select(o => (HitObject)o).ToList();
        }
    }
}