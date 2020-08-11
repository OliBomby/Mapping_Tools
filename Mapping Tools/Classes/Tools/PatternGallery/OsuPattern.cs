using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    /// <summary>
    /// Must store the objects, the greenlines, the timing, the global SV, the tickrate, the difficulty settings,
    /// the hitsounds, absolute times and positions, combocolour index, combo numbers, stack leniency, gamemode.
    /// Also store additional metadata such as the name, the date it was saved, use count, the map title, artist, diffname, and mapper.
    /// </summary>
    public class OsuPattern : BindableBase {
        #region Fields

        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        private string _name;
        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }

        private DateTime _saveDateTime;
        public DateTime SaveDateTime {
            get => _saveDateTime;
            set => Set(ref _saveDateTime, value);
        }

        private int _useCount;
        public int UseCount {
            get => _useCount;
            set => Set(ref _useCount, value);
        }

        private string _title;
        public string Title {
            get => _title;
            set => Set(ref _title, value);
        }

        private string _artist;
        public string Artist {
            get => _artist;
            set => Set(ref _artist, value);
        }

        private string _creator;
        public string Creator {
            get => _creator;
            set => Set(ref _creator, value);
        }

        private string _version;
        public string Version {
            get => _version;
            set => Set(ref _version, value);
        }

        private List<HitObject> _hitObjects;
        public List<HitObject> HitObjects {
            get => _hitObjects;
            set => Set(ref _hitObjects, value);
        }

        private List<TimingPoint> _timingPoints;
        public List<TimingPoint> TimingPoints {
            get => _timingPoints;
            set => Set(ref _timingPoints, value);
        }

        private TimingPoint _firstUnInheritedTimingPoint;
        public TimingPoint FirstUnInheritedTimingPoint {
            get => _firstUnInheritedTimingPoint;
            set => Set(ref _firstUnInheritedTimingPoint, value);
        }

        private double _sliderMultiplier;
        public double SliderMultiplier {
            get => _sliderMultiplier;
            set => Set(ref _sliderMultiplier, value);
        }

        private double _sliderTickRate;
        public double SliderTickRate {
            get => _sliderTickRate;
            set => Set(ref _sliderTickRate, value);
        }

        private double _hpDrainRate;
        public double HpDrainRate {
            get => _hpDrainRate;
            set => Set(ref _hpDrainRate, value);
        }

        private double _circleSize;
        public double CircleSize {
            get => _circleSize;
            set => Set(ref _circleSize, value);
        }

        private double _overallDifficulty;
        public double OverallDifficulty {
            get => _overallDifficulty;
            set => Set(ref _overallDifficulty, value);
        }

        private double _approachRate;
        public double ApproachRate {
            get => _approachRate;
            set => Set(ref _approachRate, value);
        }

        private GameMode _gameMode;
        public GameMode GameMode {
            get => _gameMode;
            set => Set(ref _gameMode, value);
        }

        private SampleSet _defaultSampleSet;
        public SampleSet DefaultSampleSet {
            get => _defaultSampleSet;
            set => Set(ref _defaultSampleSet, value);
        }

        private double _stackLeniency;
        public double StackLeniency {
            get => _stackLeniency;
            set => Set(ref _stackLeniency, value);
        }

        #endregion

        public double GetHitObjectStartTime() {
            return HitObjects.Min(h => h.Time);
        }

        public double GetHitObjectEndTime() {
            return HitObjects.Max(h => h.EndTime);
        }

        public Timing GetTiming() {
            var timingPoints = new List<TimingPoint>(TimingPoints);
            if (!TimingPoints.Contains(FirstUnInheritedTimingPoint))
                timingPoints.Add(FirstUnInheritedTimingPoint);

            return new Timing(timingPoints, SliderMultiplier);
        }

        public void Offset(double offset) {
            // I hope it doesnt offset the FirstUnInheritedTimingPoint twice
            if (FirstUnInheritedTimingPoint != null && !TimingPoints.Contains(FirstUnInheritedTimingPoint))
                FirstUnInheritedTimingPoint.Offset += offset;
            TimingPoints?.ForEach(tp => tp.Offset += offset);
            HitObjects?.ForEach(h => h.MoveTime(offset));
        }

        public void GiveObjectsGreenlines() {
            var beatmapTiming = GetTiming();
            foreach (var ho in HitObjects) {
                ho.SliderVelocity = beatmapTiming.GetSvAtTime(ho.Time);
                ho.TimingPoint = beatmapTiming.GetTimingPointAtTime(ho.Time);
                ho.HitsoundTimingPoint = beatmapTiming.GetTimingPointAtTime(ho.Time + 5);
                ho.UnInheritedTimingPoint = beatmapTiming.GetRedlineAtTime(ho.Time);
                ho.BodyHitsounds = beatmapTiming.GetTimingPointsInTimeRange(ho.Time, ho.EndTime);
                // Remove all body hitsound timingpoints at slider repeats
                foreach (var time in ho.GetAllTloTimes(beatmapTiming)) {
                    ho.BodyHitsounds.RemoveAll(o => Math.Abs(time - o.Offset) <= 5);
                }
            }
        }

        public OsuPattern DeepCopy() {
            var newPattern = (OsuPattern) MemberwiseClone();
            newPattern.HitObjects = HitObjects?.Select(h => h.DeepCopy()).ToList();
            newPattern.TimingPoints = TimingPoints?.Select(t => t.Copy()).ToList();
            newPattern.FirstUnInheritedTimingPoint = FirstUnInheritedTimingPoint?.Copy();
            newPattern.GiveObjectsGreenlines();
            return newPattern;
        }
    }
}