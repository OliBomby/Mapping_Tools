using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        /// The storyboard of the Beatmap. Stores everything under the [Events] section.
        /// </summary>
        public StoryBoard StoryBoard { get; set; }

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Background and Video events) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> BackgroundAndVideoEvents => StoryBoard.BackgroundAndVideoEvents;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Break Periods) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Break> BreakPeriods => StoryBoard.BreakPeriods;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 0 (Background)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerBackground => StoryBoard.StoryboardLayerBackground;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 1 (Fail)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerFail => StoryBoard.StoryboardLayerFail;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 2 (Pass)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerPass => StoryBoard.StoryboardLayerPass;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 3 (Foreground)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerForeground => StoryBoard.StoryboardLayerForeground;

        /// <summary>
        /// A list of all the lines of .osu code under the [Events] -> (Storyboard Layer 4 (Overlay)) section.
        /// These strings are the actual .osu code and must be deserialized before use.
        /// </summary>
        public List<Event> StoryboardLayerOverlay => StoryBoard.StoryboardLayerOverlay;

        /// <summary>
        /// A list of all storyboarded sound sample events under the [Events] -> (Storyboard Sound Samples) section.
        /// </summary>
        public List<StoryboardSoundSample> StoryboardSoundSamples => StoryBoard.StoryboardSoundSamples;

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
        /// Initializes a new Beatmap.
        /// </summary>
        public Beatmap() {
            Initialize();
        }

        /// <summary>
        /// Initializes a beatmap with the provided hit objects and timing points.
        /// </summary>
        /// <param name="hitObjects"></param>
        /// <param name="timingPoints"></param>
        /// <param name="firstUnInheritedTimingPoint"></param>
        /// <param name="globalSv"></param>
        /// <param name="gameMode"></param>
        public Beatmap(List<HitObject> hitObjects, List<TimingPoint> timingPoints,
            TimingPoint firstUnInheritedTimingPoint = null, double globalSv = 1.4, GameMode gameMode = GameMode.Standard) {
            Initialize();

            // Set the hit objects
            HitObjects = hitObjects;

            // Set the timing stuff
            BeatmapTiming.SetTimingPoints(timingPoints);
            BeatmapTiming.SliderMultiplier = globalSv;

            if (!BeatmapTiming.Contains(firstUnInheritedTimingPoint)) {
                BeatmapTiming.Add(firstUnInheritedTimingPoint);
            }

            // Set the global SV here too because thats absolutely necessary
            Difficulty["SliderMultiplier"] = new TValue(globalSv.ToInvariant());
            General["Mode"] = new TValue(((int) gameMode).ToInvariant());

            SortHitObjects();
            CalculateSliderEndTimes();
            GiveObjectsGreenlines();
            CalculateHitObjectComboStuff();
        }

        /// <summary>
        /// Initializes the Beatmap file format.
        /// </summary>
        /// <param name="lines">List of strings where each string is another line in the .osu file.</param>
        public Beatmap(List<string> lines) {
            Initialize();
            SetLines(lines);
        }

        private void Initialize() {
            General = new Dictionary<string, TValue>();
            Editor = new Dictionary<string, TValue>();
            Metadata = new Dictionary<string, TValue>();
            Difficulty = new Dictionary<string, TValue>();
            ComboColours = new List<ComboColour>();
            SpecialColours = new Dictionary<string, ComboColour>();
            StoryBoard = new StoryBoard();
            HitObjects = new List<HitObject>();
            BeatmapTiming = new Timing(1.4);

            FillBasicMetadata();
        }

        public void FillBasicMetadata() {
            General["AudioFilename"] = new TValue(string.Empty);
            General["AudioLeadIn"] = new TValue("0");
            General["PreviewTime"] = new TValue("-1");
            General["Countdown"] = new TValue("0");
            General["SampleSet"] = new TValue("Soft");
            General["StackLeniency"] = new TValue("0.2");
            General["Mode"] = new TValue("0");
            General["LetterboxInBreaks"] = new TValue("0");
            General["WidescreenStoryboard"] = new TValue("0");

            Metadata["Title"] = new TValue(string.Empty);
            Metadata["TitleUnicode"] = new TValue(string.Empty);
            Metadata["Artist"] = new TValue(string.Empty);
            Metadata["ArtistUnicode"] = new TValue(string.Empty);
            Metadata["Creator"] = new TValue(string.Empty);
            Metadata["Version"] = new TValue(string.Empty);
            Metadata["Source"] = new TValue(string.Empty);
            Metadata["Tags"] = new TValue(string.Empty);
            Metadata["BeatmapID"] = new TValue("0");
            Metadata["BeatmapSetID"] = new TValue("-1");

            Difficulty["HPDrainRate"] = new TValue("5");
            Difficulty["CircleSize"] = new TValue("5");
            Difficulty["OverallDifficulty"] = new TValue("5");
            Difficulty["ApproachRate"] = new TValue("5");
            Difficulty["SliderMultiplier"] = new TValue("1.4");
            Difficulty["SliderTickRate"] = new TValue("1");
        }

        /// <summary>
        /// Deserializes an entire .osu file and stores the data to this object.
        /// </summary>
        /// <param name="lines">List of strings where each string is another line in the .osu file.</param>
        public void SetLines(List<string> lines) {
            // Load up all the shit
            IEnumerable<string> generalLines = FileFormatHelper.GetCategoryLines(lines, "[General]");
            IEnumerable<string> editorLines = FileFormatHelper.GetCategoryLines(lines, "[Editor]");
            IEnumerable<string> metadataLines = FileFormatHelper.GetCategoryLines(lines, "[Metadata]");
            IEnumerable<string> difficultyLines = FileFormatHelper.GetCategoryLines(lines, "[Difficulty]");
            IEnumerable<string> timingLines = FileFormatHelper.GetCategoryLines(lines, "[TimingPoints]");
            IEnumerable<string> colourLines = FileFormatHelper.GetCategoryLines(lines, "[Colours]");
            IEnumerable<string> hitobjectLines = FileFormatHelper.GetCategoryLines(lines, "[HitObjects]");

            FileFormatHelper.FillDictionary(General, generalLines);
            FileFormatHelper.FillDictionary(Editor, editorLines);
            FileFormatHelper.FillDictionary(Metadata, metadataLines);
            FileFormatHelper.FillDictionary(Difficulty, difficultyLines);

            foreach (string line in colourLines) {
                if (line.Substring(0, 5) == "Combo") {
                    ComboColours.Add(new ComboColour(line));
                } else {
                    SpecialColours[FileFormatHelper.SplitKeyValue(line)[0].Trim()] = new ComboColour(line);
                }
            }

            foreach (string line in hitobjectLines) {
                HitObjects.Add(new HitObject(line));
            }

            // Give the lines to the storyboard
            StoryBoard.SetLines(lines);

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
                ho.CalculateSliderTemporalLength(BeatmapTiming, false);
            }
        }

        /// <summary>
        /// Calculates the end position for all hit objects.
        /// </summary>
        public void CalculateEndPositions() {
            foreach (var ho in HitObjects) {
                ho.CalculateEndPosition();
            }
        }

        /// <summary>
        /// Actual osu! stable code for calculating stacked positions of all hit objects.
        /// Make sure slider end positions are calculated before using this procedure.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        internal void UpdateStacking(int startIndex = 0, int endIndex = -1, bool rounded = false) {
            if (endIndex == -1)
                endIndex = HitObjects.Count - 1;

            // Getting some variables for use later
            double stackOffset = GetStackOffset(Difficulty["CircleSize"].DoubleValue);
            double stackLeniency = General["StackLeniency"].DoubleValue;
            double preEmpt = GetApproachTime(Difficulty["ApproachRate"].DoubleValue);

            // Round the stack offset so objects only get offset by integer values
            if (rounded) {
                stackOffset = Math.Round(stackOffset);
            }

            const int STACK_LENIENCE = 3;

            Vector2 stackVector = new Vector2(stackOffset, stackOffset);
            float stackThresold = (float) (preEmpt * stackLeniency);

            // Reset stacking inside the update range
            for (int i = startIndex; i <= endIndex; i++)
                HitObjects[i].StackCount = 0;

            // Extend the end index to include objects they are stacked on
            int extendedEndIndex = endIndex;
            for (int i = endIndex; i >= startIndex; i--) {
                int stackBaseIndex = i;
                for (int n = stackBaseIndex + 1; n < HitObjects.Count; n++) {
                    HitObject stackBaseObject = HitObjects[stackBaseIndex];
                    if (stackBaseObject.IsSpinner) break;

                    HitObject objectN = HitObjects[n];
                    if (objectN.IsSpinner) continue;

                    if (objectN.Time - stackBaseObject.EndTime > stackThresold)
                        //We are no longer within stacking range of the next object.
                        break;

                    if (Vector2.Distance(stackBaseObject.Pos, objectN.Pos) < STACK_LENIENCE ||
                        (stackBaseObject.IsSlider && Vector2.Distance(stackBaseObject.EndPos, objectN.Pos) < STACK_LENIENCE)) {
                        stackBaseIndex = n;

                        // HitObjects after the specified update range haven't been reset yet
                        objectN.StackCount = 0;
                    }
                }

                if (stackBaseIndex > extendedEndIndex) {
                    extendedEndIndex = stackBaseIndex;
                    if (extendedEndIndex == HitObjects.Count - 1)
                        break;
                }
            }

            //Reverse pass for stack calculation.
            int extendedStartIndex = startIndex;
            for (int i = extendedEndIndex; i > startIndex; i--) {
                int n = i;
                /* We should check every note which has not yet got a stack.
                    * Consider the case we have two interwound stacks and this will make sense.
                    *
                    * o <-1      o <-2
                    *  o <-3      o <-4
                    *
                    * We first process starting from 4 and handle 2,
                    * then we come backwards on the i loop iteration until we reach 3 and handle 1.
                    * 2 and 1 will be ignored in the i loop because they already have a stack value.
                    */

                HitObject objectI = HitObjects[i];

                if (objectI.StackCount != 0 || objectI.IsSpinner) continue;

                /* If this object is a hitcircle, then we enter this "special" case.
                    * It either ends with a stack of hitcircles only, or a stack of hitcircles that are underneath a slider.
                    * Any other case is handled by the "is Slider" code below this.
                    */
                if (objectI.IsCircle) {
                    while (--n >= 0) {
                        HitObject objectN = HitObjects[n];

                        if (objectN.IsSpinner) continue;

                        if (objectI.Time - objectN.EndTime > stackThresold)
                            //We are no longer within stacking range of the previous object.
                            break;

                        // HitObjects before the specified update range haven't been reset yet
                        if (n < extendedStartIndex) {
                            objectN.StackCount = 0;
                            extendedStartIndex = n;
                        }

                        /* This is a special case where hticircles are moved DOWN and RIGHT (negative stacking) if they are under the *last* slider in a stacked pattern.
                            *    o==o <- slider is at original location
                            *        o <- hitCircle has stack of -1
                            *         o <- hitCircle has stack of -2
                            */
                        if (objectN.IsSlider && Vector2.Distance(objectN.EndPos, objectI.Pos) < STACK_LENIENCE) {
                            int offset = objectI.StackCount - objectN.StackCount + 1;
                            for (int j = n + 1; j <= i; j++) {
                                //For each object which was declared under this slider, we will offset it to appear *below* the slider end (rather than above).
                                if (Vector2.Distance(objectN.EndPos, HitObjects[j].Pos) < STACK_LENIENCE)
                                    HitObjects[j].StackCount -= offset;
                            }

                            //We have hit a slider.  We should restart calculation using this as the new base.
                            //Breaking here will mean that the slider still has StackCount of 0, so will be handled in the i-outer-loop.
                            break;
                        }

                        if (Vector2.Distance(objectN.Pos, objectI.Pos) < STACK_LENIENCE) {
                            //Keep processing as if there are no sliders.  If we come across a slider, this gets cancelled out.
                            //NOTE: Sliders with start positions stacking are a special case that is also handled here.

                            objectN.StackCount = objectI.StackCount + 1;
                            objectI = objectN;
                        }
                    }
                } else if (objectI.IsSlider) {
                    /* We have hit the first slider in a possible stack.
                        * From this point on, we ALWAYS stack positive regardless.
                        */
                    while (--n >= startIndex) {
                        HitObject objectN = HitObjects[n];

                        if (objectN.IsSpinner) continue;

                        if (objectI.Time - objectN.Time > stackThresold)
                            //We are no longer within stacking range of the previous object.
                            break;

                        if (Vector2.Distance(objectN.EndPos, objectI.Pos) < STACK_LENIENCE) {
                            objectN.StackCount = objectI.StackCount + 1;
                            objectI = objectN;
                        }
                    }
                }
            }

            for (int i = startIndex; i <= endIndex; i++) {
                HitObject currHitObject = HitObjects[i];
                currHitObject.StackedPos = currHitObject.Pos - currHitObject.StackCount * stackVector;
                currHitObject.StackedEndPos = currHitObject.EndPos - currHitObject.StackCount * stackVector;
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
                    var colourIncrement = hitObject.IsSpinner ? hitObject.ComboSkip : hitObject.ComboSkip + 1;

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

        /// <summary>
        /// Adjusts combo skip for all the hitobjects so colour index is correct.
        /// </summary>
        public void FixComboSkip() {
            HitObject previousHitObject = null;
            int colourIndex = 0;

            // If there are no combo colours use the default combo colours so the hitobjects still have something
            var actingComboColours = ComboColours.Count == 0 ? ComboColour.GetDefaultComboColours() : ComboColours.ToArray();

            foreach (var hitObject in HitObjects) {
                bool newCombo = IsNewCombo(hitObject, previousHitObject);

                if (newCombo) {
                    int colourIncrement = hitObject.IsSpinner ? 0 : 1;
                    var newColourIndex = MathHelper.Mod(colourIndex + colourIncrement, actingComboColours.Length);
                    var wantedColourIndex = hitObject.ColourIndex;
                    var diff = wantedColourIndex - newColourIndex;

                    if (diff > 0) {
                        hitObject.ComboSkip = diff;
                    } else if (diff < 0) {
                        hitObject.ComboSkip = (actingComboColours.Length + diff);
                    }

                    int newColourIncrement = hitObject.IsSpinner ? hitObject.ComboSkip : hitObject.ComboSkip + 1;
                    colourIndex = MathHelper.Mod(colourIndex + newColourIncrement, actingComboColours.Length);
                }

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
                ho.BodyHitsounds = BeatmapTiming.GetTimingPointsInRange(ho.Time, ho.EndTime, false);
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
        public static double GetApproachTime(double approachRate) {
            if (approachRate < 5) {
                return 1800 - 120 * approachRate;
            }

            return 1200 - 150 * (approachRate - 5);
        }

        /// <summary>
        /// Calculates the radius of a hit circle from a given Circle Size difficulty.
        /// </summary>
        /// <param name="circleSize"></param>
        /// <returns></returns>
        public static double GetHitObjectRadius(double circleSize) {
            return (109 - 9 * circleSize) / 2;
        }

        public static double GetStackOffset(double circleSize) {
            return GetHitObjectRadius(circleSize) / 10;
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
            FileFormatHelper.AddDictionaryToLines(General, lines);
            lines.Add("");
            lines.Add("[Editor]");
            FileFormatHelper.AddDictionaryToLines(Editor, lines);
            lines.Add("");
            lines.Add("[Metadata]");
            FileFormatHelper.AddDictionaryToLines(Metadata, lines);
            lines.Add("");
            lines.Add("[Difficulty]");
            FileFormatHelper.AddDictionaryToLines(Difficulty, lines);
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
                lines.AddRange(ComboColours.Select((t, i) => "Combo" + (i + 1) + " : " + t));
                lines.AddRange(SpecialColours.Select(specialColour => specialColour.Key + " : " + specialColour.Value));
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
            BeatmapTiming.Offset(offset);
            HitObjects?.ForEach(h => h.MoveTime(offset));
        }

        private IEnumerable<Event> EnumerateAllEvents() {
            return BackgroundAndVideoEvents.Concat(BreakPeriods).Concat(StoryboardSoundSamples)
                .Concat(StoryboardLayerFail).Concat(StoryboardLayerPass).Concat(StoryboardLayerBackground)
                .Concat(StoryboardLayerForeground).Concat(StoryboardLayerOverlay);
        }

        public double GetLeadInTime() {
            var leadInTime = General["AudioLeadIn"].DoubleValue;
            var od = Difficulty["OverallDifficulty"].DoubleValue;
            var window50 = Math.Ceiling(200 - 10 * od);
            var eventsWithStartTime = EnumerateAllEvents().OfType<IHasStartTime>().ToArray();
            if (eventsWithStartTime.Length > 0)
                leadInTime = Math.Max(-eventsWithStartTime.Min(o => o.StartTime), leadInTime);
            if (HitObjects.Count > 0) {
                var approachTime = GetApproachTime(Difficulty["ApproachRate"].DoubleValue);
                leadInTime = Math.Max(approachTime - HitObjects[0].Time, leadInTime);
            }
            return leadInTime + window50 + 1000;
        }

        public double GetMapStartTime() {
            return -GetLeadInTime();
        }

        public double GetMapEndTime() {
            var endTime = HitObjects.Count > 0
                ? Math.Max(GetHitObjectEndTime() + 200, HitObjects.Last().EndTime + 3000)
                : double.NegativeInfinity;
            var eventsWithEndTime = EnumerateAllEvents().OfType<IHasEndTime>().ToArray();
            if (eventsWithEndTime.Length > 0)
                endTime = Math.Max(endTime, eventsWithEndTime.Max(o => o.EndTime) - 500);
            return endTime;
        }

        /// <summary>
        /// Gets the time at which auto-fail gets checked by osu!
        /// The counted judgements must add up to the object count at this time.
        /// </summary>
        /// <returns></returns>
        public double GetAutoFailCheckTime() {
            return GetHitObjectEndTime() + 200;
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
            var time = TypeConverters.ParseOsuTimestamp(code).TotalMilliseconds;

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

        public Beatmap DeepCopy() {
            var newBeatmap = (Beatmap)MemberwiseClone();
            newBeatmap.HitObjects = HitObjects?.Select(h => h.DeepCopy()).ToList();
            newBeatmap.BeatmapTiming = new Timing(BeatmapTiming.TimingPoints.Select(t => t.Copy()).ToList(), BeatmapTiming.SliderMultiplier);
            newBeatmap.GiveObjectsGreenlines();
            return newBeatmap;
        }
    }
}
