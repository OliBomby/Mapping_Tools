using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class StoryBoard : ITextFile
    {
        public List<string> Events { get; set; }
        public List<string> BackgroundAndVideoEvents { get; set; }
        public List<string> BreakPeriods { get; set; }
        public List<string> StoryboardLayer0 { get; set; }
        public List<string> StoryboardLayer1 { get; set; }
        public List<string> StoryboardLayer2 { get; set; }
        public List<string> StoryboardLayer3 { get; set; }
        public List<string> StoryboardLayer4 { get; set; }
        public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }


        public StoryBoard(List<string> lines) {
            // Load up all the shit

            List<string> eventsLines = GetCategoryLines(lines, "[Events]");
            List<string> BackgroundAndVideoEventsLines = GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            List<string> BreakPeriodsLines = GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            List<string> StoryboardLayer0Lines = GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            List<string> StoryboardLayer1Lines = GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            List<string> StoryboardLayer2Lines = GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            List<string> StoryboardLayer3Lines = GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            List<string> StoryboardLayer4Lines = GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            List<string> StoryboardSoundSamplesLines = GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });

            BackgroundAndVideoEvents = new List<string>();
            BreakPeriods = new List<string>();
            StoryboardLayer0 = new List<string>();
            StoryboardLayer1 = new List<string>();
            StoryboardLayer2 = new List<string>();
            StoryboardLayer3 = new List<string>();
            StoryboardLayer4 = new List<string>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();

            foreach (string line in eventsLines) {
                Events.Add(line);
            }
            foreach (string line in BackgroundAndVideoEventsLines) {
                BackgroundAndVideoEvents.Add(line);
            }
            foreach (string line in BreakPeriodsLines) {
                BreakPeriods.Add(line);
            }
            foreach (string line in StoryboardLayer0Lines) {
                StoryboardLayer0.Add(line);
            }
            foreach (string line in StoryboardLayer1Lines) {
                StoryboardLayer1.Add(line);
            }
            foreach (string line in StoryboardLayer2Lines) {
                StoryboardLayer2.Add(line);
            }
            foreach (string line in StoryboardLayer3Lines) {
                StoryboardLayer3.Add(line);
            }
            foreach (string line in StoryboardLayer4Lines) {
                StoryboardLayer4.Add(line);
            }
            foreach (string line in StoryboardSoundSamplesLines) {
                StoryboardSoundSamples.Add(new StoryboardSoundSample(line));
            }
        }

        public List<string> GetLines() {
            // Getting all the shit
            List<string> lines = new List<string>
            {
                "[Events]"
            };
            lines.Add("//Background and Video events");
            foreach (string line in BackgroundAndVideoEvents) {
                lines.Add(line);
            }
            lines.Add("//Break Periods");
            foreach (string line in BreakPeriods) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 0 (Background)");
            foreach (string line in StoryboardLayer0) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 1 (Fail)");
            foreach (string line in StoryboardLayer1) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 2 (Pass)");
            foreach (string line in StoryboardLayer2) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 3 (Foreground)");
            foreach (string line in StoryboardLayer3) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Layer 4 (Overlay)");
            foreach (string line in StoryboardLayer4) {
                lines.Add(line);
            }
            lines.Add("//Storyboard Sound Samples");
            foreach (StoryboardSoundSample sbss in StoryboardSoundSamples) {
                lines.Add(sbss.GetLine());
            }
            lines.Add("");

            return lines;
        }

        public string GetFileName(string artist, string title, string creator) {
            string fileName = string.Format("{0} - {1} ({2}).osb", artist, title, creator);

            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
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
