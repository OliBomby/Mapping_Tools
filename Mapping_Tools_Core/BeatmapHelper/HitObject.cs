using JetBrains.Annotations;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.BeatmapHelper {
    /// <summary>
    /// Base class for a hit object in osu! stable.
    /// Has extra fields for easier editing and analysis.
    /// </summary>
    public abstract class HitObject : IComparable<HitObject>, IHasPosition, IHasStartTime {
        /// <summary>
        /// Selected hit object.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Base position of hit object.
        /// </summary>
        public Vector2 Pos { get; set; }

        /// <summary>
        /// Stacked position of hit object. Must be computed by beatmap.
        /// </summary>
        public Vector2 StackedPos { get; set; }

        /// <summary>
        /// Absolute time of the hit object in milliseconds.
        /// </summary>
        public double StartTime { get; set; }

        /// <summary>
        /// Hit object forces a new combo.
        /// </summary>
        public bool NewCombo { get; set; }

        /// <summary>
        /// Extra combo skip for combo colours.
        /// By default a new combo adds one to the colour index.
        /// This value adds additional increments to the colour index each new combo.
        /// </summary>
        public int ComboSkip { get; set; }

        /// <summary>
        /// Whether to play the hitnormal hitsound.
        /// In the osu! std gamemode this gets played regardless.
        /// </summary>
        public bool Normal { get; set; }

        /// <summary>
        /// Wether to play the hitwhistle hitsound.
        /// </summary>
        public bool Whistle { get; set; }

        /// <summary>
        /// Wether to play the hitfinish hitsound.
        /// </summary>
        public bool Finish { get; set; }

        /// <summary>
        /// Wether to play the hitclap hitsound.
        /// </summary>
        public bool Clap { get; set; }

        /// <summary>
        /// The sample set to use for the hitnormal, sliderslider, and slidertick hitsounds.
        /// </summary>
        public SampleSet SampleSet { get; set; }

        /// <summary>
        /// The sample set to use for the hitwhistle, hitfinish, and hitclap hitsounds.
        /// </summary>
        public SampleSet AdditionSet { get; set; }

        /// <summary>
        /// The index for custom hitsound sample sets.
        /// Index zero indicates no custom samples are used.
        /// </summary>
        public int CustomIndex { get; set; }

        /// <summary>
        /// The hitsound volume of this hit object.
        /// </summary>
        public double SampleVolume { get; set; }

        /// <summary>
        /// The filename of the custom sample to play as the hitsound of this hit object.
        /// If a valid filename is used, the file will override all other hitsounds.
        /// </summary>
        public string Filename { get; set; }

        // Special combined with beatmap
        /// <summary>
        /// The stack count indicates the number of hit objects that this object is stacked upon.
        /// Used for calculating stack offset.
        /// </summary>
        public int StackCount { get; set; }

        /// <summary>
        /// Whether a new combo starts on this hit object.
        /// </summary>
        public bool ActualNewCombo { get; set; }

        /// <summary>
        /// The combo number of this hit object.
        /// </summary>
        public int ComboIndex { get; set; }

        /// <summary>
        /// The colour index of the hit object.
        /// Determines which combo colour of the beatmap to use.
        /// </summary>
        public int ColourIndex { get; set; }

        /// <summary>
        /// The colour of this hit object.
        /// </summary>
        public IComboColour Colour { get; set; }

        // Special combined with greenline
        /// <summary>
        /// The greenline slider velocity at the start time of this hit object.
        /// 100x inverse of actual slider velocity multiplier. TODO yeet this 100x inverse BS
        /// </summary>
        public double SliderVelocity { get; set; }

        /// <summary>
        /// The timing point active at the start time of this hit object.
        /// Usefull for determining slider velocity.
        /// </summary>
        [CanBeNull]
        public TimingPoint TimingPoint { get; set; }

        /// <summary>
        /// The timing point active 5 milliseconds after the start time of this hit object.
        /// This is the timing point determining the hitsounds for this hit object at the start time.
        /// </summary>
        [CanBeNull]
        public TimingPoint HitsoundTimingPoint { get; set; }

        /// <summary>
        /// The uninherited timing point active at the start time of this hit object.
        /// Determines the BPM at the start time of this hit object.
        /// </summary>
        [CanBeNull]
        public TimingPoint UnInheritedTimingPoint { get; set; }

        // Special combined with timeline
        /// <summary>
        /// Indicates the number of timeline objects this object should have.
        /// </summary>
        public abstract int TloCount { get; }

        /// <summary>
        /// The timeline objects associated with this hit object.
        /// </summary>
        [NotNull]
        public List<TimelineObject> TimelineObjects { get; set; }

        protected HitObject() {
            TimelineObjects = new List<TimelineObject>();
        }

        /// <summary>
        /// Gets all times of timeline objects of this object.
        /// </summary>
        /// <param name="timing">The timing to align the timeline object times to.</param>
        /// <returns>List of all timeline object times.</returns>
        public abstract List<double> GetAllTloTimes(Timing timing);

        /// <summary>
        /// Removes all hitounds and sets samplesets to auto.
        /// Also clears hitsounds from timeline objects and clears body hitsounds.
        /// </summary>
        public virtual void ResetHitsounds() {
            Normal = false;
            Whistle = false;
            Finish = false;
            Clap = false;
            SampleSet = SampleSet.Auto;
            AdditionSet = SampleSet.Auto;
            SampleVolume = 0;
            CustomIndex = 0;
            Filename = string.Empty;

            foreach (var tlo in TimelineObjects) {
                tlo.ResetHitsounds();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="deltaTime"></param>
        public virtual void MoveTime(double deltaTime) {
            StartTime += deltaTime;

            // Move its timelineobjects
            foreach (var tlo in TimelineObjects) tlo.Time += deltaTime;
        }

        /// <summary>
        /// Update the associated timeline object with new time information.
        /// </summary>
        public virtual void UpdateTimelineObjectTimes() {
            if (this is IHasRepeatDuration hasRepeatDuration) {
                for (int i = 0; i < TimelineObjects.Count; i++) {
                    double time = Math.Floor(StartTime + hasRepeatDuration.RepeatDuration * i);
                    TimelineObjects[i].Time = time;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="delta"></param>
        public virtual void Move(Vector2 delta) {
            Pos += delta;
        }

        /// <summary>
        /// Apply a 2x2 transformation matrix to the hit object position.
        /// </summary>
        /// <param name="mat">The transformation matrix to apply to the position</param>
        public virtual void Transform(Matrix2 mat) {
            Pos = Matrix2.Mult(mat, Pos);
        }

        public override string ToString() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Makes a deep clone of this hit object and returns it.
        /// </summary>
        /// <returns>The deep clone of this hit object.</returns>
        public HitObject DeepClone() {
            var newHitObject = (HitObject) MemberwiseClone();
            newHitObject.TimelineObjects = TimelineObjects.Select(o => o.Copy()).ToList();
            newHitObject.TimingPoint = TimingPoint?.Copy();
            newHitObject.HitsoundTimingPoint = HitsoundTimingPoint?.Copy();
            newHitObject.UnInheritedTimingPoint = UnInheritedTimingPoint?.Copy();
            newHitObject.Colour = (IComboColour) Colour?.Clone();

            // Deep clone for the types inheriting HitObject
            DeepCloneAdd(newHitObject);

            return newHitObject;
        }

        protected virtual void DeepCloneAdd(HitObject clonedHitObject) { }

        public int CompareTo(HitObject other) {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            if (StartTime == other.StartTime) return other.NewCombo.CompareTo(NewCombo);
            return StartTime.CompareTo(other.StartTime);
        }
    }
}