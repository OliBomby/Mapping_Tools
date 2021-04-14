using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.BeatDivisors;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Objects;
using Mapping_Tools_Core.BeatmapHelper.TimingStuff;
using Mapping_Tools_Core.BeatmapHelper.Types;
using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.ToolHelpers {
    /// <summary>
    /// Helper class for resnapping stuff to a <see cref="Timing"/>.
    /// </summary>
    public static class ResnapUtil {
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
            double remainder = (time - tp.Offset) % d;
            if (remainder < 0.5 * d) {
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
        /// <param name="timing">The timing to resnap to.</param>
        /// <param name="time"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <param name="exactMode">If true, interprets time not as milliseconds and prevents big rounding operations.</param>
        /// <returns>The snapped time.</returns>
        public static double Resnap(this Timing timing, double time, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true,
            TimingPoint tp = null, TimingPoint firstTp = null, bool exactMode = false) {
            TimingPoint beforeTp = tp ?? timing.GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? timing.GetRedlineAfterTime(time) : null;

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
            return floor && !exactMode ? Math.Floor(newTime + Precision.DOUBLE_EPSILON) : newTime;
        }

        /// <summary>
        /// Calculates the snapped beat time for a given beat time and multiple different options.
        /// </summary>
        /// <param name="timing">The timing to resnap to.</param>
        /// <param name="time"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <param name="exactMode">If true, interprets time not as milliseconds and prevents big rounding operations.</param>
        /// <returns>The snapped time.</returns>
        public static double ResnapBeatTime(this Timing timing, double time, IEnumerable<IBeatDivisor> beatDivisors,
            TimingPoint tp = null, TimingPoint firstTp = null, bool exactMode = false) {
            TimingPoint beforeTp = tp ?? timing.GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? timing.GetRedlineAfterTime(time) : null;

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
        /// <param name="timing">The timing to resnap to.</param>
        /// <param name="time"></param>
        /// <param name="duration"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="floor"></param>
        /// <param name="tp"></param>
        /// <param name="firstTp"></param>
        /// <returns></returns>
        public static double ResnapDuration(this Timing timing, double time, double duration, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true,
            TimingPoint tp = null, TimingPoint firstTp = null) {
            TimingPoint beforeTp = tp ?? timing.GetRedlineAtTime(time, firstTp);

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

            return floor ? Math.Floor(newDuration + Precision.DOUBLE_EPSILON) : newDuration;
        }

        /// <summary>
        /// Calculates the snapped time for a given time and makes sure the snapped time is not outside the time range of a hit object.
        /// This can be used to resnap stuff that has to be within the time range of a slider. For example volume changes inside a slider body.
        /// </summary>
        /// <param name="timing">The timing to resnap to.</param>
        /// <param name="time"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="floor">Whether or not to floor the time after snapping.</param>
        /// <param name="tp">The uninherited timing point to snap to. Leave null for automatic selection.</param>
        /// <param name="firstTp">Overwrites the timing for anything that happens before the first timing point.
        ///     You can set this to avoid bad timing when there could be an inherited timing point before the first red line.</param>
        /// <returns>The snapped time.</returns>
        public static double ResnapInRange(this Timing timing, double time, IEnumerable<IBeatDivisor> beatDivisors, double startTime, double endTime, bool floor = true, TimingPoint tp = null, TimingPoint firstTp = null) {
            TimingPoint beforeTp = tp ?? timing.GetRedlineAtTime(time, firstTp);
            TimingPoint afterTp = tp == null ? timing.GetRedlineAfterTime(time) : null;

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
            if (newTime <= startTime + 1 || newTime >= endTime - 1) {
                newTime = time;
            }

            return floor ? Math.Floor(newTime + Precision.DOUBLE_EPSILON) : newTime;
        }

        #region HitObject

        public static bool ResnapSelf(this HitObject ho, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = ho.GetResnappedTime(timing, beatDivisors, floor, tp, firstTp);
            var deltaTime = newTime - ho.StartTime;
            ho.MoveTime(deltaTime);
            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public static bool ResnapEndTimeSmart(this Slider slider, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            // If there is a redline in the sliderbody then the sliderend gets snapped to a tick of the latest redline
            return timing.Redlines.Any(o => o.Offset <= slider.EndTime + 20 && o.Offset > slider.StartTime) ? 
                slider.ResnapEndTime(timing, beatDivisors, floor, tp, firstTp) : 
                slider.ResnapDuration(slider.StartTime, timing, beatDivisors, firstTp);

        }

        public static bool ResnapEndTime(this IDuration obj, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            var newTime = timing.Resnap(obj.EndTime, beatDivisors, floor, tp: tp, firstTp: firstTp);

            var deltaTime = newTime - obj.EndTime;
            obj.SetEndTime(newTime);

            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public static bool ResnapDuration(this IDuration obj, double time, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, TimingPoint firstTp = null) {
            double deltaTime;
            if (obj is IRepeats repeating) {
                var newDuration = timing.ResnapDuration(time, repeating.SpanDuration, beatDivisors, false, firstTp: firstTp);
                deltaTime = newDuration - repeating.SpanDuration;
                repeating.SetSpanDuration(newDuration);
            }
            else {
                var newDuration = timing.ResnapDuration(time, obj.Duration, beatDivisors, false, firstTp: firstTp);
                deltaTime = newDuration - obj.Duration;
                obj.SetDuration(newDuration);
            }

            return Math.Abs(deltaTime) > Precision.DOUBLE_EPSILON;
        }

        public static bool ResnapPosition(this HitObject ho, GameMode mode, double circleSize) {
            if (mode != GameMode.Mania) return false;
            // Resnap X to the middle of the columns and Y to 192
            var dist = 512d / Math.Round(circleSize);
            var hdist = dist / 2;

            var dX = Math.Floor(Math.Round((ho.Pos.X - hdist) / dist) * dist + hdist) - ho.Pos.X;
            var dY = 192 - ho.Pos.Y;
            ho.Move(new Vector2(dX, dY));

            return Math.Abs(dX) > Precision.DOUBLE_EPSILON || Math.Abs(dY) > Precision.DOUBLE_EPSILON;
        }

        public static double GetResnappedTime(this IHasStartTime obj, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null,
            TimingPoint firstTp = null) {
            return timing.Resnap(obj.StartTime, beatDivisors, floor, tp: tp, firstTp: firstTp);
        }

        #endregion

        #region TimingPoints

        /// <summary>
        /// Can clarify if the current timing point should snap to the nearest beat of the previous timing point.
        /// </summary>
        /// <param name="thisTimingPoint">The timing point to resnap.</param>
        /// <param name="timing"></param>
        /// <param name="beatDivisors"></param>
        /// <param name="floor"></param>
        /// <param name="tp"></param>
        /// <param name="firstTP"></param>
        /// <returns></returns>
        public static bool ResnapSelf(this TimingPoint thisTimingPoint, Timing timing, IEnumerable<IBeatDivisor> beatDivisors, bool floor = true, TimingPoint tp = null, TimingPoint firstTP = null) {
            double newTime = timing.Resnap(thisTimingPoint.Offset, beatDivisors, floor, tp: tp, firstTp: firstTP);
            double deltaTime = newTime - thisTimingPoint.Offset;
            thisTimingPoint.Offset += deltaTime;
            return deltaTime != 0;
        }

        #endregion
    }
}