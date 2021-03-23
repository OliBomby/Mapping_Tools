using Mapping_Tools_Core.BeatmapHelper.Events;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// Stores everything under the [Events] section of an osu beatmap or storyboard.
    /// </summary>
    public class Storyboard : IStoryboard
    {
        /// <summary>
        /// A list of all Events under the [Events] -> (Background and Video events) section.
        /// </summary>
        public List<Event> BackgroundAndVideoEvents { get; set; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 0 (Background)) section.
        /// </summary>
        public List<Event> StoryboardLayerBackground { get; set; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 1 (Fail)) section.
        /// </summary>
        public List<Event> StoryboardLayerFail { get; set; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 2 (Pass)) section.
        /// </summary>
        public List<Event> StoryboardLayerPass { get; set; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
        /// </summary>
        public List<Event> StoryboardLayerForeground { get; set; }

        /// <summary>
        /// A list of all Events under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
        /// </summary>
        public List<Event> StoryboardLayerOverlay { get; set; }

        /// <summary>
        /// A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
        /// </summary>
        public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

        /// <summary>
        /// Initializes an empty storyboard.
        /// </summary>
        public Storyboard() {
            BackgroundAndVideoEvents = new List<Event>();
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
