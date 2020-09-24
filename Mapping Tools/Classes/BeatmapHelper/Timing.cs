using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.HitsoundStuff;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// The timing of a beatmap. This objects contains all the timing points (data from the [TimingPoints] section) plus the global slider multiplier.
    /// This also has a number of helper methods to fetch data from the timing points.
    /// With this object you can always calculate the slider velocity at any time.
    /// <see cref="Beatmap"/> objects use this object to store all timing data.
    /// </summary>
    public class Timing : IList<TimingPoint> {
        /// <summary>
        /// List of all timing points. This included uninherited timing points and inherited timing points.
        /// This list should be sorted at all times.
        /// </summary>
        private List<TimingPoint> _timingPoints { get; set; }
        private List<TimingPoint> _redlines { get; set; }
        private List<TimingPoint> _greenlines { get; set; }

        public IReadOnlyList<TimingPoint> TimingPoints => _timingPoints;
        public IReadOnlyList<TimingPoint> Redlines => _redlines;
        public IReadOnlyList<TimingPoint> Greenlines => _greenlines;

        /// <summary>
        /// The global slider multiplier of a <see cref="Beatmap"/>. This is here for convenience sake to calculate absolute slider velocities.
        /// </summary>
        public double SliderMultiplier { get; set; }

        public Timing(double sliderMultiplier) {
            SetTimingPoints(null);
            SliderMultiplier = sliderMultiplier;
        }

        /// <inheritdoc />
        public Timing(List<TimingPoint> timingPoints, double sliderMultiplier) {
            SetTimingPoints(timingPoints);
            SliderMultiplier = sliderMultiplier;
        }

        /// <inheritdoc />
        public Timing(IEnumerable<string> timingLines, double sliderMultiplier) {
            SetTimingPoints(GetTimingPoints(timingLines).ToList());
            SliderMultiplier = sliderMultiplier;
        }

        /// <summary>
        /// Replaces all the timingpoints and sorts again.
        /// </summary>
        /// <param name="timingPoints"></param>
        public void SetTimingPoints(List<TimingPoint> timingPoints) {
            _timingPoints = timingPoints ?? new List<TimingPoint>();
            _timingPoints.Sort();
            _redlines = _timingPoints.Where(tp => tp.Uninherited).ToList();
            _greenlines = _timingPoints.Where(tp => !tp.Uninherited).ToList();
        }

        /// <summary>
        /// Sorts all <see cref="TimingPoint"/> in order of time.
        /// </summary>
        public void Sort() {
            _timingPoints.Sort();
            _redlines.Sort();
            _greenlines.Sort();
        }

        #region BasicOperations

        public void Add(TimingPoint tp) {
            if (tp == null) return;

            var index = _timingPoints.BinarySearch(tp);
            if (index < 0)
                index = ~index;

            _timingPoints.Insert(index, tp);

            if (tp.Uninherited) {
                index = _redlines.BinarySearch(tp);
                if (index < 0)
                    index = ~index;

                _redlines.Insert(index, tp);
            } else {
                index = _greenlines.BinarySearch(tp);
                if (index < 0)
                    index = ~index;

                _greenlines.Insert(index, tp);
            }
        }

        public bool Remove(TimingPoint tp) {
            var index = _timingPoints.BinarySearch(tp);
            if (index >= 0) {
                _timingPoints.RemoveAt(index);
            }

            if (tp.Uninherited) {
                index = _redlines.BinarySearch(tp);
                if (index >= 0) {
                    _redlines.RemoveAt(index);
                    return true;
                }
            } else {
                index = _greenlines.BinarySearch(tp);
                if (index >= 0) {
                    _greenlines.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(TimingPoint[] array, int arrayIndex) {
            _timingPoints.CopyTo(array, arrayIndex);
        }

        bool ICollection<TimingPoint>.Remove(TimingPoint tp) {
            return tp != null && Remove(tp);
        }

        public int Count => _timingPoints.Count;
        public bool IsReadOnly => false;

        public void Clear() {
            _timingPoints.Clear();
            _redlines.Clear();
            _greenlines.Clear();
        }

        public bool Contains(TimingPoint item) {
            return _timingPoints.Contains(item);
        }

        public void Offset(double offset) {
            _timingPoints.ForEach(tp => tp.Offset += offset);
        }

        public int RemoveAll(Func<TimingPoint, bool> match) {
            var itemsToRemove = _timingPoints.Where(match).ToList();

            foreach (var itemToRemove in itemsToRemove) {
                Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        public IEnumerator<TimingPoint> GetEnumerator() {
            return _timingPoints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public int IndexOf(TimingPoint item) {
            return _timingPoints.IndexOf(item);
        }

        /// <summary>
        /// Ignores index so it remains sorted.
        /// </summary>
        public void Insert(int index, TimingPoint item) {
            Add(item);
        }

        public void RemoveAt(int index) {
            var itemToRemove = _timingPoints[index];
            Remove(itemToRemove);
        }

        public TimingPoint this[int index] {
            get => _timingPoints[index];
            set => _timingPoints[index] = value;
        }

        #endregion

        /// <summary>
        /// Calculates the number of beats between the start time and the end time.
        /// The resulting number of beats will be rounded to a 1/16 or 1/12 beat divisor.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="round">To round the number of beats to a snap divisor.</param>
        /// <returns></returns>
        public double GetBeatLength(double startTime, double endTime, bool round = false) {
            var redlines = GetRedlinesInRange(startTime, endTime, false);

            double beats = 0;
            double lastTime = startTime;
            var lastRedline = GetRedlineAtTime(startTime);
            foreach (var redline in redlines) {
                var inc1 = (redline.Offset - lastTime) / lastRedline.MpB;
                beats += round ? MultiSnapRound(inc1, 16, 12) : inc1;

                lastTime = redline.Offset;
                lastRedline = redline;
            }
            var inc2 = (endTime - lastTime) / lastRedline.MpB;
            beats += round ? MultiSnapRound(inc2, 16, 12) : inc2;

            return beats;
        }

        private static double MultiSnapRound(double value, double divisor1, double divisor2) {
            var round1 = Math.Round(value * divisor1) / divisor1;
            var round2 = Math.Round(value * divisor2) / divisor2;
            return Math.Abs(round1 - value) < Math.Abs(round2 - value) ? round1 : round2;
        }

        /// <summary>
        /// This method calculates time of the tick on the timeline which is nearest to specified time.
        /// This method is mostly used to snap objects to timing.
        /// </summary>
        /// <param name="time">Specified time.</param>
        /// <param name="tp">Uninherited timing point to get the timing from.</param>
        /// <param name="divisor">How many timeline ticks to have per beat.</param>
        /// <returns></returns>
        public static double GetNearestTimeMeter(double time, TimingPoint tp, int divisor) {
            double d = tp.MpB / divisor;
            double remainder = ( time - tp.Offset ) % d;
            if( remainder < 0.5 * d ) {
                return time - remainder;
            }

            return time - remainder + d;
        }

        /// <summary>
        /// Calculates the nearest value to <see cref="duration"/> which is also a multiple of <see cref="divisor"/>.
        /// </summary>
        /// <param name="duration">The target value.</param>
        /// <param name="divisor">The value it has to be a multiple of.</param>
        /// <returns></returns>
        public static double GetNearestMultiple(double duration, double divisor) {
            double remainder = duration % divisor;

            if (remainder < 0.5 * divisor) {
                return duration - remainder;
            }

            return duration - remainder + divisor;
        }

        /// <summary>
        /// Calculates the snapped time for a given time and multiple different options.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="divisor2">The first beat snap divisor.</param>
        /// <param name="divisor3">The second beat snap divisor.</param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        /// You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <returns>The snapped time.</returns>
        public double Resnap(double time, int divisor2, int divisor3, bool floor=true, TimingPoint tp=null, TimingPoint firstTp=null) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime2 = GetNearestTimeMeter(time, beforeTp, divisor2);
            double snapDistance2 = Math.Abs(time - newTime2);

            double newTime3 = GetNearestTimeMeter(time, beforeTp, divisor3);
            double snapDistance3 = Math.Abs(time - newTime3);

            double newTime = snapDistance3 < snapDistance2 ? newTime3 : newTime2;

            if( afterTp != null && newTime > beforeTp.Offset + 10 && newTime >= afterTp.Offset - 10 ) {
                newTime = afterTp.Offset;
            }
            return floor ? Math.Floor(newTime) : newTime;
        }

        /// <summary>
        /// Calculates the snapped time for a given time and makes sure the snapped time is not outside the time range of a hit object.
        /// This can be used to resnap stuff that has to be within the time range of a slider. For example volume changes inside a slider body.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="divisor2">The first beat snap divisor.</param>
        /// <param name="divisor3">The second beat snap divisor.</param>
        /// <param name="ho">The hit object with a time range that the specified time has to stay inside.</param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        /// You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <returns>The snapped time.</returns>
        public double ResnapInRange(double time, int divisor2, int divisor3, HitObject ho, bool floor=true, TimingPoint tp=null, TimingPoint firstTp=null) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime2 = GetNearestTimeMeter(time, beforeTp, divisor2);
            double snapDistance2 = Math.Abs(time - newTime2);

            double newTime3 = GetNearestTimeMeter(time, beforeTp, divisor3);
            double snapDistance3 = Math.Abs(time - newTime3);

            double newTime = snapDistance3 < snapDistance2 ? newTime3 : newTime2;

            if ( afterTp != null && Precision.DefinitelyBigger(newTime, afterTp.Offset) ) {
                newTime = afterTp.Offset;
            }

            if( newTime <= ho.Time + 1 || newTime >= ho.EndTime - 1 ) // Don't resnap if it would move outside
            {
                newTime = time;
            }

            return floor ? Math.Floor(newTime) : newTime;
        }

        /// <summary>
        /// Finds the timing point which is in effect at a given time with a custom set of timing points.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints">All the timing points.</param>
        /// <param name="firstTimingpoint">The first timing point to start searching from.</param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAtTime(double time, IReadOnlyList<TimingPoint> timingPoints, TimingPoint firstTimingpoint) {
            var index = BinarySearchUtil.BinarySearch(timingPoints, time, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Rightmost);
            if (index < 0) {
                index = ~index;
                return index == 0 ? firstTimingpoint : timingPoints[index - 1];
            }

            return timingPoints[index];
        }

        /// <summary>
        /// Gets the first timing point after specified time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints"></param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAfterTime(double time, IReadOnlyList<TimingPoint> timingPoints) {
            var index = BinarySearchUtil.BinarySearch(timingPoints, time, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Rightmost);
            if (index < 0) {
                index = ~index;

                return index < timingPoints.Count ? timingPoints[index] : null;
            }

            return index + 1 < timingPoints.Count ? timingPoints[index + 1] : null;
        }

        public static List<TimingPoint> GetTimingPointsInRange(double startTime, double endTime,
            List<TimingPoint> timingPoints, bool inclusive = true) {
            if (!inclusive) {
                startTime += Precision.DOUBLE_EPSILON;
                endTime -= Precision.DOUBLE_EPSILON;
            } else {
                startTime -= Precision.DOUBLE_EPSILON;
                endTime += Precision.DOUBLE_EPSILON;
            }

            var startIndex = BinarySearchUtil.BinarySearch(timingPoints, startTime, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Leftmost);
            if (startIndex < 0)
                startIndex = ~startIndex;

            var endIndex = BinarySearchUtil.BinarySearch(timingPoints, endTime, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Rightmost);
            if (endIndex < 0)
                endIndex = ~endIndex - 1;

            return timingPoints.GetRange(startIndex, Math.Max(endIndex - startIndex + 1, 0));
        }

        /// <summary>
        /// Finds the timing point which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetTimingPointAtTime(double time) {
            return GetTimingPointAtTime(time, _timingPoints, GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds all the timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="inclusive"></param>
        /// <returns></returns>
        public List<TimingPoint> GetTimingPointsInRange(double startTime, double endTime, bool inclusive = true) {
            return GetTimingPointsInRange(startTime, endTime, _timingPoints, inclusive);
        }

        /// <summary>
        /// Finds all the uninherited timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="inclusive"></param>
        /// <returns></returns>
        public List<TimingPoint> GetRedlinesInRange(double startTime, double endTime, bool inclusive = true) {
            return GetTimingPointsInRange(startTime, endTime, _redlines, inclusive);
        }

        /// <summary>
        /// Gets the BPM at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetBpmAtTime(double time) {
            return 60000 / GetMpBAtTime(time);
        }

        /// <summary>
        /// Gets the milliseconds per beat at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetMpBAtTime(double time) {
            return GetRedlineAtTime(time).MpB;
        }

        /// <summary>
        /// Finds the inherited <see cref="TimingPoint"/> which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetGreenlineAtTime(double time) {
            return GetTimingPointAtTime(time, _greenlines, GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds the uninherited <see cref="TimingPoint"/> which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="firstTimingPoint"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAtTime(double time, TimingPoint firstTimingPoint=null) {
            return GetTimingPointAtTime(time, _redlines, firstTimingPoint ?? GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds the nearest uninherited timing point which starts after a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAfterTime(double time) {
            return GetTimingPointAfterTime(time, _redlines);
        }

        /// <summary>
        /// Gets the slider velocity multiplier at a given time.
        /// Its that number on inherited timing points that ranges from 0.1 to 10.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetSvMultiplierAtTime(double time) {
            return -100 / GetSvAtTime(time);
        }

        /// <summary>
        /// Gets the slider velocity at a given time.
        /// This gives the value from the .osu.
        /// Ranges from -1000 to -10.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetSvAtTime(double time) {
            var lastTp = GetTimingPointAtTime(time, _timingPoints, null);
            if (lastTp == null || lastTp.Uninherited) {
                return -100;
            }

            return MathHelper.Clamp(lastTp.MpB, -1000, -10);
        }

        /// <summary>
        /// Calculates the size of the effective time range of a given timing point.
        /// This range stops at the next timing point, so it just returns the offset of the next timing point.
        /// </summary>
        /// <param name="timingPoint"></param>
        /// <returns>The timing point after specified timing point.</returns>
        public double GetTimingPointEffectiveRange(TimingPoint timingPoint) {
            var afterTp = GetTimingPointAfterTime(timingPoint.Offset, _timingPoints);
            return afterTp?.Offset ?? double.PositiveInfinity;
        }

        /// <summary>
        /// Calculates the duration of a slider using the slider velocity and milliseconds per beat at a given time, global multiplier and the pixel length.
        /// </summary>
        /// <param name="time">Time of slider.</param>
        /// <param name="length">Pixel length of slider.</param>
        /// <returns>The duration of the slider in milliseconds.</returns>
        public double CalculateSliderTemporalLength(double time, double length) {
            var sv = GetSvAtTime(time);
            return CalculateSliderTemporalLength(time, length, sv);
        }

        public double CalculateSliderTemporalLength(double time, double length, double sv) {
            return (length * GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv)) / (-10000 * SliderMultiplier);
        }

        /// <summary>
        /// Calculates the pixel length of a slider using the duration of the slider.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="temporalLength"></param>
        /// <returns></returns>
        public double CalculateSliderLength(double time, double temporalLength) {
            var sv = GetSvAtTime(time);
            return ( -10000 * temporalLength * SliderMultiplier ) / ( GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv) );
        }

        public double CalculateSliderLengthCustomSv(double time, double temporalLength, double sv) {
            return ( -10000 * temporalLength * SliderMultiplier ) / ( GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv) );
        }

        private static IEnumerable<TimingPoint> GetTimingPoints(IEnumerable<string> timingLines) {
            return timingLines.Select(line => new TimingPoint(line));
        }

        public TimingPoint GetFirstTimingPointExtended() {
            // Add an extra timingpoint that is the same as the first redline but like 10 x meter beats earlier so any objects before the first redline can use that thing

            // When you have a greenline before the first redline, the greenline will act like the first redline and you can snap objects to the greenline's bpm. 
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SliderVelocity slider will be 600 bpm.
            // The timeline will work like a redline on 0 offset and 1000 milliseconds per beat

            TimingPoint firstTp = _timingPoints.FirstOrDefault();
            if( firstTp != null && firstTp.Uninherited ) {
                return new TimingPoint(firstTp.Offset - firstTp.MpB * firstTp.Meter.TempoDenominator * 10, firstTp.MpB,
                                        firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume, firstTp.Uninherited, false, false);
            }

            if (firstTp != null)
                return new TimingPoint(0, 1000, firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume,
                    firstTp.Uninherited, false, false);

            return new TimingPoint(0, 0, 0, SampleSet.Auto, 0, 0, true, false, false);
        }
    }
}
