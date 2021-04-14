using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper.TimingStuff {
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
        public double GlobalSliderMultiplier { get; set; }

        public Timing(double globalSliderMultiplier) {
            SetTimingPoints(null);
            GlobalSliderMultiplier = globalSliderMultiplier;
        }

        public Timing(List<TimingPoint> timingPoints, double globalSliderMultiplier) {
            SetTimingPoints(timingPoints);
            GlobalSliderMultiplier = globalSliderMultiplier;
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

        public void AddRange(IEnumerable<TimingPoint> timingPoints) {
            foreach (var timingPoint in timingPoints) {
                Add(timingPoint);
            }
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

        public Timing Copy() {
            return new Timing(_timingPoints.Select(o => o.Copy()).ToList(), GlobalSliderMultiplier);
        }

        #endregion

        /// <summary>
        /// Calculates the number of beats between the start time and the end time.
        /// Optionally the resulting number of beats will be rounded to a set of beat divisors.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="round">To round the number of beats to a snap divisor.</param>
        /// <param name="divisors">The beat divisors to round to. If null, the default beat divisors will be used.</param>
        /// <returns></returns>
        public double GetBeatLength(double startTime, double endTime, bool round = false, IBeatDivisor[] divisors = null) {
            bool reverse = false;
            if (startTime > endTime) {
                var endTimeTemp = endTime;
                endTime = startTime;
                startTime = endTimeTemp;
                reverse = true;
            }

            var redlines = GetRedlinesInRange(startTime, endTime, false);
            divisors ??= RationalBeatDivisor.GetDefaultBeatDivisors();

            double beats = 0;
            double lastTime = startTime;
            var lastRedline = GetRedlineAtTime(startTime);
            foreach (var redline in redlines) {
                var inc1 = (redline.Offset - lastTime) / lastRedline.MpB;
                beats += round ? MultiSnapRound(inc1, divisors) : inc1;

                lastTime = redline.Offset;
                lastRedline = redline;
            }
            var inc2 = (endTime - lastTime) / lastRedline.MpB;
            beats += round ? MultiSnapRound(inc2, divisors) : inc2;

            return reverse ? -beats : beats;
        }

        private static double MultiSnapRound(double value, IBeatDivisor[] beatDivisors) {
            double minDiff = double.PositiveInfinity;
            double bestRound = value;

            foreach (var beatDivisor in beatDivisors) {
                var round = Math.Round(value / beatDivisor.GetValue()) * beatDivisor.GetValue();
                var diff = Math.Abs(round - value);

                if (diff < minDiff) {
                    minDiff = diff;
                    bestRound = round;
                }
            }

            return bestRound;
        }

        /// <summary>
        /// Assumes all the redlines are in beat timing and calculates the millisecond time for a beat time.
        /// 0 beatTime returns originTime.
        /// </summary>
        /// <param name="originTime"></param>
        /// <param name="beatTime"></param>
        /// <param name="round"></param>
        /// <param name="divisors"></param>
        /// <returns></returns>
        public double GetMilliseconds(double beatTime, double originTime = 0, bool round = false, IBeatDivisor[] divisors = null) {
            double ms = originTime;

            if (beatTime >= 0) {
                var redlines = GetRedlinesInRange(0, beatTime, false);
                TimingPoint lastRedline = GetRedlineAtTime(0);
                ms += round
                    ? MultiSnapRound(lastRedline.Offset, divisors) * lastRedline.MpB
                    : lastRedline.Offset * lastRedline.MpB;
                foreach (var redline in redlines) {
                    ms += round 
                        ? MultiSnapRound(redline.Offset - lastRedline.Offset, divisors) * lastRedline.MpB
                        : (redline.Offset - lastRedline.Offset) * lastRedline.MpB;

                    lastRedline = redline;
                }
                ms += round
                    ? MultiSnapRound(beatTime - lastRedline.Offset, divisors) * lastRedline.MpB
                    : (beatTime - lastRedline.Offset) * lastRedline.MpB;
            } else {
                var redlines = GetRedlinesInRange(beatTime, 0, false);
                TimingPoint lastRedline = GetRedlineAtTime(beatTime);
                ms += round
                    ? MultiSnapRound(beatTime - lastRedline.Offset, divisors) * lastRedline.MpB
                    : (beatTime - lastRedline.Offset) * lastRedline.MpB;
                foreach (var redline in redlines) {
                    ms -= round
                        ? MultiSnapRound(redline.Offset - lastRedline.Offset, divisors) * lastRedline.MpB
                        : (redline.Offset - lastRedline.Offset) * lastRedline.MpB;

                    lastRedline = redline;
                }
                ms += round
                    ? MultiSnapRound(lastRedline.Offset, divisors) * lastRedline.MpB
                    : lastRedline.Offset * lastRedline.MpB;
            }

            return ms;
        }

        /// <summary>
        /// Assumes all the redlines are in beat timing and calculates the beat time which is X milliseconds offset for a beat time.
        /// 0 beatTime returns originTime.
        /// </summary>
        /// <returns></returns>
        public double WalkMillisecondsInBeatTime(double startBeatTime, double milliseconds) {
            double beatTime = startBeatTime;

            if (milliseconds >= 0) {
                TimingPoint firstRedline = GetRedlineAtTime(startBeatTime);
                TimingPoint lastRedline = firstRedline;
                int index = GetTimingPointIndexAfterTime(startBeatTime, _redlines);
                for (int i = index; i < _redlines.Count && i != -1; i++) {
                    var redline = _redlines[index];
                    var beatDiff = lastRedline == firstRedline ? 
                        redline.Offset - startBeatTime:
                        redline.Offset - lastRedline.Offset;

                    if (beatDiff * lastRedline.MpB > milliseconds + Precision.DOUBLE_EPSILON) {
                        break;
                    }

                    milliseconds -= beatDiff * lastRedline.MpB;
                    beatTime += beatDiff;

                    lastRedline = redline;
                }
                beatTime += milliseconds / lastRedline.MpB;
            } else {
                int index = GetTimingPointIndexAtTime(startBeatTime, _redlines);
                double lastBeatTime = startBeatTime;
                TimingPoint redline = index == -1 ? GetFirstTimingPointExtended() : _redlines[index];
                for (int i = index; i >= 0; i--) {
                    redline = _redlines[index];
                    double beatDiff = redline.Offset - lastBeatTime;

                    if (beatDiff * redline.MpB < milliseconds - Precision.DOUBLE_EPSILON) {
                        break;
                    }

                    milliseconds -= beatDiff * redline.MpB;
                    beatTime += beatDiff;

                    lastBeatTime = redline.Offset;
                }
                beatTime += milliseconds / redline.MpB;
            }

            return beatTime;
        }

        /// <summary>
        /// Assumes all the redlines are in beat timing and calculates the millisecond time for a beat time.
        /// 0 beatTime returns originTime.
        /// </summary>
        /// <param name="originTime"></param>
        /// <param name="beatTime"></param>
        /// <param name="round"></param>
        /// <param name="divisors"></param>
        /// <returns></returns>
        public double WalkBeatsInMillisecondTime(double beatTime, double originTime = 0, bool round = false, IBeatDivisor[] divisors = null) {
            double ms = originTime;

            if (beatTime >= 0) {
                TimingPoint firstRedline = GetRedlineAtTime(originTime);
                TimingPoint lastRedline = firstRedline;
                int index = GetTimingPointIndexAfterTime(originTime, _redlines);
                for (int i = index; i < _redlines.Count && i != -1; i++) {
                    var redline = _redlines[index];
                    var msDiff = lastRedline == firstRedline ?
                        redline.Offset - originTime :
                        redline.Offset - lastRedline.Offset;
                    var beatDiff = round ? MultiSnapRound(msDiff / lastRedline.MpB, divisors) : msDiff / lastRedline.MpB;

                    if (beatDiff > beatTime + Precision.DOUBLE_EPSILON) {
                        break;
                    }

                    beatTime -= beatDiff;
                    ms += msDiff;

                    lastRedline = redline;
                }
                ms += beatTime * lastRedline.MpB;
            } else {
                int index = GetTimingPointIndexAtTime(originTime, _redlines);
                double lastBeatTime = originTime;
                TimingPoint redline = index == -1 ? GetFirstTimingPointExtended() : _redlines[index];
                for (int i = index; i >= 0; i--) {
                    redline = _redlines[index];
                    double msDiff = redline.Offset - lastBeatTime;
                    var beatDiff = round ? MultiSnapRound(msDiff / redline.MpB, divisors) : msDiff / redline.MpB;

                    if (beatDiff < beatTime - Precision.DOUBLE_EPSILON) {
                        break;
                    }

                    beatTime -= beatDiff;
                    ms += msDiff;

                    lastBeatTime = redline.Offset;
                }
                ms += beatTime * redline.MpB;
            }

            return ms;
        }

        #region TimingPointGetters

        /// <summary>
        /// Finds the timing point which is in effect at a given time with a custom set of timing points.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints">All the timing points.</param>
        /// <param name="firstTimingpoint">The first timing point to start searching from.</param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAtTime(double time, IReadOnlyList<TimingPoint> timingPoints, TimingPoint firstTimingpoint) {
            var index = GetTimingPointIndexAtTime(time, timingPoints);
            return index != -1 ? timingPoints[index] : firstTimingpoint;
        }

        /// <summary>
        /// Finds the index of the timing point which is in effect at a given time with a custom set of timing points.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints">All the timing points.</param>
        /// <returns></returns>
        public static int GetTimingPointIndexAtTime(double time, IReadOnlyList<TimingPoint> timingPoints) {
            var index = BinarySearchUtil.BinarySearch(timingPoints, time, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Rightmost);
            if (index < 0) {
                index = ~index;
                return index == 0 ? -1 : index - 1;
            }

            return index;
        }

        /// <summary>
        /// Gets the first timing point after specified time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints"></param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAfterTime(double time, IReadOnlyList<TimingPoint> timingPoints) {
            var index = GetTimingPointIndexAfterTime(time, timingPoints);
            return index != -1 ? timingPoints[index] : null;
        }

        /// <summary>
        /// Gets the index of the first timing point after specified time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints"></param>
        /// <returns></returns>
        public static int GetTimingPointIndexAfterTime(double time, IReadOnlyList<TimingPoint> timingPoints) {
            var index = BinarySearchUtil.BinarySearch(timingPoints, time, tp => tp.Offset, BinarySearchUtil.EqualitySelection.Rightmost);
            if (index < 0) {
                index = ~index;

                return index < timingPoints.Count ? index : -1;
            }

            return index + 1 < timingPoints.Count ? index + 1 : -1;
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
        /// This gives the value from the .osu.
        /// Ranges from 0.1 to 10.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetSvAtTime(double time) {
            var lastTp = GetTimingPointAtTime(time, _timingPoints, null);
            if (lastTp == null || lastTp.Uninherited) {
                return 1;
            }

            return MathHelper.Clamp(lastTp.GetSliderVelocity(), 0.1, 10);
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
        
        #endregion

        /// <summary>
        /// Calculates the duration of a slider using the slider velocity and milliseconds per beat at a given time, global multiplier and the pixel length.
        /// </summary>
        /// <param name="time">StartTime of slider.</param>
        /// <param name="length">Pixel length of slider.</param>
        /// <returns>The duration of the slider in milliseconds.</returns>
        public double CalculateSliderDuration(double time, double length) {
            var sv = GetSvAtTime(time);
            return CalculateSliderDuration(time, length, sv);
        }

        public double CalculateSliderDuration(double time, double length, double sv) {
            return SvHelper.CalculateSliderDuration(length, GetMpBAtTime(time), sv, GlobalSliderMultiplier);
        }

        public double CalculateSliderBeatLength(double length, double sv) {
            return SvHelper.CalculateSliderBeatLength(length, sv, GlobalSliderMultiplier);
        }

        public TimingPoint GetFirstTimingPointExtended() {
            // Add an extra timingpoint that is the same as the first redline but 128 x meter beats earlier so any objects before the first redline can use that thing

            // When you have a greenline before the first redline, the greenline will act like the first redline and you can snap objects to the greenline's bpm. 
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SliderVelocity slider will be 600 bpm.
            // The timeline will work like a redline on 0 offset and 1000 milliseconds per beat

            TimingPoint firstTp = _timingPoints.FirstOrDefault();
            if( firstTp != null && firstTp.Uninherited ) {
                return new TimingPoint(firstTp.Offset - firstTp.MpB * firstTp.Meter.TempoDenominator * 128, firstTp.MpB,
                                        firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume, firstTp.Uninherited, false, false);
            }

            if (firstTp != null)
                return new TimingPoint(0, 1000, firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume,
                    firstTp.Uninherited, false, false);

            return new TimingPoint(0, 0, 0, SampleSet.Auto, 0, 0, true, false, false);
        }
    }
}
