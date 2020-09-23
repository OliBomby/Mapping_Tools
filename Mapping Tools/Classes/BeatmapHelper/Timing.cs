using Mapping_Tools.Classes.MathUtil;
using System;
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
    public class Timing {
        /// <summary>
        /// List of all timing points. This included uninherited timing points and inherited timing points.
        /// This list should be sorted at all times.
        /// </summary>
        public List<TimingPoint> TimingPoints { get; set; }

        /// <summary>
        /// The global slider multiplier of a <see cref="Beatmap"/>. This is here for convenience sake to calculate absolute slider velocities.
        /// </summary>
        public double SliderMultiplier { get; set; }

        public Timing(double sliderMultiplier) {
            TimingPoints = new List<TimingPoint>();
            SliderMultiplier = sliderMultiplier;
        }

        /// <inheritdoc />
        public Timing(List<TimingPoint> timingPoints, double sliderMultiplier) {
            TimingPoints = timingPoints;
            SliderMultiplier = sliderMultiplier;
            Sort();
        }

        /// <inheritdoc />
        public Timing(List<string> timingLines, double sliderMultiplier) {
            TimingPoints = GetTimingPoints(timingLines);
            SliderMultiplier = sliderMultiplier;
            Sort();
        }

        /// <summary>
        /// Sorts all <see cref="TimingPoint"/> in order of time.
        /// </summary>
        public void Sort() {
            TimingPoints = TimingPoints.OrderBy(o => o.Offset).ThenByDescending(o => o.Uninherited).ToList();
        }

        /// <summary>
        /// Calculates the number of beats between the start time and the end time.
        /// The resulting number of beats will be rounded to a 1/16 or 1/12 beat divisor.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="round">To round the number of beats to a snap divisor.</param>
        /// <returns></returns>
        public double GetBeatLength(double startTime, double endTime, bool round = false) {
            var redlines = GetTimingPointsInTimeRange(startTime, endTime)
                .Where(tp => tp.Uninherited);

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

            if( afterTp != null && newTime >= afterTp.Offset - 10 ) {
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
        /// Calculates the size of the effective time range of a given timing point.
        /// This range stops at the next timing point, so it just returns the offset of the next timing point.
        /// </summary>
        /// <param name="timingPoint"></param>
        /// <returns>The timing point after specified timing point.</returns>
        public double GetTimingPointEffectiveRange(TimingPoint timingPoint) {
            foreach (var tp in TimingPoints.Where(tp => Precision.DefinitelyBigger(tp.Offset, timingPoint.Offset))) {
                return tp.Offset;
            }

            return double.PositiveInfinity; // Being the last timingpoint, the effective range is infinite (very big)
        }

        /// <summary>
        /// Finds the timing point which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetTimingPointAtTime(double time) {
            return GetTimingPointAtTime(time, TimingPoints, GetFirstTimingPointExtended());
        }

        /// <summary>
        /// Finds the timing point which is in effect at a given time with a custom set of timing points.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints">All the timing points.</param>
        /// <param name="firstTimingpoint">The first timing point to start searching from.</param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAtTime(double time, List<TimingPoint> timingPoints, TimingPoint firstTimingpoint) {
            TimingPoint lastTp = firstTimingpoint;
            foreach (TimingPoint tp in timingPoints) {
                if (Precision.DefinitelyBigger(tp.Offset, time)) {
                    return lastTp;
                }
                lastTp = tp;
            }
            return lastTp;
        }

        /// <summary>
        /// Finds all the timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<TimingPoint> GetTimingPointsInTimeRange(double startTime, double endTime) {
            return TimingPoints.Where(tp => Precision.DefinitelyBigger(tp.Offset, startTime) && Precision.DefinitelyBigger(endTime, tp.Offset)).ToList();
        }

        /// <summary>
        /// Finds all the uninherited timing points in a specified time range.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<TimingPoint> GetRedlinesInTimeRange(double startTime, double endTime) {
            return TimingPoints.Where(tp => tp.Uninherited && Precision.DefinitelyBigger(tp.Offset, startTime) && Precision.DefinitelyBigger(endTime, tp.Offset)).ToList();
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
            TimingPoint lastTp = GetFirstTimingPointExtended();
            foreach (TimingPoint tp in TimingPoints) {
                if (Precision.DefinitelyBigger(tp.Offset, time)) {
                    return lastTp;
                }
                if (!tp.Uninherited) {
                    lastTp = tp;
                }
            }
            return lastTp;
        }

        /// <summary>
        /// Finds the uninherited <see cref="TimingPoint"/> which is in effect at a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="firstTimingPoint"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAtTime(double time, TimingPoint firstTimingPoint=null) {
            TimingPoint lastTp = firstTimingPoint ?? GetFirstTimingPointExtended();
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) ) {
                    return lastTp;
                }
                if( tp.Uninherited ) {
                    lastTp = tp;
                }
            }
            return lastTp;
        }

        /// <summary>
        /// Finds the nearest uninherited timing point which starts after a given time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAfterTime(double time) {
            return TimingPoints.FirstOrDefault(tp => Precision.DefinitelyBigger(tp.Offset, time) && tp.Uninherited);
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
            double lastSv = -100;
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) ) {
                    return MathHelper.Clamp(lastSv, -1000, -10);
                }
                if( !tp.Uninherited ) {
                    lastSv = tp.MpB;
                }
                else {
                    lastSv = -100;
                }
            }
            return MathHelper.Clamp(lastSv, -1000, -10);
        }

        /// <summary>
        /// Calculates the duration of a slider using the slider velocity and milliseconds per beat at a given time, global multiplier and the pixel length.
        /// </summary>
        /// <param name="time">Time of slider.</param>
        /// <param name="length">Pixel length of slider.</param>
        /// <returns>The duration of the slider in milliseconds.</returns>
        public double CalculateSliderTemporalLength(double time, double length) {
            var sv = GetSvAtTime(time);
            return ( length * GetMpBAtTime(time) * (double.IsNaN(sv) ? -100 : sv) ) / ( -10000 * SliderMultiplier );
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

        public List<TimingPoint> GetAllRedlines() {
            return TimingPoints.Where(tp => tp.Uninherited).ToList();
        }

        public List<TimingPoint> GetAllGreenlines() {
            return TimingPoints.Where(tp => !tp.Uninherited).ToList();
        }

        private static List<TimingPoint> GetTimingPoints(List<string> timingLines) {
            return timingLines.Select(line => new TimingPoint(line)).ToList();
        }

        public TimingPoint GetFirstTimingPointExtended() {
            // Add an extra timingpoint that is the same as the first redline but like 10 x meter beats earlier so any objects before the first redline can use that thing

            // When you have a greenline before the first redline, the greenline will act like the first redline and you can snap objects to the greenline's bpm. 
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SliderVelocity slider will be 600 bpm.
            // The timeline will work like a redline on 0 offset and 1000 milliseconds per beat

            TimingPoint firstTp = TimingPoints.FirstOrDefault();
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
