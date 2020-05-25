using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mapping_Tools.Classes.BeatmapHelper.Events;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// 
    /// </summary>
    public class StoryBoard : ITextFile
    {
        /// <summary>
        /// List of storyboard actions that are defined within the .osb or .osu file
        /// </summary>
        public List<string> Events { get; set; }
        /// <summary>
        /// Events that happen to the playfield background, which includes beatmap backgrounds and video.
        /// </summary>
        public List<string> BackgroundAndVideoEvents { get; set; }
        /// <summary>
        /// (For .osu only) The times where a break is placed within the beatmap.
        /// </summary>
        public List<string> BreakPeriods { get; set; }
        /// <summary>
        /// The Layer events relating to the storyboard background.
        /// </summary>
        public List<string> StoryboardBackgroundLayer { get; set; }
        /// <summary>
        /// The layer events relating to the storyboard pass layer.
        /// </summary>
        public List<string> StoryboardPassLayer { get; set; }
        /// <summary>
        /// The layer events relating to the storyboard fail layer.
        /// </summary>
        public List<string> StoryboardFailLayer { get; set; }
        /// <summary>
        /// The layer events relating to the storyboard Foreground layer.
        /// </summary>
        public List<string> StoryboardForegroundLayer { get; set; }
        /// <summary>
        /// The layer events relating to the storyboard overlay layer.
        /// </summary>
        public List<string> StoryboardOverlayLayer { get; set; }
        /// <summary>
        /// The layer events relating to the storyboard samples.
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
            List<string> storyboardLayer0Lines = GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            List<string> storyboardLayer1Lines = GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            List<string> storyboardLayer2Lines = GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            List<string> storyboardLayer3Lines = GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            List<string> storyboardLayer4Lines = GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            List<string> storyboardSoundSamplesLines = GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });

            Events = new List<string>();
            BackgroundAndVideoEvents = new List<string>();
            BreakPeriods = new List<string>();
            StoryboardBackgroundLayer = new List<string>();
            StoryboardPassLayer = new List<string>();
            StoryboardFailLayer = new List<string>();
            StoryboardForegroundLayer = new List<string>();
            StoryboardOverlayLayer = new List<string>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();

            foreach (string line in eventsLines) {
                Events.Add(line);
            }
            foreach (string line in backgroundAndVideoEventsLines) {
                BackgroundAndVideoEvents.Add(line);
            }
            foreach (string line in breakPeriodsLines) {
                BreakPeriods.Add(line);
            }
            foreach (string line in storyboardLayer0Lines) {
                StoryboardBackgroundLayer.Add(line);
            }
            foreach (string line in storyboardLayer1Lines) {
                StoryboardPassLayer.Add(line);
            }
            foreach (string line in storyboardLayer2Lines) {
                StoryboardFailLayer.Add(line);
            }
            foreach (string line in storyboardLayer3Lines) {
                StoryboardForegroundLayer.Add(line);
            }
            foreach (string line in storyboardLayer4Lines) {
                StoryboardOverlayLayer.Add(line);
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

            foreach (string line in BackgroundAndVideoEvents) {
                lines.Add(line);
            }
            lines.Add("//Break Periods");
            foreach (string line in BreakPeriods) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 0 (Background)");
            foreach (string line in StoryboardBackgroundLayer) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 1 (Fail)");
            foreach (string line in StoryboardPassLayer) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 2 (Pass)");
            foreach (string line in StoryboardFailLayer) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 3 (Foreground)");
            foreach (string line in StoryboardForegroundLayer) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 4 (Overlay)");
            foreach (string line in StoryboardOverlayLayer) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Sound Samples");
            foreach (StoryboardSoundSample sbss in StoryboardSoundSamples) {
                lines.Add(sbss.GetLine());
            }
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
