using Editor_Reader;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Classes.Tools
{

    public abstract class EditorReaderStuff
    {
        private static readonly EditorReader editorReader = new EditorReader();
        public static string DontCoolSaveWhenMD5EqualsThisString = "";
        public static readonly string EditorReaderIsDisabledText = "You need to enable Editor Reader to use this feature.";

        /// <summary>
        /// Don't use this unless you know what you're doing
        /// </summary>
        /// <returns></returns>
        public static EditorReader GetEditorReader()
        {
            return editorReader;
        }

        /// <summary>
        /// Gets the instance of EditorReader with FetchAll. Throws an exception if the editor is not open.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetFullEditorReader(out EditorReader reader)
        {
            reader = editorReader;

            if (!SettingsManager.Settings.UseEditorReader) return false;

            try
            {
                editorReader.FetchAll();
                FixFullReader(editorReader);
                return ValidateFullReader(editorReader);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes all invalid hit objects from the reader object
        /// </summary>
        /// <param name="reader">The fully fetched editor reader</param>
        private static void FixFullReader(EditorReader reader)
        {
            reader.hitObjects.RemoveAll(readerHitObject =>
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
            bool result = true;

            foreach (var readerHitObject in reader.hitObjects)
            {
                if (readerHitObject.SegmentCount > 9000 || readerHitObject.Type == 0 || readerHitObject.SampleSet > 1000 || readerHitObject.SampleSetAdditions > 1000 || readerHitObject.SampleVolume > 1000)
                {
                    result = false;
                }
            }

            if (!result)
            {
                // Save error log
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

                MessageBox.Show("A problem has been encountered with editor reader. An error log has been saved to editor_reader_error.txt", "Warning");
            }

            return result;
        }

        /// <summary>
        /// Saves current beatmap with the newest version from memory and rounded coordinates
        /// </summary>
        public static void CoolSave()
        {
            try
            {
                if (!SettingsManager.Settings.UseEditorReader)
                {
                    MessageBox.Show(EditorReaderIsDisabledText);
                    return;
                }
                var editor = GetNewestVersion(IOHelper.GetCurrentBeatmap());
                editor.SaveFile();
                MainWindow.Snackbar.MessageQueue.Enqueue("Succesfully saved current beatmap!");
            }
            catch (Exception e)
            {
                MessageBox.Show($"BetterSave™ wasn't better after all\n{e.Message}");
            }
        }

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

        public static List<HitObject> GetSelectedObjects(BeatmapEditor editor, EditorReader reader)
        {
            try
            {
                string songs = SettingsManager.GetSongsPath();
                string folder = reader.ContainingFolder;
                string filename = reader.Filename;
                string memoryPath = Path.Combine(songs, folder, filename);

                // Check whether the beatmap in the editor is the same as the beatmap you want
                if (memoryPath != editor.Path)
                    return new List<HitObject>();

                reader.FetchSelected();
                var convertedSelected = reader.selectedObjects.Select(o => (HitObject)o).ToList();
                var selectedHitObjects = new List<HitObject>(convertedSelected.Count());
                var comparer = new HitObjectComparer();

                // Get all the hit objects that are selected according to the editor reader
                foreach (var ho in editor.Beatmap.HitObjects)
                {
                    if (convertedSelected.Contains(ho, comparer))
                    {
                        selectedHitObjects.Add(ho);
                    }
                }
                return selectedHitObjects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception ({ex.Message}) while editor reading.");
                return new List<HitObject>();
            }
        }

        public static BeatmapEditor GetNewestVersion(EditorReader reader, out List<HitObject> selected)
        {
            // Get the path from the beatmap in memory
            string songs = SettingsManager.GetSongsPath();
            string folder = reader.ContainingFolder;
            string filename = reader.Filename;
            string memoryPath = Path.Combine(songs, folder, filename);

            var editor = new BeatmapEditor(memoryPath);

            // Update the beatmap with memory values
            selected = SettingsManager.Settings.UseEditorReader ? UpdateBeatmap(editor.Beatmap, reader) : new List<HitObject>();

            return editor;
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>An editor for the beatmap</returns>
        public static BeatmapEditor GetNewestVersion(string path, EditorReader fullReader = null)
        {
            return GetNewestVersion(path, out _, fullReader);
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="selected">List of selected hit objects</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>An editor for the beatmap</returns>
        public static BeatmapEditor GetNewestVersion(string path, out List<HitObject> selected, EditorReader fullReader = null)
        {
            BeatmapEditor editor = new BeatmapEditor(path);
            selected = new List<HitObject>();

            // Check if Editor Reader is enabled
            if (!SettingsManager.Settings.UseEditorReader)
            {
                return editor;
            }

            // Get a reader object that has everything fetched
            var reader = fullReader;
            if (reader == null)
                if (!TryGetFullEditorReader(out reader))
                    return editor;

            // Get the path from the beatmap in memory
            // This can only crash if the provided fullReader didn't fetch all values
            try
            {
                string songs = SettingsManager.GetSongsPath();
                string folder = reader.ContainingFolder;
                string filename = reader.Filename;
                string memoryPath = Path.Combine(songs, folder, filename);

                // Check whether the beatmap in the editor is the same as the beatmap you want
                if (memoryPath != path)
                    return editor;

                // Update the beatmap with memory values
                selected = UpdateBeatmap(editor.Beatmap, reader);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception ({ex.Message}) while editor reading.");
            }

            return editor;
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

            beatmap.BeatmapTiming.TimingPoints = reader.controlPoints.Select(o => (TimingPoint)o).ToList();

            List<HitObject> selected = new List<HitObject>();
            beatmap.HitObjects = reader.hitObjects.Select(o => { var nho = (HitObject)o; if (o.IsSelected) selected.Add(nho); return nho; }).ToList();

            beatmap.General["PreviewTime"] = new TValue(reader.PreviewTime.ToString(CultureInfo.InvariantCulture));
            beatmap.Difficulty["SliderMultiplier"] = new TValue(reader.SliderMultiplier.ToString(CultureInfo.InvariantCulture));
            beatmap.Difficulty["SliderTickRate"] = new TValue(reader.SliderTickRate.ToString(CultureInfo.InvariantCulture));

            // Update all the other stuff based on these values
            beatmap.BeatmapTiming.SliderMultiplier = reader.SliderMultiplier;

            // Sort the stuff
            beatmap.HitObjects = beatmap.HitObjects.OrderBy(o => o.Time).ToList();
            beatmap.BeatmapTiming.Sort();

            beatmap.CalculateSliderEndTimes();
            beatmap.GiveObjectsGreenlines();

            return selected;
        }

        /// <summary>
        /// Gets the hit objects out of an editor reader and converts them to better type
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static List<HitObject> GetHitObjects(EditorReader reader)
        {
            return reader.hitObjects.Select(o => (HitObject)o).ToList();
        }
    }
}