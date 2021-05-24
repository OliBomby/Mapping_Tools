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
        /// <summary>
        /// A list of all Events under the [Events] -> (Background and Video events) section.
        /// </summary>
        public List<Event> BackgroundAndVideoEvents { get; set; }

        /// <summary>
        /// A list of all Breaks under the [Events] -> (Break Periods) section.
        /// </summary>
        public List<Break> BreakPeriods { get; set; }

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
        public StoryBoard() {
            Initialize();
        }

        public StoryBoard(List<string> lines) {
            Initialize();
            SetLines(lines);
        }

        private void Initialize() {
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
        /// Creates the base string lines for the storyboard file.
        /// </summary>
        /// <param name="lines"></param>
        public void SetLines(List<string> lines) {
            // Load up all the stuff
            IEnumerable<string> backgroundAndVideoEventsLines = FileFormatHelper.GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            IEnumerable<string> breakPeriodsLines = FileFormatHelper.GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerBackgroundLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerFailLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerPassLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerForegroundLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            IEnumerable<string> storyboardLayerOverlayLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            IEnumerable<string> storyboardSoundSamplesLines = FileFormatHelper.GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });

            foreach (string line in backgroundAndVideoEventsLines) {
                BackgroundAndVideoEvents.Add(Event.MakeEvent(line));
            }
            foreach (string line in breakPeriodsLines) {
                BreakPeriods.Add(new Break(line));
            }

            StoryboardLayerBackground.AddRange(Event.ParseEventTree(storyboardLayerBackgroundLines));
            StoryboardLayerFail.AddRange(Event.ParseEventTree(storyboardLayerFailLines));
            StoryboardLayerPass.AddRange(Event.ParseEventTree(storyboardLayerPassLines));
            StoryboardLayerForeground.AddRange(Event.ParseEventTree(storyboardLayerForegroundLines));
            StoryboardLayerOverlay.AddRange(Event.ParseEventTree(storyboardLayerOverlayLines));

            foreach (string line in storyboardSoundSamplesLines) {
                StoryboardSoundSamples.Add(new StoryboardSoundSample(line));
            }
        }

        /// <summary>
        /// Returns a list of string with all the serialized contents of this storyboard.
        /// </summary>
        /// <returns></returns>
        public List<string> GetLines() {
            List<string> lines = new List<string>();
            AppendLines(lines);
            return lines;
        }

        /// <summary>
        /// Appends all serialized contents of this storyboards to specified list of strings.
        /// </summary>
        /// <param name="lines"></param>
        public void AppendLines(List<string> lines) {
            lines.Add("[Events]");
            lines.Add("//Background and Video events");
            lines.AddRange(BackgroundAndVideoEvents.Select(e => e.GetLine()));
            lines.Add("//Break Periods");
            lines.AddRange(BreakPeriods.Select(b => b.GetLine()));
            lines.Add("//Storyboard Layer 0 (Background)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerBackground));
            lines.Add("//Storyboard Layer 1 (Fail)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerFail));
            lines.Add("//Storyboard Layer 2 (Pass)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerPass));
            lines.Add("//Storyboard Layer 3 (Foreground)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerForeground));
            lines.Add("//Storyboard Layer 4 (Overlay)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerOverlay));
            lines.Add("//Storyboard Sound Samples");
            lines.AddRange(StoryboardSoundSamples.Select(sbss => sbss.GetLine()));
            lines.Add("");
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
    }
}
