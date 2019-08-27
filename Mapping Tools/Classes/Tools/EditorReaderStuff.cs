using Editor_Reader;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Mapping_Tools.Classes.Tools
{
    public static class EditorReaderStuff {
        private readonly static EditorReader editorReader = new EditorReader();

        public static EditorReader GetEditorReader() {
            return editorReader;
        }

        /// <summary>
        /// Gets the instance of EditorReader with FetchAll. Throws an exception if the editor is not open.
        /// </summary>
        /// <returns></returns>
        public static bool TryGetFullEditorReader(out EditorReader reader) {
            reader = editorReader;
            try {
                editorReader.FetchAll();
                return true;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Returns an editor for the beatmap of the specified path. If said beatmap is currently open in the editor it will update the Beatmap object with the latest values.
        /// </summary>
        /// <param name="path">Path to the beatmap</param>
        /// <param name="fullReader">Reader object that has already fetched all</param>
        /// <returns>An editor for the beatmap</returns>
        public static BeatmapEditor GetNewestVersion(string path, EditorReader fullReader = null) {
            BeatmapEditor editor = new BeatmapEditor(path);

            // Get a reader object that has everything fetched
            var reader = fullReader;
            if (reader == null)
                if (!TryGetFullEditorReader(out reader))
                    return editor;

            // Get the path from the beatmap in memory
            // This can only crash if the provided fullReader didn't fetch all values
            try {
                string songs = SettingsManager.GetSongsPath();
                string folder = reader.ContainingFolder;
                string filename = reader.Filename;
                string memoryPath = Path.Combine(songs, folder, filename);

                // Check whether the beatmap in the editor is the same as the beatmap you want
                if (memoryPath != path)
                    return editor;

                // Update the beatmap with memory values
                UpdateBeatmap(editor.Beatmap, reader);
            } catch {
                MessageBox.Show("Exception while editor reading.");
            }

            return editor;
        }

        /// <summary>
        /// Replaces hit objects and timing points with the values in the editor reader
        /// </summary>
        /// <param name="beatmap">Beatmap to replace values in</param>
        /// <param name="reader">Reader that contains the values from memory</param>
        public static void UpdateBeatmap(Beatmap beatmap, EditorReader reader) {
            beatmap.SetBookmarks(reader.bookmarks.Select<int, double>(o => o).ToList());

            beatmap.BeatmapTiming.TimingPoints = reader.controlPoints.Select(o => (TimingPoint)o).ToList();

            beatmap.HitObjects = reader.hitObjects.Select(o => (BeatmapHelper.HitObject)o).ToList();

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
        }
    }
}
