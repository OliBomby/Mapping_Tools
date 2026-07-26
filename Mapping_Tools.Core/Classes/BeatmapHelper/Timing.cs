using System.Collections;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Classes.BeatmapHelper {
#nullable disable

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
        private List<TimingPoint> timingPoints;
        private List<TimingPoint> redlines;
        private List<TimingPoint> greenlines;

        /// <summary>
        /// Gets the timing points.
        /// </summary>
        public IReadOnlyList<TimingPoint> TimingPoints => timingPoints;
        /// <summary>
        /// Gets the redlines.
        /// </summary>
        public IReadOnlyList<TimingPoint> Redlines => redlines;
        /// <summary>
        /// Gets the greenlines.
        /// </summary>
        public IReadOnlyList<TimingPoint> Greenlines => greenlines;

        /// <summary>
        /// The global slider multiplier of a <see cref="Beatmap"/>. This is here for convenience sake to calculate absolute slider velocities.
        /// </summary>
        public double SliderMultiplier { get; set; }

        /// <summary>
        /// Creates empty timing data with a global slider multiplier.
        /// </summary>
        /// <param name="sliderMultiplier">The slider multiplier.</param>
        public Timing(double sliderMultiplier) {
            SetTimingPoints(null);
            SliderMultiplier = sliderMultiplier;
        }

        /// <summary>
        /// Creates timing data from points, sorting and partitioning redlines and greenlines.
        /// </summary>
        /// <param name="timingPoints">The timing points.</param>
        /// <param name="sliderMultiplier">The slider multiplier.</param>
        public Timing(List<TimingPoint> timingPoints, double sliderMultiplier) {
            SetTimingPoints(timingPoints);
            SliderMultiplier = sliderMultiplier;
        }

        /// <summary>
        /// Parses timing-point lines and builds sorted timing indexes.
        /// </summary>
        /// <param name="timingLines">The timing lines.</param>
        /// <param name="sliderMultiplier">The slider multiplier.</param>
        public Timing(IEnumerable<string> timingLines, double sliderMultiplier) {
            SetTimingPoints(GetTimingPoints(timingLines).ToList());
            SliderMultiplier = sliderMultiplier;
        }

        /// <summary>
        /// Replaces all the timingpoints and sorts again.
        /// </summary>
        /// <param name="timingPoints"></param>
        public void SetTimingPoints(List<TimingPoint> timingPoints) {
            this.timingPoints = timingPoints ?? new List<TimingPoint>();
            this.timingPoints.Sort();
            redlines = this.timingPoints.Where(tp => tp.Uninherited).ToList();
            greenlines = this.timingPoints.Where(tp => !tp.Uninherited).ToList();
        }

        /// <summary>
        /// Sorts all <see cref="TimingPoint"/> in order of time.
        /// </summary>
        public void Sort() {
            timingPoints.Sort();
            redlines.Sort();
            greenlines.Sort();
        }

        #region BasicOperations

        /// <summary>
        /// Inserts a timing point into the complete list and the matching redline/greenline index while preserving order.
        /// </summary>
        /// <param name="tp">The tp.</param>
        public void Add(TimingPoint tp) {
            if (tp == null) return;

            var index = timingPoints.BinarySearch(tp);
            if (index < 0)
                index = ~index;

            timingPoints.Insert(index, tp);

            if (tp.Uninherited) {
                index = redlines.BinarySearch(tp);
                if (index < 0)
                    index = ~index;

                redlines.Insert(index, tp);
            } else {
                index = greenlines.BinarySearch(tp);
                if (index < 0)
                    index = ~index;

                greenlines.Insert(index, tp);
            }
        }

        /// <summary>
        /// Removes the equal sorted entry from both the complete and type-specific indexes.
        /// </summary>
        /// <param name="tp">The tp.</param>
        /// <returns><see langword="true"/> when the point was present in its type-specific index.</returns>
        public bool Remove(TimingPoint tp) {
            var index = timingPoints.BinarySearch(tp);
            if (index >= 0) {
                timingPoints.RemoveAt(index);
            }

            if (tp.Uninherited) {
                index = redlines.BinarySearch(tp);
                if (index >= 0) {
                    redlines.RemoveAt(index);
                    return true;
                }
            } else {
                index = greenlines.BinarySearch(tp);
                if (index >= 0) {
                    greenlines.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Inserts each point through <see cref="Add(TimingPoint)"/> so all indexes remain sorted.
        /// </summary>
        /// <param name="timingPoints">The timing points.</param>
        public void AddRange(IEnumerable<TimingPoint> timingPoints) {
            foreach (var timingPoint in timingPoints) {
                Add(timingPoint);
            }
        }

        /// <summary>
        /// Copies the chronologically sorted complete timing list into an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The array index.</param>
        public void CopyTo(TimingPoint[] array, int arrayIndex) {
            timingPoints.CopyTo(array, arrayIndex);
        }

        bool ICollection<TimingPoint>.Remove(TimingPoint tp) {
            return tp != null && Remove(tp);
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        public int Count => timingPoints.Count;
        /// <summary>
        /// Gets the is read only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes every point from the complete, redline, and greenline indexes.
        /// </summary>
        public void Clear() {
            timingPoints.Clear();
            redlines.Clear();
            greenlines.Clear();
        }

        /// <summary>
        /// Checks the complete timing list using <see cref="TimingPoint.Equals(TimingPoint)"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><see langword="true"/> when an equal point exists.</returns>
        public bool Contains(TimingPoint item) {
            return timingPoints.Contains(item);
        }

        /// <summary>
        /// Shifts every timing-point offset by a number of milliseconds without changing order.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public void Offset(double offset) {
            timingPoints.ForEach(tp => tp.Offset += offset);
        }

        /// <summary>
        /// Removes every point matching a predicate while keeping all indexes synchronized.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns>The number of removed points.</returns>
        public int RemoveAll(Func<TimingPoint, bool> match) {
            var itemsToRemove = timingPoints.Where(match).ToList();

            foreach (var itemToRemove in itemsToRemove) {
                Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }

        /// <summary>
        /// Enumerates all timing points in chronological order.
        /// </summary>
        /// <returns>An enumerator over the complete timing list.</returns>
        public IEnumerator<TimingPoint> GetEnumerator() {
            return timingPoints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// Finds a point's index in the complete chronological list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The zero-based index, or -1 when absent.</returns>
        public int IndexOf(TimingPoint item) {
            return timingPoints.IndexOf(item);
        }

        /// <summary>
        /// Ignores index so it remains sorted.
        /// </summary>
        public void Insert(int index, TimingPoint item) {
            Add(item);
        }

        /// <summary>
        /// Removes the point at a chronological index from every relevant index.
        /// </summary>
        /// <param name="index">The chronological index to remove.</param>
        public void RemoveAt(int index) {
            var itemToRemove = timingPoints[index];
            Remove(itemToRemove);
        }

        /// <summary>
        /// Gets or sets the value at the specified index.
        /// </summary>
        /// <param name="index">The chronological index.</param>
        /// <returns>The point at the chronological index.</returns>
        public TimingPoint this[int index] {
            get => timingPoints[index];
            set => timingPoints[index] = value;
        }

        /// <summary>
        /// Deep-copies timing points while preserving the global slider multiplier.
        /// </summary>
        /// <returns>Independently mutable timing data.</returns>
        public Timing Copy() {
            return new Timing(timingPoints.Select(o => o.Copy()).ToList(), SliderMultiplier);
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
            divisors = divisors ?? RationalBeatDivisor.GetDefaultBeatDivisors();

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
                int startIndex = GetTimingPointIndexAfterTime(startBeatTime, redlines);
                for (int i = startIndex; i < redlines.Count && i != -1; i++) {
                    var redline = redlines[i];
                    var beatDiff = lastRedline == firstRedline ? 
                        redline.Offset - startBeatTime:
                        redline.Offset - lastRedline.Offset;

                    if (beatDiff * lastRedline.MpB > milliseconds + Precision.DoubleEpsilon) {
                        break;
                    }

                    milliseconds -= beatDiff * lastRedline.MpB;
                    beatTime += beatDiff;

                    lastRedline = redline;
                }
                beatTime += milliseconds / lastRedline.MpB;
            } else {
                int startIndex = GetTimingPointIndexAtTime(startBeatTime, redlines);
                double lastBeatTime = startBeatTime;
                TimingPoint redline = startIndex == -1 ? GetFirstTimingPointExtended(true) : redlines[startIndex];
                for (int i = startIndex; i >= 0; i--) {
                    redline = redlines[i];
                    double beatDiff = redline.Offset - lastBeatTime;

                    if (beatDiff * redline.MpB < milliseconds - Precision.DoubleEpsilon) {
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
        /// Assumes all the redlines are in millsecond timing and calculates the millisecond time for a beat time.
        /// 0 beatTime returns originTime.
        /// </summary>
        /// <param name="originTime"></param>
        /// <param name="beatTime"></param>
        /// <returns></returns>
        public double WalkBeatsInMillisecondTime(double beatTime, double originTime = 0, bool round = false, IBeatDivisor[] divisors = null) {
            double ms = originTime;

            if (beatTime >= 0) {
                TimingPoint firstRedline = GetRedlineAtTime(originTime);
                TimingPoint lastRedline = firstRedline;
                int startIndex = GetTimingPointIndexAfterTime(originTime, redlines);
                for (int i = startIndex; i < redlines.Count && i != -1; i++) {
                    var redline = redlines[i];
                    var msDiff = lastRedline == firstRedline ?
                        redline.Offset - originTime :
                        redline.Offset - lastRedline.Offset;
                    var beatDiff = round ? MultiSnapRound(msDiff / lastRedline.MpB, divisors) : msDiff / lastRedline.MpB;

                    if (beatDiff > beatTime + Precision.DoubleEpsilon) {
                        break;
                    }

                    beatTime -= beatDiff;
                    ms += msDiff;

                    lastRedline = redline;
                }
                ms += beatTime * lastRedline.MpB;
            } else {
                int startIndex = GetTimingPointIndexAtTime(originTime, redlines);
                double lastBeatTime = originTime;
                TimingPoint redline = startIndex == -1 ? GetFirstTimingPointExtended(true) : redlines[startIndex];
                for (int i = startIndex; i >= 0; i--) {
                    redline = redlines[i];
                    double msDiff = redline.Offset - lastBeatTime;
                    var beatDiff = round ? MultiSnapRound(msDiff / redline.MpB, divisors) : msDiff / redline.MpB;

                    if (beatDiff < beatTime - Precision.DoubleEpsilon) {
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

        /// <summary>
        /// This method calculates time of the tick on the timeline which is nearest to specified time.
        /// This method is mostly used to snap objects to timing.
        /// </summary>
        /// <param name="time">Specified time.</param>
        /// <param name="tp">Uninherited timing point to get the timing from.</param>
        /// <param name="beatDivisor">How many beats to have per timeline tick.</param>
        /// <returns></returns>
        public static double GetNearestTick(double time, TimingPoint tp, IBeatDivisor beatDivisor) {
            double d = tp.MpB * beatDivisor.GetValue();
            double remainder = ( time - tp.Offset ) % d;
            if( remainder < 0.5 * d ) {
                return time - remainder;
            }

            return time - remainder + d;
        }

        /// <summary>
        /// This method calculates time of the tick on the timeline which is nearest to specified time in beat time.
        /// This method is mostly used to snap objects to timing.
        /// </summary>
        /// <param name="time">Specified time.</param>
        /// <param name="tp">Uninherited timing point to get the timing from.</param>
        /// <param name="beatDivisor">How many beats to have per timeline tick.</param>
        /// <returns></returns>
        public static double GetNearestTickBeatTime(double time, TimingPoint tp, IBeatDivisor beatDivisor) {
            double d = beatDivisor.GetValue();
            double remainder = (time - tp.Offset) % d;
            if (remainder < 0.5 * d) {
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
        /// <param name="beatDivisors"></param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <param name="exactMode">If true, interprets time not as milliseconds and prevents big rounding operations.</param>
        /// <returns>The snapped time.</returns>
        public double Resnap(double time, IEnumerable<IBeatDivisor> beatDivisors, bool floor=true, 
            TimingPoint tp=null, TimingPoint firstTp=null, bool exactMode=false) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime = 0;
            double lowestDistance = double.PositiveInfinity;

            foreach (var beatDivisor in beatDivisors) {
                var t = GetNearestTick(time, beforeTp, beatDivisor);
                var d = Math.Abs(time - t);

                if (d < lowestDistance) {
                    lowestDistance = d;
                    newTime = t;
                }
            }

            if (!exactMode && afterTp != null && newTime > beforeTp.Offset + 10 && newTime >= afterTp.Offset - 10) {
                newTime = afterTp.Offset;
            }
            return floor && !exactMode ? Math.Floor(newTime) : newTime;
        }

        /// <summary>
        /// Calculates the snapped beat time for a given beat time and multiple different options.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <param name="exactMode">If true, interprets time not as milliseconds and prevents big rounding operations.</param>
        /// <returns>The snapped time.</returns>
        public double ResnapBeatTime(double time, IEnumerable<IBeatDivisor> beatDivisors,
            TimingPoint tp = null, TimingPoint firstTp = null, bool exactMode = false) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime = 0;
            double lowestDistance = double.PositiveInfinity;

            foreach (var beatDivisor in beatDivisors) {
                var t = GetNearestTickBeatTime(time, beforeTp, beatDivisor);
                var d = Math.Abs(time - t);

                if (d < lowestDistance) {
                    lowestDistance = d;
                    newTime = t;
                }
            }

            if (!exactMode && afterTp != null && newTime > beforeTp.Offset + 10 / beforeTp.MpB && newTime >= afterTp.Offset - 10 / beforeTp.MpB) {
                newTime = afterTp.Offset;
            }
            return newTime;
        }

        /// <summary>
        /// New duration is N times a beat divisor duration.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="duration"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="floor"></param>
        /// <param name="tp"></param>
        /// <param name="firstTp"></param>
        /// <returns></returns>
        public double ResnapDuration(double time, double duration, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true,
            TimingPoint tp = null, TimingPoint firstTp = null) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);

            double newDuration = 0;
            double lowestDistance = double.PositiveInfinity;

            foreach (var beatDivisor in beatDivisors) {
                var nd = GetNearestMultiple(duration, beforeTp.MpB * beatDivisor.GetValue());
                var d = Math.Abs(duration - nd);

                if (d < lowestDistance) {
                    lowestDistance = d;
                    newDuration = nd;
                }
            }

            return floor ? Math.Floor(newDuration) : newDuration;
        }

        /// <summary>
        /// Calculates the snapped time for a given time and makes sure the snapped time remains inside the specified time range.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="rangeStart">The exclusive lower boundary for the snapped time.</param>
        /// <param name="rangeEnd">The exclusive upper boundary for the snapped time.</param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <returns>The snapped time.</returns>
        public double ResnapInRange(double time, IEnumerable<IBeatDivisor> beatDivisors, double rangeStart, double rangeEnd, bool floor=true, TimingPoint tp=null, TimingPoint firstTp=null) {
            TimingPoint beforeTp = tp ?? GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime = 0;
            double lowestDistance = double.PositiveInfinity;

            foreach (var beatDivisor in beatDivisors) {
                var t = GetNearestTick(time, beforeTp, beatDivisor);
                var d = Math.Abs(time - t);

                if (d < lowestDistance) {
                    lowestDistance = d;
                    newTime = t;
                }
            }

            if (afterTp != null && newTime > beforeTp.Offset + 10 && newTime >= afterTp.Offset - 10) {
                newTime = afterTp.Offset;
            }

            // Don't resnap if it would move outside
            if (newTime <= rangeStart + 1 || newTime >= rangeEnd - 1) {
                newTime = time;
            }

            return floor ? Math.Floor(newTime) : newTime;
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

        /// <summary>
        /// Selects a contiguous chronological slice between two millisecond offsets.
        /// </summary>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="timingPoints">The timing points.</param>
        /// <param name="inclusive">The inclusive.</param>
        /// <returns>A new list containing the points inside the inclusive or exclusive bounds.</returns>
        public static List<TimingPoint> GetTimingPointsInRange(double startTime, double endTime,
            List<TimingPoint> timingPoints, bool inclusive = true) {
            if (!inclusive) {
                startTime += Precision.DoubleEpsilon;
                endTime -= Precision.DoubleEpsilon;
            } else {
                startTime -= Precision.DoubleEpsilon;
                endTime += Precision.DoubleEpsilon;
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
            return GetTimingPointAtTime(time, timingPoints, GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds all the timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="inclusive"></param>
        /// <returns></returns>
        public List<TimingPoint> GetTimingPointsInRange(double startTime, double endTime, bool inclusive = true) {
            return GetTimingPointsInRange(startTime, endTime, timingPoints, inclusive);
        }

        /// <summary>
        /// Finds all the uninherited timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="inclusive"></param>
        /// <returns></returns>
        public List<TimingPoint> GetRedlinesInRange(double startTime, double endTime, bool inclusive = true) {
            return GetTimingPointsInRange(startTime, endTime, redlines, inclusive);
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
            return GetTimingPointAtTime(time, greenlines, GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds the uninherited <see cref="TimingPoint"/> which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="firstTimingPoint"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAtTime(double time, TimingPoint firstTimingPoint=null) {
            return GetTimingPointAtTime(time, redlines, firstTimingPoint ?? GetFirstTimingPointExtended(true));
        }

        /// <summary>
        /// Finds the nearest uninherited timing point which starts after a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAfterTime(double time) {
            return GetTimingPointAfterTime(time, redlines);
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
            var lastTp = GetTimingPointAtTime(time, timingPoints, null);
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
            var afterTp = GetTimingPointAfterTime(timingPoint.Offset, timingPoints);
            return afterTp?.Offset ?? double.PositiveInfinity;
        }
        
        #endregion

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

        /// <summary>
        /// Converts pixel length to milliseconds using an explicit inherited SV value.
        /// </summary>
        /// <param name="time">The slider start time in milliseconds, used to resolve BPM.</param>
        /// <param name="length">The slider length in osu! pixels.</param>
        /// <param name="sv">The sv.</param>
        /// <returns>The duration of one slider span in milliseconds.</returns>
        public double CalculateSliderTemporalLength(double time, double length, double sv) {
            return length * GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : MathHelper.Clamp(sv, -1000, -10)) / 
                   (-10000 * SliderMultiplier);
        }

        /// <summary>
        /// Converts pixel length to beats using an explicit inherited SV value.
        /// </summary>
        /// <param name="length">The slider length in osu! pixels.</param>
        /// <param name="sv">The sv.</param>
        /// <returns>The duration as a number of beats.</returns>
        public double CalculateSliderBeatLength(double length, double sv) {
            return length * (double.IsNaN(sv) ? -100 : MathHelper.Clamp(sv, -1000, -10)) / 
                   (-10000 * SliderMultiplier);
        }

        /// <summary>
        /// Calculates the pixel length of a slider using the duration of the slider.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="temporalLength"></param>
        /// <returns></returns>
        public double CalculateSliderLength(double time, double temporalLength) {
            var sv = GetSvAtTime(time);
            return -10000 * temporalLength * SliderMultiplier / ( GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv) );
        }

        /// <summary>
        /// Converts a millisecond duration to slider pixels using an explicit inherited SV value.
        /// </summary>
        /// <param name="time">The slider start time in milliseconds, used to resolve BPM.</param>
        /// <param name="temporalLength">The temporal length.</param>
        /// <param name="sv">The sv.</param>
        /// <returns>The slider pixel length.</returns>
        public double CalculateSliderLengthCustomSv(double time, double temporalLength, double sv) {
            return -10000 * temporalLength * SliderMultiplier / ( GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv) );
        }

        private static IEnumerable<TimingPoint> GetTimingPoints(IEnumerable<string> timingLines) {
            return timingLines.Select(line => new TimingPoint(line));
        }

        /// <summary>
        /// Synthesizes timing before the first real point so early objects can resolve beat and sample settings.
        /// </summary>
        /// <param name="needRedline">The need redline.</param>
        /// <returns>An earlier copy of the first redline, a 1000-ms fallback based on the first greenline, or a zeroed default.</returns>
        public TimingPoint GetFirstTimingPointExtended(bool needRedline = false) {
            // Add an extra timingpoint that is the same as the first redline but like 10 x meter beats earlier so any objects before the first redline can use that thing

            // When you have a greenline before the first redline, the greenline will act like the first redline and you can snap objects to the greenline's bpm. 
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SliderVelocity slider will be 600 bpm.
            // The timeline will work like a redline on 0 offset and 1000 milliseconds per beat

            TimingPoint firstTp = timingPoints.FirstOrDefault();
            if( firstTp != null && firstTp.Uninherited ) {
                return new TimingPoint(firstTp.Offset - firstTp.MpB * firstTp.Meter.TempoDenominator * 10, firstTp.MpB,
                                        firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume, needRedline || firstTp.Uninherited, false, false);
            }

            if (firstTp != null)
                return new TimingPoint(0, 1000, firstTp.Meter, firstTp.SampleSet, firstTp.SampleIndex, firstTp.Volume,
                    needRedline || firstTp.Uninherited, false, false);

            return new TimingPoint(0, 0, 0, SampleSet.None, 0, 0, true, false, false);
        }
    }
}
