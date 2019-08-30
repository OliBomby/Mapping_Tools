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
    public class Timing {
        public List<TimingPoint> TimingPoints { get; set; }
        public double SliderMultiplier { get; set; }

        public Timing(List<string> timingLines, double sliderMultiplier) {
            TimingPoints = GetTimingPoints(timingLines);
            SliderMultiplier = sliderMultiplier;
            Sort();
        }

        public void Sort() {
            TimingPoints = TimingPoints.OrderBy(o => o.Offset).ThenByDescending(o => o.Inherited).ToList();
        }

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

        public static double GetNearestMultiple(double duration, double divisor) {
            double remainder = duration % divisor;

            if (remainder < 0.5 * divisor) {
                return duration - remainder;
            } else {
                return duration - remainder + divisor;
            }
        }

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

        public double GetTimingPointEffectiveRange(TimingPoint ttp) {
            foreach( TimingPoint tp in TimingPoints ) {
                if(Precision.DefinitelyBigger(tp.Offset, ttp.Offset) ) {
                    return tp.Offset;
                }
            }
            return double.PositiveInfinity; // Being the last timingpoint, the effective range is infinite (very big)
        }

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

        public List<TimingPoint> GetTimingPointsInTimeRange(double startTime, double endTime) {
            List<TimingPoint> TPs = new List<TimingPoint>();
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, startTime) && Precision.DefinitelyBigger(endTime, tp.Offset) ) {
                    TPs.Add(tp);
                }
            }
            return TPs;
        }

        public double GetBPMAtTime(double time) {
            return 60000 / GetMpBAtTime(time);
        }

        public double GetMpBAtTime(double time) {
            return GetRedlineAtTime(time).MpB;
        }

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

        public TimingPoint GetRedlineAfterTime(double time) {
            foreach( TimingPoint tp in TimingPoints ) {
                if( Precision.DefinitelyBigger(tp.Offset, time) && tp.Inherited) {
                    return tp;
                }
            }
            return null;
        }

        public double GetSVMultiplierAtTime(double time) {
            return -100 / GetSVAtTime(time);
        }

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

        public double CalculateSliderTemporalLength(double time, double length) {
            return ( length * GetMpBAtTime(time) * GetSVAtTime(time) ) / ( -10000 * SliderMultiplier );
        }

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
            // The value in the greenline will be used as the milliseconds per beat, so for example a 1x SV slider will be 600 bpm.
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
