using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.BeatmapHelper {

    /// <summary>
    /// Class containing all the data from a .osu beatmap file. It also supports serialization to .osu format and helper methods to get data in specific ways.
    /// </summary>
    public class Beatmap : ITextFile {

        /// <summary>
        /// Contains all the values in the [General] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// AudioFilename,
        /// AudioLeadIn,
        /// PreviewTime,
        /// Countdown,
        /// SampleSet,
        /// StackLeniency,
        /// Mode,
        /// LetterboxInBreaks,
        /// StoryFireInFront,
        /// SkinPreference,
        /// EpilepsyWarning,
        /// CountdownOffset,
        /// SpecialStyle,
        /// WidescreenStoryboard,
        /// SamplesMatchPlaybackRate
        /// </summary>
        public Dictionary<string, TValue> General { get; set; }

        /// <summary>
        /// Contains all the values in the [Editor] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// Bookmarks,
        /// DistanceSpacing,
        /// BeatDivisor,
        /// GridSize,
        /// TimelineZoom
        /// </summary>
        public Dictionary<string, TValue> Editor { get; set; }

        /// <summary>
        /// Contains all the values in the [Metadata] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// Title,
        /// TitleUnicode,
        /// Artist,
        /// ArtistUnicode,
        /// Creator,
        /// Version,
        /// Source,
        /// Tags,
        /// BeatmapID,
        /// BeatmapSetID
        /// </summary>
        public Dictionary<string, TValue> Metadata { get; set; }

        /// <summary>
        /// Contains all the values in the [Difficulty] section of a .osu file. The key is the variable name and the value is the value.
        /// This section typically contains:
        /// HPDrainRate,
        /// CircleSize,
        /// OverallDifficulty,
        /// ApproachRate,
        /// SliderMultiplier,
        /// SliderTickRate
        /// </summary>
        public Dictionary<string, TValue> Difficulty { get; set; }

        /// <summary>
        /// Contains all the basic combo colours. The order of this list is the same as how they are numbered in the .osu.
        /// There can not be more than 8 combo colours.
        /// <c>Combo1 : 245,222,139</c>
        /// </summary>
        public List<ComboColour> ComboColours { get; set; }

        /// <summary>
        /// Contains all the special colours. These include the colours of slider bodies or slider outlines.
        /// The key is the name of the special colour and the value is the actual colour.
        /// </summary>
        public Dictionary<string, ComboColour> SpecialColours { get; set; }

        /// <summary>
        /// The timing of this beatmap. This objects contains all the timing points (data from the [TimingPoints] section) plus the global slider multiplier.
        /// It also has a number of helper methods to fetch data from the timing points.
        /// With this object you can always calculate the slider velocity at any time.
        /// Any changes to the slider multiplier property in this object will not be serialized. Change the value in <see cref="Difficulty"/> instead.
        /// </summary>
        public Timing BeatmapTiming { get; set; }

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

        /// <summary>
        /// List of all the hit objects in this beatmap.
        /// </summary>
        public List<HitObject> HitObjects { get; set; }

        /// <summary>
        /// Gets or sets the bookmarks of this beatmap. This returns a clone of the real bookmarks which are stored in the <see cref="Editor"/> property.
        /// The bookmarks are represented with just a double which is the time of the bookmark.
        /// </summary>
        public List<double> Bookmarks { get => GetBookmarks(); set => SetBookmarks(value); }

        /// <summary>
        /// When true, all coordinates and times will be serialized without rounding.
        /// </summary>
        public bool SaveWithFloatPrecision { get; set; }

        /// <summary>
        /// Initializes the Beatmap file format.
        /// </summary>
        /// <param name="lines">List of strings where each string is another line in the .osu file.</param>
        public Beatmap(List<string> lines) {
            SetLines(lines);
        }

        /// <summary>
        /// Deserializes an entire .osu file and stores the data to this object.
        /// </summary>
        /// <param name="lines">List of strings where each string is another line in the .osu file.</param>
        public void SetLines(List<string> lines) {
            // Load up all the shit
            List<string> generalLines = GetCategoryLines(lines, "[General]");
            List<string> editorLines = GetCategoryLines(lines, "[Editor]");
            List<string> metadataLines = GetCategoryLines(lines, "[Metadata]");
            List<string> difficultyLines = GetCategoryLines(lines, "[Difficulty]");
            List<string> backgroundAndVideoEventsLines = GetCategoryLines(lines, "//Background and Video events", new[] { "[", "//" });
            List<string> breakPeriodsLines = GetCategoryLines(lines, "//Break Periods", new[] { "[", "//" });
            List<string> storyboardLayerBackgroundLines = GetCategoryLines(lines, "//Storyboard Layer 0 (Background)", new[] { "[", "//" });
            List<string> storyboardLayerFailLines = GetCategoryLines(lines, "//Storyboard Layer 1 (Fail)", new[] { "[", "//" });
            List<string> storyboardLayerPassLines = GetCategoryLines(lines, "//Storyboard Layer 2 (Pass)", new[] { "[", "//" });
            List<string> storyboardLayerForegroundLines = GetCategoryLines(lines, "//Storyboard Layer 3 (Foreground)", new[] { "[", "//" });
            List<string> storyboardLayerOverlayLines = GetCategoryLines(lines, "//Storyboard Layer 4 (Overlay)", new[] { "[", "//" });
            List<string> storyboardSoundSamplesLines = GetCategoryLines(lines, "//Storyboard Sound Samples", new[] { "[", "//" });
            List<string> timingLines = GetCategoryLines(lines, "[TimingPoints]");
            List<string> colourLines = GetCategoryLines(lines, "[Colours]");
            List<string> hitobjectLines = GetCategoryLines(lines, "[HitObjects]");

            General = new Dictionary<string, TValue>();
            Editor = new Dictionary<string, TValue>();
            Metadata = new Dictionary<string, TValue>();
            Difficulty = new Dictionary<string, TValue>();
            ComboColours = new List<ComboColour>();
            SpecialColours = new Dictionary<string, ComboColour>();
            BackgroundAndVideoEvents = new List<Event>();
            BreakPeriods = new List<Break>();
            StoryboardLayerBackground = new List<Event>();
            StoryboardLayerPass = new List<Event>();
            StoryboardLayerFail = new List<Event>();
            StoryboardLayerForeground = new List<Event>();
            StoryboardLayerOverlay = new List<Event>();
            StoryboardSoundSamples = new List<StoryboardSoundSample>();
            HitObjects = new List<HitObject>();

            FillDictionary(General, generalLines);
            FillDictionary(Editor, editorLines);
            FillDictionary(Metadata, metadataLines);
            FillDictionary(Difficulty, difficultyLines);

            foreach (string line in colourLines) {
                if (line.Substring(0, 5) == "Combo") {
                    ComboColours.Add(new ComboColour(line));
                } else {
                    SpecialColours[SplitKeyValue(line)[0].Trim()] = new ComboColour(line);
                }
            }

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
            foreach (string line in hitobjectLines) {
                HitObjects.Add(new HitObject(line));
            }

            // Set the timing object
            BeatmapTiming = new Timing(timingLines, Difficulty["SliderMultiplier"].DoubleValue);

            SortHitObjects();
            CalculateHitObjectComboStuff();
            CalculateSliderEndTimes();
            GiveObjectsGreenlines();
        }

        /// <summary>
        /// Sorts all hitobjects in map by order of time.
        /// </summary>
        public void SortHitObjects() {
            HitObjects.Sort();
        }

        /// <summary>
        /// Calculates the temporal length for all <see cref="HitObject"/> sliders and stores it to their internal property.
        /// </summary>
        public void CalculateSliderEndTimes() {
            foreach (var ho in HitObjects.Where(ho => ho.IsSlider)) {
                if (double.IsNaN(ho.PixelLength) || ho.PixelLength < 0 || ho.CurvePoints.All(o => o == ho.Pos)) {
                    ho.TemporalLength = 0;
                }
                else {
                    ho.TemporalLength = BeatmapTiming.CalculateSliderTemporalLength(ho.Time, ho.PixelLength);
                }
            }
        }
        
        /// <summary>
        /// Calculates the which hit objects actually have a new combo.
        /// Calculates the combo index and combo colours for each hit object.
        /// This includes cases where the previous hit object is a spinner or doesnt exist.
        /// </summary>
        public void CalculateHitObjectComboStuff() {
            HitObject previousHitObject = null;
            int colourIndex = 0;
            int comboIndex = 0;

            // If there are no combo colours use the default combo colours so the hitobjects still have something
            var actingComboColours = ComboColours.Count == 0 ? ComboColour.GetDefaultComboColours() : ComboColours.ToArray();

            foreach (var hitObject in HitObjects) {
                hitObject.ActualNewCombo = IsNewCombo(hitObject, previousHitObject);

                if (hitObject.ActualNewCombo) {
                    var colourIncrement = hitObject.ComboSkip;
                    if (!hitObject.IsSpinner) {
                        colourIncrement++;
                    }

                    colourIndex = MathHelper.Mod(colourIndex + colourIncrement, actingComboColours.Length);
                    comboIndex = 1;
                } else {
                    comboIndex++;
                }

                hitObject.ComboIndex = comboIndex;
                hitObject.ColourIndex = colourIndex;
                hitObject.Colour = actingComboColours[colourIndex];

                previousHitObject = hitObject;
            }
        }

        public static bool IsNewCombo(HitObject hitObject, HitObject previousHitObject) {
            return hitObject.NewCombo || hitObject.IsSpinner || previousHitObject == null || previousHitObject.IsSpinner;
        }

        /// <summary>
        /// For each hit object it stores the timingpoints from <see cref="BeatmapTiming"/> which are affecting that hit object.
        /// Basically making all hit objects aware of the effects on themselves coming from the <see cref="BeatmapTiming"/>.
        /// </summary>
        public void GiveObjectsGreenlines() {
            foreach (var ho in HitObjects) {
                ho.SliderVelocity = BeatmapTiming.GetSvAtTime(ho.Time);
                ho.TimingPoint = BeatmapTiming.GetTimingPointAtTime(ho.Time);
                ho.HitsoundTimingPoint = BeatmapTiming.GetTimingPointAtTime(ho.Time + 5);
                ho.UnInheritedTimingPoint = BeatmapTiming.GetRedlineAtTime(ho.Time);
                ho.BodyHitsounds = BeatmapTiming.GetTimingPointsInTimeRange(ho.Time, ho.EndTime);
                foreach (var time in ho.GetAllTloTimes(BeatmapTiming)) {
                    ho.BodyHitsounds.RemoveAll(o => Math.Abs(time - o.Offset) <= 5);
                }
            }
        }

        /// <summary>
        /// Calculates the time in milliseconds between a hit object appearing on screen and getting perfectly hit for a given approach rate value.
        /// </summary>
        /// <param name="approachRate">The approach rate difficulty setting.</param>
        /// <returns>The time in milliseconds between a hit object appearing on screen and getting perfectly hit.</returns>
        public static double ApproachRateToMs(double approachRate) {
            if (approachRate < 5) {
                return 1800 - 120 * approachRate;
            }

            return 1200 - 150 * (approachRate - 5);
        }

        /// <summary>
        /// Finds all hit objects from this beatmap which are within a specified range.
        /// Just any part of the hit object has to overlap with the time range in order to be included.
        /// </summary>
        /// <param name="start">The start of the time range.</param>
        /// <param name="end">The end of the time range.</param>
        /// <returns>All <see cref="HitObject"/> that are found within specified range.</returns>
        public List<HitObject> GetHitObjectsWithRangeInRange(double start, double end) {
            return HitObjects.FindAll(o => o.EndTime >= start && o.Time <= end);
        }

        /// <summary>
        /// Creates a new <see cref="Timeline"/> for this Beatmap.
        /// Upon creation the timeline is updated with all the current timing and hitsounds of this beatmap,
        /// but later changes wont be automatically synchronized.
        /// </summary>
        /// <returns></returns>
        public Timeline GetTimeline() {
            Timeline tl = new Timeline(HitObjects, BeatmapTiming);
            tl.GiveTimingPoints(BeatmapTiming);
            return tl;
        }

        /// <summary>
        /// Grabs all bookmarks
        /// </summary>
        /// <returns>The list of Bookmarks.</returns>
        public List<double> GetBookmarks() {
            try {
                return Editor["Bookmarks"].GetDoubleList();
            }
            catch (KeyNotFoundException) {
                return new List<double>();
            }
        }

        /// <summary>
        /// Sets the bookmarks value in <see cref="Editor"/> with a new list of bookmarks.
        /// Decimal values will be rounded in this process.
        /// </summary>
        /// <param name="bookmarks"></param>
        public void SetBookmarks(List<double> bookmarks) {
            if (bookmarks.Count > 0) {
                Editor["Bookmarks"] = new TValue(string.Join(",", bookmarks.Select(d => Math.Round(d))));
            }
        }

        /// <summary>
        /// Returns all hit objects that have a bookmark in their range.
        /// </summary>
        /// <returns>A list of hit objects that have a bookmark in their range.</returns>
        public List<HitObject> GetBookmarkedObjects() {
            List<double> bookmarks = GetBookmarks();
            List<HitObject> markedObjects = HitObjects.FindAll(ho => bookmarks.Exists(o => (ho.Time <= o && o <= ho.EndTime)));
            return markedObjects;
        }

        /// <summary>
        /// Serializes all data of this beatmap to .osu format.
        /// </summary>
        /// <returns>List of lines of .osu code.</returns>
        public List<string> GetLines() {
            // Getting all the stuff
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
            lines.Add("[TimingPoints]");
            lines.AddRange(BeatmapTiming.TimingPoints.Where(tp => tp != null).Select(tp => {
                tp.SaveWithFloatPrecision = SaveWithFloatPrecision;
                return tp.GetLine();
            }));
            lines.Add("");
            if (ComboColours.Any()) {
                lines.Add("");
                lines.Add("[Colours]");
                for (int i = 0; i < ComboColours.Count; i++) {
                    lines.Add("Combo" + (i + 1) + " : " + ComboColours[i]);
                }
                foreach (KeyValuePair<string, ComboColour> specialColour in SpecialColours) {
                    lines.Add(specialColour.Key + " : " + specialColour.Value);
                }
            }
            lines.Add("");
            lines.Add("[HitObjects]");
            lines.AddRange(HitObjects.Select(ho => {
                ho.SaveWithFloatPrecision = SaveWithFloatPrecision;
                return ho.GetLine();
            }));

            return lines;
        }

        public double GetHitObjectStartTime() {
            return HitObjects.Min(h => h.Time);
        }

        public double GetHitObjectEndTime() {
            return HitObjects.Max(h => h.EndTime);
        }

        public void OffsetTime(double offset) {
            BeatmapTiming.TimingPoints?.ForEach(tp => tp.Offset += offset);
            HitObjects?.ForEach(h => h.MoveTime(offset));
        }

        private IEnumerable<Event> EnumerateAllEvents() {
            return BackgroundAndVideoEvents.Concat(BreakPeriods).Concat(StoryboardSoundSamples)
                .Concat(StoryboardLayerFail).Concat(StoryboardLayerPass).Concat(StoryboardLayerBackground)
                .Concat(StoryboardLayerForeground).Concat(StoryboardLayerOverlay);
        }

        public double GetLeadInTime() {
            var leadInTime = General["AudioLeadIn"].DoubleValue;
            leadInTime = Math.Max(-EnumerateAllEvents().OfType<IHasStartTime>().Min(o => o.StartTime), leadInTime);
            if (HitObjects.Count > 0)
                leadInTime = Math.Max(Difficulty["ApproachRate"].DoubleValue - HitObjects[0].Time, leadInTime);
            return leadInTime;
        }

        public double GetMapStartTime() {
            return -GetLeadInTime();
        }

        public double GetMapEndTime() {
            var hitObjectEndTime = HitObjects.Count > 0
                ? Math.Max(GetHitObjectEndTime() + 200, HitObjects.Last().EndTime + 3000)
                : double.NegativeInfinity;
            var lastEventTime = EnumerateAllEvents().OfType<IHasEndTime>()
                .Max(o => o.EndTime);
            return Math.Max(hitObjectEndTime, lastEventTime - 500);
        }

        /// <summary>
        /// Finds the objects refered by specified time code.
        /// Example time code: <example>00:56:823 (1,2,1,2) - </example>
        /// </summary>
        /// <param name="code">The time code</param>
        /// <returns></returns>
        public IEnumerable<HitObject> QueryTimeCode(string code) {
            var startBracketIndex = code.IndexOf("(", StringComparison.Ordinal);
            var endBracketIndex = code.IndexOf(")", StringComparison.Ordinal);

            // Extract the list of combo numbers from the code
            IEnumerable<int> comboNumbers;
            if (startBracketIndex == -1) {
                // If there is not start bracket, then we assume that there is no list of combo numbers in the code
                // -1 means just get any combo number
                comboNumbers = new[] {-1};
            } else {
                if (endBracketIndex == -1) {
                    endBracketIndex = code.Length - 1;
                }

                // Get the part of the code between the brackets
                var comboNumbersString = code.Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);

                comboNumbers = comboNumbersString.Split(',').Select(int.Parse);
            }

            // Parse the time span in the code
            var time = TimeSpan.ParseExact(
                code.Substring(0, startBracketIndex == -1 ? code.Length : startBracketIndex - 1).Trim(),
                @"mm\:ss\:fff", CultureInfo.InvariantCulture, TimeSpanStyles.None).TotalMilliseconds;

            // Enumerate through the hit objects from the first object at the time
            int objectIndex = HitObjects.FindIndex(h => h.Time >= time);
            foreach (var comboNumber in comboNumbers) {
                while (comboNumber != -1 && objectIndex < HitObjects.Count && HitObjects[objectIndex].ComboIndex != comboNumber) {
                    objectIndex++;
                }

                if (objectIndex >= HitObjects.Count)
                    yield break;

                yield return HitObjects[objectIndex++];
            }
        }

        /// <summary>
        /// Grabs the specified file name of beatmap file.
        /// with format of:
        /// <c>Artist - Title (Host) [Difficulty].osu</c>
        /// </summary>
        /// <returns>String of file name.</returns>
        public string GetFileName() {
            return GetFileName(Metadata["Artist"].Value, Metadata["Title"].Value,
                Metadata["Creator"].Value, Metadata["Version"].Value);
        }

        /// <summary>
        /// Grabs the specified file name of beatmap file.
        /// with format of:
        /// <c>Artist - Title (Host) [Difficulty].osu</c>
        /// </summary>
        /// <returns>String of file name.</returns>
        public static string GetFileName(string artist, string title, string creator, string version) {
            string fileName = $"{artist} - {title} ({creator}) [{version}].osu";

            string regexSearch = new string(Path.GetInvalidFileNameChars());
            Regex r = new Regex($"[{Regex.Escape(regexSearch)}]");
            fileName = r.Replace(fileName, "");
            return fileName;
        }

        private static void AddDictionaryToLines(Dictionary<string, TValue> dict, List<string> lines) {
            lines.AddRange(dict.Select(kvp => kvp.Key + ":" + kvp.Value.Value));
        }

        private static void FillDictionary(Dictionary<string, TValue> dict, List<string> lines) {
            foreach (var split in lines.Select(SplitKeyValue)) {
                dict[split[0]] = new TValue(split[1]);
            }
        }

        private static string[] SplitKeyValue(string line) {
            return line.Split(new[] { ':' }, 2);
        }

        private static List<string> GetCategoryLines(List<string> lines, string category, string[] categoryIdentifiers=null) {
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

        public Beatmap DeepCopy() {
            var newBeatmap = (Beatmap)MemberwiseClone();
            newBeatmap.HitObjects = HitObjects?.Select(h => h.DeepCopy()).ToList();
            newBeatmap.BeatmapTiming = new Timing(BeatmapTiming.TimingPoints.Select(t => t.Copy()).ToList(), BeatmapTiming.SliderMultiplier);
            newBeatmap.GiveObjectsGreenlines();
            return newBeatmap;
        }
    }
}
