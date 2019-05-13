using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    public class Beatmap {
        public Dictionary<string, TValue> General { get; set; }
        public Dictionary<string, TValue> Editor { get; set; }
        public Dictionary<string, TValue> Metadata { get; set; }
        public Dictionary<string, TValue> Difficulty { get; set; }
        public List<Colour> ComboColours { get; set; }
        public Dictionary<string, Colour> SpecialColours { get; set; }
        public Timing BeatmapTiming { get; set; }
        public List<string> Events { get; set; }
        public List<HitObject> HitObjects { get; set; }
        public List<double> Bookmarks { get => GetBookmarks(); set => SetBookmarks(value); }


        public Beatmap(List<string> lines) {
            // Load up all the shit
            BeatmapTiming = new Timing(lines);

            string[] generalLines = GetCategoryLines(lines, "[General]");
            string[] editorLines = GetCategoryLines(lines, "[Editor]");
            string[] metadataLines = GetCategoryLines(lines, "[Metadata]");
            string[] difficultyLines = GetCategoryLines(lines, "[Difficulty]");
            string[] eventsLines = GetCategoryLines(lines, "[Events]");
            string[] colourLines = GetCategoryLines(lines, "[Colours]");
            string[] hitobjectLines = GetCategoryLines(lines, "[HitObjects]");

            General = new Dictionary<string, TValue>();
            Editor = new Dictionary<string, TValue>();
            Metadata = new Dictionary<string, TValue>();
            Difficulty = new Dictionary<string, TValue>();
            ComboColours = new List<Colour>();
            SpecialColours = new Dictionary<string, Colour>();
            Events = new List<string>();
            HitObjects = new List<HitObject>();

            FillDictionary(General, generalLines);
            FillDictionary(Editor, editorLines);
            FillDictionary(Metadata, metadataLines);
            FillDictionary(Difficulty, difficultyLines);

            foreach (string line in colourLines) {
                if (line.Substring(0, 5) == "Combo") {
                    ComboColours.Add(new Colour(line));
                } else {
                    SpecialColours[line.Split(':')[0].Trim()] = new Colour(line);
                }
            }
            foreach (string line in eventsLines) {
                Events.Add(line);
            }
            foreach (string line in hitobjectLines) {
                HitObjects.Add(new HitObject(line));
            }
            // Sort the HitObjects
            HitObjects = HitObjects.OrderBy(o => o.Time).ToList();

            CalculateSliderEndTimes();
            GiveObjectsGreenlines();
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
            foreach (HitObject ho in HitObjects) {
                ho.SV = BeatmapTiming.GetSVAtTime(ho.Time);
                ho.TP = BeatmapTiming.GetTimingPointAtTime(ho.Time);
                ho.HitsoundTP = BeatmapTiming.GetTimingPointAtTime(ho.Time + 5);
                ho.Redline = BeatmapTiming.GetRedlineAtTime(ho.Time);
                ho.BodyHitsounds = BeatmapTiming.GetTimingPointsInTimeRange(ho.Time, ho.EndTime);
            }
        }

        public List<HitObject> GetHitObjectsWithRangeInRange(double start, double end) {
            List<HitObject> list = new List<HitObject>();
            foreach (HitObject ho in HitObjects) {
                if (!(ho.EndTime <= start)) {
                    if (ho.Time >= end) // Range is after the range
                    {
                        break;
                    }
                    list.Add(ho);
                }
            }
            return list;
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
            foreach (string line in Events) {
                lines.Add(line);
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
            string fileName = String.Format("{0} - {1} ({2}) [{3}].osu", Metadata["Artist"].StringValue, Metadata["Title"].StringValue, Metadata["Creator"].StringValue, Metadata["Version"].StringValue);

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

        private void FillDictionary(Dictionary<string, TValue> dict, string[] lines) {
            foreach (string line in lines) {
                string[] split = line.Split(':');
                dict[split[0]] = new TValue(split[1]);
            }
        }

        private string[] GetCategoryLines(List<string> lines, string category) {
            List<string> categoryLines = new List<string>();
            bool atCategory = false;

            foreach (string line in lines) {
                if (atCategory && line != "") {
                    if (line[0] == '[') // Reached another category
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
            return categoryLines.ToArray();
        }
    }
}
