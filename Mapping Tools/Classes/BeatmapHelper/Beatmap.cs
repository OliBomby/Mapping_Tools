using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class Beatmap : ITextFile {
        public Dictionary<string, TValue> General { get; set; }
        public Dictionary<string, TValue> Editor { get; set; }
        public Dictionary<string, TValue> Metadata { get; set; }
        public Dictionary<string, TValue> Difficulty { get; set; }
        public List<Colour> ComboColours { get; set; }
        public Dictionary<string, Colour> SpecialColours { get; set; }
        public Timing BeatmapTiming { get; set; }
        public List<string> Events { get; set; }
        public List<string> BackgroundAndVideoEvents { get; set; }
        public List<string> BreakPeriods { get; set; }
        public List<string> StoryboardLayer0 { get; set; }
        public List<string> StoryboardLayer1 { get; set; }
        public List<string> StoryboardLayer2 { get; set; }
        public List<string> StoryboardLayer3 { get; set; }
        public List<string> StoryboardLayer4 { get; set; }
        public List<StoryboardSoundSample> StoryboardSoundSamples { get; set; }
        public List<HitObject> HitObjects { get; set; }
        public List<double> Bookmarks { get => GetBookmarks(); set => SetBookmarks(value); }

        public Beatmap(List<string> lines) {
            SetLines(lines);
        }

        public void SetLines(List<string> lines) {
            // Load up all the shit
            List<string> generalLines = GetCategoryLines(lines, "[General]");
            List<string> editorLines = GetCategoryLines(lines, "[Editor]");
            List<string> metadataLines = GetCategoryLines(lines, "[Metadata]");
            List<string> difficultyLines = GetCategoryLines(lines, "[Difficulty]");
            List<string> eventsLines = GetCategoryLines(lines, "[Events]");
            List<string> backgroundAndVideoEventsLines = GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            List<string> breakPeriodsLines = GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            List<string> storyboardLayer0Lines = GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            List<string> storyboardLayer1Lines = GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            List<string> storyboardLayer2Lines = GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            List<string> storyboardLayer3Lines = GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            List<string> storyboardLayer4Lines = GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            List<string> storyboardSoundSamplesLines = GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });
            List<string> timingLines = GetCategoryLines(lines, "[TimingPoints]");
            List<string> colourLines = GetCategoryLines(lines, "[Colours]");
            List<string> hitobjectLines = GetCategoryLines(lines, "[HitObjects]");

            General = new Dictionary<string, TValue>();
            Editor = new Dictionary<string, TValue>();
            Metadata = new Dictionary<string, TValue>();
            Difficulty = new Dictionary<string, TValue>();
            ComboColours = new List<Colour>();
            SpecialColours = new Dictionary<string, Colour>();
            Events = new List<string>();
            BackgroundAndVideoEvents = new List<string>();
            BreakPeriods = new List<string>();
            StoryboardLayer0 = new List<string>();
            StoryboardLayer1 = new List<string>();
            StoryboardLayer2 = new List<string>();
            StoryboardLayer3 = new List<string>();
            StoryboardLayer4 = new List<string>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();
            HitObjects = new List<HitObject>();

            FillDictionary(General, generalLines);
            FillDictionary(Editor, editorLines);
            FillDictionary(Metadata, metadataLines);
            FillDictionary(Difficulty, difficultyLines);

            foreach (string line in colourLines) {
                if (line.Substring(0, 5) == "Combo") {
                    ComboColours.Add(new Colour(line));
                } else {
                    SpecialColours[SplitKeyValue(line)[0].Trim()] = new Colour(line);
                }
            }
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
                StoryboardLayer0.Add(line);
            }
            foreach (string line in storyboardLayer1Lines) {
                StoryboardLayer1.Add(line);
            }
            foreach (string line in storyboardLayer2Lines) {
                StoryboardLayer2.Add(line);
            }
            foreach (string line in storyboardLayer3Lines) {
                StoryboardLayer3.Add(line);
            }
            foreach (string line in storyboardLayer4Lines) {
                StoryboardLayer4.Add(line);
            }
            foreach (string line in storyboardSoundSamplesLines) {
                StoryboardSoundSamples.Add(new StoryboardSoundSample(line));
            }
            foreach (string line in hitobjectLines) {
                HitObjects.Add(new HitObject(line));
            }

            // Set the timing object
            BeatmapTiming = new Timing(timingLines, Difficulty["SliderMultiplier"].Value);

            SortHitObjects();
            CalculateSliderEndTimes();
            GiveObjectsGreenlines();
        }

        public void SortHitObjects() {
            // Sort the HitObjects
            HitObjects = HitObjects.OrderBy(o => o.Time).ToList();
        }

        public void CalculateSliderEndTimes() {
            foreach (HitObject ho in HitObjects) {
                if (ho.IsSlider) {
                    ho.TemporalLength = BeatmapTiming.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                    ho.EndTime = Math.Floor(ho.Time + ho.TemporalLength * ho.Repeat);
                }
            }
        }

        public void GiveObjectsGreenlines() {
            foreach (var ho in HitObjects) {
                ho.SV = BeatmapTiming.GetSVAtTime(ho.Time);
                ho.TP = BeatmapTiming.GetTimingPointAtTime(ho.Time);
                ho.HitsoundTP = BeatmapTiming.GetTimingPointAtTime(ho.Time + 5);
                ho.Redline = BeatmapTiming.GetRedlineAtTime(ho.Time);
                ho.BodyHitsounds = BeatmapTiming.GetTimingPointsInTimeRange(ho.Time, ho.EndTime);
                foreach (var time in ho.GetAllTloTimes(BeatmapTiming)) {
                    ho.BodyHitsounds.RemoveAll(o => Math.Abs(time - o.Offset) <= 5);
                }
            }
        }

        public List<HitObject> GetHitObjectsWithRangeInRange(double start, double end) {
            return HitObjects.FindAll(o => o.EndTime >= start && o.Time <= end);
        }

        public Timeline GetTimeline() {
            Timeline tl = new Timeline(HitObjects, BeatmapTiming);
            tl.GiveTimingPoints(BeatmapTiming);
            return tl;
        }

        public List<double> GetBookmarks() {
            try {
                return Editor["Bookmarks"].GetStringValue().Split(',').Select(p => double.Parse(p)).ToList();
            }
            catch (KeyNotFoundException) {
                return new List<double>();
            }
        }

        public void SetBookmarks(List<double> bookmarks) {
            if (bookmarks.Count > 0) {
                Editor["Bookmarks"] = new TValue(string.Join(",", bookmarks.Select(d => Math.Round(d))));
            }
        }

        /// <summary>
        /// Returns all hit objects that have a bookmark in their range
        /// </summary>
        /// <returns>A list of hit objects that have a bookmark in their range</returns>
        public List<HitObject> GetBookmarkedObjects() {
            List<double> bookmarks = GetBookmarks();
            List<HitObject> markedObjects = HitObjects.FindAll(ho => bookmarks.Exists(o => (ho.Time <= o && o <= ho.EndTime)));
            return markedObjects;
        }

        public List<string> GetLines() {
            // Getting all the shit
            List<string> lines = new List<string>
            {
                "osu file format v14",
                "",
                "[General]"
            };
            AddDictionaryToLines(General, lines);
            lines.Add("");
            lines.Add("[Editor]");
            AddDictionaryToLines(Editor, lines);
            lines.Add("");
            lines.Add("[Metadata]");
            AddDictionaryToLines(Metadata, lines);
            lines.Add("");
            lines.Add("[Difficulty]");
            AddDictionaryToLines(Difficulty, lines);
            lines.Add("");
            lines.Add("[Events]");
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
            lines.Add("[TimingPoints]");
            foreach (TimingPoint tp in BeatmapTiming.TimingPoints) {
                if (tp == null) {
                    continue;
                }
                lines.Add(tp.GetLine());
            }
            lines.Add("");
            if (ComboColours.Count() > 0) {
                lines.Add("");
                lines.Add("[Colours]");
                for (int i = 0; i < ComboColours.Count; i++) {
                    lines.Add("Combo" + (i + 1) + " : " + ComboColours[i].ToString());
                }
                foreach (KeyValuePair<string, Colour> specialColour in SpecialColours) {
                    lines.Add(specialColour.Key + " : " + specialColour.Value.ToString());
                }
            }
            lines.Add("");
            lines.Add("[HitObjects]");
            foreach (HitObject ho in HitObjects) {
                lines.Add(ho.GetLine());
            }

            return lines;
        }

        public string GetFileName() {
            string fileName = string.Format("{0} - {1} ({2}) [{3}].osu", Metadata["Artist"].StringValue, Metadata["Title"].StringValue, Metadata["Creator"].StringValue, Metadata["Version"].StringValue);

            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            fileName = r.Replace(fileName, "");
            return fileName;
        }

        private void AddDictionaryToLines(Dictionary<string, TValue> dict, List<string> lines) {
            foreach (KeyValuePair<string, TValue> kvp in dict) {
                lines.Add(kvp.Key + ":" + kvp.Value.StringValue);
            }
        }

        private void FillDictionary(Dictionary<string, TValue> dict, List<string> lines) {
            foreach (string line in lines) {
                string[] split = SplitKeyValue(line);
                dict[split[0]] = new TValue(split[1]);
            }
        }

        private string[] SplitKeyValue(string line) {
            return line.Split(new[] { ':' }, 2);
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
