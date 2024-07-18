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
        /// Whether to add the overlay layer header even if there are no events in that layer.
        /// </summary>
        public bool ForceAddOverlayLayer { get; set; }

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
            var eventLines = FileFormatHelper.GetCategoryLines(lines, "[Events]").ToList();
            List<string> backgroundAndVideoEventsLines = new List<string>();
            List<string> breakPeriodsLines = new List<string>();
            List<string> storyboardLayerBackgroundLines = new List<string>();
            List<string> storyboardLayerFailLines = new List<string>();
            List<string> storyboardLayerPassLines = new List<string>();
            List<string> storyboardLayerForegroundLines = new List<string>();
            List<string> storyboardLayerOverlayLines = new List<string>();
            List<string> storyboardSoundSamplesLines =new List<string>();

            string[] backgroundAndVideoIdentifiers = {
                "0", "1", "Video"
            };
            string[] breakPeriodsIdentifiers = {
                "2", "Break"
            };
            string[] soundSampleIdentifiers = {
                "5", "Sample"
            };
            string[] categoryIdentifiers = {
                "//Storyboard Layer 0 (Background)",
                "//Storyboard Layer 1 (Fail)",
                "//Storyboard Layer 2 (Pass)",
                "//Storyboard Layer 3 (Foreground)",
                "//Storyboard Layer 4 (Overlay)"
            };
            string lastCategory = categoryIdentifiers[0];

            foreach (string line in eventLines) {
                if (backgroundAndVideoIdentifiers.Any(line.StartsWith)) {
                    backgroundAndVideoEventsLines.Add(line);
                } else if (breakPeriodsIdentifiers.Any(line.StartsWith)) {
                    breakPeriodsLines.Add(line);
                } else if (soundSampleIdentifiers.Any(line.StartsWith)) {
                    storyboardSoundSamplesLines.Add(line);
                } else if (categoryIdentifiers.Any(line.StartsWith)) {
                    lastCategory = line;
                } else if (!line.StartsWith("//")) {
                    switch (lastCategory) {
                        case "//Storyboard Layer 0 (Background)":
                            storyboardLayerBackgroundLines.Add(line);
                            break;
                        case "//Storyboard Layer 1 (Fail)":
                            storyboardLayerFailLines.Add(line);
                            break;
                        case "//Storyboard Layer 2 (Pass)":
                            storyboardLayerPassLines.Add(line);
                            break;
                        case "//Storyboard Layer 3 (Foreground)":
                            storyboardLayerForegroundLines.Add(line);
                            break;
                        case "//Storyboard Layer 4 (Overlay)":
                            storyboardLayerOverlayLines.Add(line);
                            break;
                    }
                }
            }

            ForceAddOverlayLayer = FileFormatHelper.CategoryExists(eventLines, "//Storyboard Layer 4 (Overlay)");

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
            lines.Add("//Storyboard Layer 0 (Background)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerBackground));
            lines.Add("//Storyboard Layer 1 (Fail)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerFail));
            lines.Add("//Storyboard Layer 2 (Pass)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerPass));
            lines.Add("//Storyboard Layer 3 (Foreground)");
            lines.AddRange(Event.SerializeEventTree(StoryboardLayerForeground));
            if (ForceAddOverlayLayer || StoryboardLayerOverlay.Count > 0) {
                lines.Add("//Storyboard Layer 4 (Overlay)");
                lines.AddRange(Event.SerializeEventTree(StoryboardLayerOverlay));
            }
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
