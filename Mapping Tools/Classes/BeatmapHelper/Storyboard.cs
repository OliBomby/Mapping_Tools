using Mapping_Tools.Classes.BeatmapHelper.Events;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// 
    /// </summary>
    public class StoryBoard : ITextFile
    {
        // <summary>
        /// A list of all the lines of .osu code under the [Events] section. These strings are the actual .osu code and must be deserialized before use.
        /// Any changes to this property will not be serialized. Use <see cref="BackgroundAndVideoEvents"/> and the other sub-headers instead.
        /// </summary>
        public List<string> Events { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Background and Video events) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> BackgroundAndVideoEvents { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Break Periods) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Break> BreakPeriods { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 0 (Background)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerBackground { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 1 (Fail)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerFail { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 2 (Pass)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerPass { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerForeground { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerOverlay { get; set; }

        /// <summary>
        /// A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
        /// </summary>
        public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }

        /// <inheritdoc />
        public StoryBoard(List<string> lines) {
            SetLines(lines);
        }

        /// <summary>
        /// Creates the base string lines for the storyboard file.
        /// </summary>
        /// <param name="lines"></param>
        public void SetLines(List<string> lines) {
            // Load up all the stuff

            List<string> eventsLines = GetCategoryLines(lines, "[Events]");
            List<string> backgroundAndVideoEventsLines = GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            List<string> breakPeriodsLines = GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            List<string> storyboardLayerBackgroundLines = GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            List<string> storyboardLayerFailLines = GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            List<string> storyboardLayerPassLines = GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            List<string> storyboardLayerForegroundLines = GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            List<string> storyboardLayerOverlayLines = GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            List<string> storyboardSoundSamplesLines = GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });

            Events = new List<string>();
            BackgroundAndVideoEvents = new List<Event>();
            BreakPeriods = new List<Break>();
            StoryboardLayerBackground = new List<Event>();
            StoryboardLayerPass = new List<Event>();
            StoryboardLayerFail = new List<Event>();
            StoryboardLayerForeground = new List<Event>();
            StoryboardLayerOverlay = new List<Event>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();

            Events.AddRange(eventsLines);

            foreach (string line in backgroundAndVideoEventsLines) {
                BackgroundAndVideoEvents.Add(Event.MakeEvent(line));
            }
            foreach (string line in breakPeriodsLines) {
                BreakPeriods.Add(new Break(line));
            }
            foreach (string line in storyboardLayerBackgroundLines) {
                StoryboardLayerBackground.Add(Event.MakeEvent(line));
            }
            foreach (string line in storyboardLayerFailLines) {
                StoryboardLayerFail.Add(Event.MakeEvent(line));
            }
            foreach (string line in storyboardLayerPassLines) {
                StoryboardLayerPass.Add(Event.MakeEvent(line));
            }
            foreach (string line in storyboardLayerForegroundLines) {
                StoryboardLayerForeground.Add(Event.MakeEvent(line));
            }
            foreach (string line in storyboardLayerOverlayLines) {
                StoryboardLayerOverlay.Add(Event.MakeEvent(line));
            }
            foreach (string line in storyboardSoundSamplesLines) {
                StoryboardSoundSamples.Add(new StoryboardSoundSample(line));
            }
        }

        /// <summary>
        /// Returns with all
        /// </summary>
        /// <returns></returns>
        public List<string> GetLines() {
            // Getting all the Stuff
            List<string> lines = new List<string> {"[Events]", "//Background and Video events"};

            lines.AddRange(BackgroundAndVideoEvents.Select(e => e.GetLine()));
            lines.Add("//Break Periods");
            lines.AddRange(BreakPeriods.Select(b => b.GetLine()));
            lines.Add("//Storyboard Layer 0 (Background)");
            lines.AddRange(StoryboardLayerBackground.Select(e => e.GetLine()));
            lines.Add("//Storyboard Layer 1 (Fail)");
            lines.AddRange(StoryboardLayerPass.Select(e => e.GetLine()));
            lines.Add("//Storyboard Layer 2 (Pass)");
            lines.AddRange(StoryboardLayerFail.Select(e => e.GetLine()));
            lines.Add("//Storyboard Layer 3 (Foreground)");
            lines.AddRange(StoryboardLayerForeground.Select(e => e.GetLine()));
            lines.Add("//Storyboard Layer 4 (Overlay)");
            lines.AddRange(StoryboardLayerOverlay.Select(e => e.GetLine()));
            lines.Add("//Storyboard Sound Samples");
            lines.AddRange(StoryboardSoundSamples.Select(sbss => sbss.GetLine()));
            lines.Add("");

            return lines;
        }

        /// <summary>
        /// Grabs the specified file name of storyboard file.
        /// with format of:
        /// <c>Artist - Title (Host).osb</c>
        /// </summary>
        /// <returns>String of file name.</returns>
        public string GetFileName(string artist, string title, string creator) {
            string fileName = $"{artist} - {title} ({creator}).osb";

            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
            fileName = r.Replace(fileName, "");
            return fileName;
        }

        private List<string> GetCategoryLines(List<string> lines, string category, string[] categoryIdentifiers=null) {
            if (categoryIdentifiers == null)
                categoryIdentifiers = new[] { "[" };

            List<string> categoryLines = new List<string>();
            bool atCategory = false;

            foreach (string line in lines) {
                if (atCategory && line != "") {
                    if (categoryIdentifiers.Any(o => line.StartsWith(o))) // Reached another category
                    {
                        break;
                    }
                    categoryLines.Add(line);
                }
                else {
                    if (line == category) {
                        atCategory = true;
                    }
                }
            }
            return categoryLines;
        }
    }
}
