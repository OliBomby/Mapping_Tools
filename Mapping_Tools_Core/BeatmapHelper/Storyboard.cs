using Mapping_Tools_Core.BeatmapHelper.Events;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// Stores everything under the [Events] section of an osu beatmap or storyboard.
    /// </summary>
    public class Storyboard : IStoryboard {
        public List<Event> BackgroundAndVideoEvents { get; set; }

        public List<Break> BreakPeriods { get; set; }

        public List<Event> StoryboardLayerBackground { get; set; }

        public List<Event> StoryboardLayerFail { get; set; }

        public List<Event> StoryboardLayerPass { get; set; }

        public List<Event> StoryboardLayerForeground { get; set; }

        public List<Event> StoryboardLayerOverlay { get; set; }

        public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

        /// <summary>
        /// Initializes an empty storyboard.
        /// </summary>
        public Storyboard() {
            BackgroundAndVideoEvents = new List<Event>();
            BreakPeriods = new List<Break>();
            StoryboardLayerBackground = new List<Event>();
            StoryboardLayerPass = new List<Event>();
            StoryboardLayerFail = new List<Event>();
            StoryboardLayerForeground = new List<Event>();
            StoryboardLayerOverlay = new List<Event>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();
        }

        /// <summary>
        /// Grabs the specified file name of storyboard file.
        /// with format of:
        /// <c>Artist - Title (Host).osb</c>
        /// </summary>
        /// <returns>String of file name.</returns>
        public static string GetFileName(string artist, string title, string creator) {
            string fileName = $"{artist} - {title} ({creator}).osb";

            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
            fileName = r.Replace(fileName, "");
            return fileName;
        }
    }
}
