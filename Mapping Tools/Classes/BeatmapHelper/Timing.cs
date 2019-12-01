using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.BeatmapHelper {
    /// <summary>
    /// 
    /// </summary>
    public class Timing {
        /// <summary>
        /// 
        /// </summary>
        public List<TimingPoint> TimingPoints { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double SliderMultiplier { get; set; }

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
            TimingPoints = TimingPoints.OrderBy(o => o.Offset).ThenByDescending(o => o.Inherited).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="tp"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static double GetNearestTimeMeter(double time, TimingPoint tp, int divisor) {
            double d = tp.MpB / divisor;
            double remainder = ( time - tp.Offset ) % d;
            if( remainder < 0.5 * d ) {
                return time - remainder;
            }
            else {
                return time - remainder + d;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="divisor"></param>
        /// <returns></returns>
        public static double GetNearestMultiple(double duration, double divisor) {
            double remainder = duration % divisor;

            if (remainder < 0.5 * divisor) {
                return duration - remainder;
            } else {
                return duration - remainder + divisor;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="divisor2"></param>
        /// <param name="divisor3"></param>
        /// <param name="floor"></param>
        /// <param name="tp"></param>
        /// <param name="firstTP"></param>
        /// <returns></returns>
        public double Resnap(double time, int divisor2, int divisor3, bool floor=true, TimingPoint tp=null, TimingPoint firstTP=null) {
            TimingPoint beforeTP = tp ?? GetRedlineAtTime(time, firstTP);
            TimingPoint afterTP = tp == null ? GetRedlineAfterTime(time) : null;

            double newTime2 = GetNearestTimeMeter(time, beforeTP, divisor2);
            double snapDistance2 = Math.Abs(time - newTime2);

            double newTime3 = GetNearestTimeMeter(time, beforeTP, divisor3);
            double snapDistance3 = Math.Abs(time - newTime3);

            double newTime = snapDistance3 < snapDistance2 ? newTime3 : newTime2;

            if( afterTP != null && newTime >= afterTP.Offset - 10 ) {
                newTime = afterTP.Offset;
            }
            if (floor) {
                return Math.Floor(newTime);
            } else {
                return newTime;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="divisor2"></param>
        /// <param name="divisor3"></param>
        /// <param name="ho"></param>
        /// <param name="floor"></param>
        /// <returns></returns>
        public double ResnapInRange(double time, int divisor2, int divisor3, HitObject ho, bool floor=true) {
            TimingPoint beforeTP = GetRedlineAtTime(time);
            TimingPoint afterTP = GetRedlineAfterTime(time);

            double newTime2 = GetNearestTimeMeter(time, beforeTP, divisor2);
            double snapDistance2 = Math.Abs(time - newTime2);

            double newTime3 = GetNearestTimeMeter(time, beforeTP, divisor3);
            double snapDistance3 = Math.Abs(time - newTime3);

            double newTime = snapDistance3 < snapDistance2 ? newTime3 : newTime2;

            if ( afterTP != null && Precision.DefinitelyBigger(newTime, afterTP.Offset) ) {
                newTime = afterTP.Offset;
            }

            if( newTime <= ho.Time + 1 || newTime >= ho.EndTime - 1 ) // Don't resnap if it would move outside
            {
                newTime = time;
            }
            if (floor) {
                return Math.Floor(newTime);
            } else {
                return newTime;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ttp"></param>
        /// <returns></returns>
        public double GetTimingPointEffectiveRange(TimingPoint ttp) {
            foreach( TimingPoint tp in TimingPoints ) {
                if(Precision.DefinitelyBigger(tp.Offset, ttp.Offset) ) {
                    return tp.Offset;
                }
            }
            return double.PositiveInfinity; // Being the last timingpoint, the effective range is infinite (very big)
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetTimingPointAtTime(double time) {
            TimingPoint lastTP = GetFirstTimingPointExtended();
            foreach( TimingPoint tp in TimingPoints ) {
                if(Precision.DefinitelyBigger(tp.Offset, time) ) {
                    return lastTP;
                }
                lastTP = tp;
            }
            return lastTP;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="timingPoints"></param>
        /// <param name="firstTimingpoint"></param>
        /// <returns></returns>
        public static TimingPoint GetTimingPointAtTime(double time, List<TimingPoint> timingPoints, TimingPoint firstTimingpoint) {
            TimingPoint lastTP = firstTimingpoint;
            foreach (TimingPoint tp in timingPoints) {
                if (Precision.DefinitelyBigger(tp.Offset, time)) {
                    return lastTP;
                }
                lastTP = tp;
            }
            return lastTP;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public List<TimingPoint> GetTimingPointsInTimeRange(double startTime, double endTime) {
            List<TimingPoint> TPs = new List<TimingPoint>();
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, startTime) && Precision.DefinitelyBigger(endTime, tp.Offset) ) {
                    TPs.Add(tp);
                }
            }
            return TPs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetBPMAtTime(double time) {
            return 60000 / GetMpBAtTime(time);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetMpBAtTime(double time) {
            return GetRedlineAtTime(time).MpB;
        }

        /// <summary>
        /// Returns with the inherited <see cref="TimingPoint"/> from the selected time in ms.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetGreenlineAtTime(double time) {
            TimingPoint lastTP = GetFirstTimingPointExtended();
            foreach (TimingPoint tp in TimingPoints) {
                if (Precision.DefinitelyBigger(tp.Offset, time)) {
                    return lastTP;
                }
                if (!tp.Inherited) {
                    lastTP = tp;
                }
            }
            return lastTP;
        }

        /// <summary>
        /// Returns the <see cref="TimingPoint"/> 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="firstTimingPoint"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAtTime(double time, TimingPoint firstTimingPoint=null) {
            TimingPoint lastTP = firstTimingPoint ?? GetFirstTimingPointExtended();
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) ) {
                    return lastTP;
                }
                if( tp.Inherited ) {
                    lastTP = tp;
                }
            }
            return lastTP;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public TimingPoint GetRedlineAfterTime(double time) {
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) && tp.Inherited) {
                    return tp;
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetSVMultiplierAtTime(double time) {
            return -100 / GetSVAtTime(time);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetSVAtTime(double time) {
            double lastSV = -100;
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) ) {
                    return MathHelper.Clamp(lastSV, -1000, -10);
                }
                if( !tp.Inherited ) {
                    lastSV = tp.MpB;
                }
                else {
                    lastSV = -100;
                }
            }
            return MathHelper.Clamp(lastSV, -1000, -10);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public double CalculateSliderTemporalLength(double time, double length) {
            return ( length * GetMpBAtTime(time) * GetSVAtTime(time) ) / ( -10000 * SliderMultiplier );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="temporalLength"></param>
        /// <returns></returns>
        public double CalculateSliderLength(double time, double temporalLength) {
            return ( -10000 * temporalLength * SliderMultiplier ) / ( GetMpBAtTime(time) * GetSVAtTime(time) );
        }

        public double CalculateSliderLengthCustomSV(double time, double temporalLength, double sv) {
            return ( -10000 * temporalLength * SliderMultiplier ) / ( GetMpBAtTime(time) * sv );
        }

        public List<TimingPoint> GetAllRedlines() {
            List<TimingPoint> redlines = new List<TimingPoint>();
            foreach( TimingPoint tp in TimingPoints ) {
                if( tp.Inherited ) {
                    redlines.Add(tp);
                }
            }
            return redlines;
        }

        public List<TimingPoint> GetAllGreenlines() {
            List<TimingPoint> greenlines = new List<TimingPoint>();
            foreach (TimingPoint tp in TimingPoints) {
                if (!tp.Inherited) {
                    greenlines.Add(tp);
                }
            }
            return greenlines;
        }

        private List<TimingPoint> GetTimingPoints(List<string> timingLines) {
            List<TimingPoint> timingPoints = new List<TimingPoint>();

            foreach (string line in timingLines) { 
                timingPoints.Add(new TimingPoint(line));
            }

            return timingPoints;
        }

        public TimingPoint GetFirstTimingPointExtended() {
            // Add an extra timingpoint that is the same as the first redline but like 10 x meter beats earlier so any objects before the first redline can use that thing

            // When you have a greenline before the first redline, the greenline will act like the first redline and you can snap objects to the greenline's bpm. 
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SliderVelocity slider will be 600 bpm.
            // The timeline will work like a redline on 0 offset and 1000 milliseconds per beat

            TimingPoint firstTP = TimingPoints.FirstOrDefault();
            if( firstTP.Inherited ) {
                return new TimingPoint(firstTP.Offset - firstTP.MpB * firstTP.Meter * 10, firstTP.MpB,
                                        firstTP.Meter, firstTP.SampleSet, firstTP.SampleIndex, firstTP.Volume, firstTP.Inherited, false, false);
            }
            else {
                return new TimingPoint(0, 1000, firstTP.Meter, firstTP.SampleSet, firstTP.SampleIndex, firstTP.Volume, firstTP.Inherited, false, false);
            }

        }
    }
}
