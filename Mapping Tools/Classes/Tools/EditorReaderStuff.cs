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
using static System.Net.Mime.MediaTypeNames;

namespace Mapping_Tools.Classes.Tools
{
    public static class EditorReaderStuff
    {
        private static string UpdatedFileHash;
        private readonly static EditorReader editorReader = new EditorReader();
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
            try
            {
                editorReader.FetchAll();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves current beatmap with the newest version from memory and rounded coordinates
        /// </summary>
        public static void CoolSave()
        {
            string hashString = "";
            var currentPath = IOHelper.CurrentBeatmap();
            try
            {
                if (File.Exists(currentPath))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var fileStream = File.OpenRead(currentPath))
                        {
                            var hash = md5.ComputeHash(fileStream);
                            hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
            }
            catch
            {
                return;
            }

            try
            {
                var editor = GetNewestVersion(currentPath);
                var data = editor.Beatmap.GetLines();
                byte[] byteData = data.SelectMany(s => System.Text.Encoding.ASCII.GetBytes(s)).ToArray();
                string memoryHash = "";
                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(byteData);
                    memoryHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
                if (memoryHash == hashString)
                {
                    return;
                }
                editor.SaveFile();
                Console.WriteLine(memoryHash);
            }
            catch (Exception e)
            {
                MessageBox.Show($"BetterSave™ wasn't better after all\n{e.Message}");
            }
        }

        public static List<BeatmapHelper.HitObject> GetSelectedObjects(BeatmapEditor editor, EditorReader reader)
        {
            try
            {
                string songs = SettingsManager.GetSongsPath();
                string folder = reader.ContainingFolder;
                string filename = reader.Filename;
                string memoryPath = Path.Combine(songs, folder, filename);

                // Check whether the beatmap in the editor is the same as the beatmap you want
                if (memoryPath != editor.Path)
                    return new List<BeatmapHelper.HitObject>();

                reader.FetchSelected();
                var convertedSelected = reader.selectedObjects.Select(o => (BeatmapHelper.HitObject)o).ToList();
                var selectedHitObjects = new List<BeatmapHelper.HitObject>(convertedSelected.Count());
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
                return new List<BeatmapHelper.HitObject>();
            }
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>An editor for the beatmap</returns>
        public static BeatmapEditor GetNewestVersion(string path, EditorReader fullReader = null)
        {
            return GetNewestVersion(path, out var _, fullReader);
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="selected">List of selected hit objects</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>An editor for the beatmap</returns>
        public static BeatmapEditor GetNewestVersion(string path, out List<BeatmapHelper.HitObject> selected, EditorReader fullReader = null)
        {
            BeatmapEditor editor = new BeatmapEditor(path);
            selected = new List<BeatmapHelper.HitObject>();

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
        public static List<BeatmapHelper.HitObject> UpdateBeatmap(Beatmap beatmap, EditorReader reader)
        {
            beatmap.SetBookmarks(reader.bookmarks.Select<int, double>(o => o).ToList());

            beatmap.BeatmapTiming.TimingPoints = reader.controlPoints.Select(o => (TimingPoint)o).ToList();

            List<BeatmapHelper.HitObject> selected = new List<BeatmapHelper.HitObject>();
            beatmap.HitObjects = reader.hitObjects.Select(o => { var nho = (BeatmapHelper.HitObject)o; if (o.IsSelected) selected.Add(nho); return nho; }).ToList();

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
    }
}
