using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;

namespace Mapping_Tools.Classes.Tools {
    public class Sliderator {
        public delegate double PositionFunctionDelegate(double t);
        public PositionFunctionDelegate PositionFunction { get; set; } // position along slider, ms -> px
        public double MaxT { get; set; } // end time for PositionFunction, in ms
        public double Velocity { get; set; } // slider velocity, in px/ms

        private double _Pos(double s) => PositionFunction(s * Velocity); // normalized position function, px -> px
        private double _MaxS => MaxT * Velocity; // expected pixellength

        private List<Vector2> _path; // input path
        private List<Vector2> _diff; // path segments
        private List<double> _diffL; // length of segments
        private List<double> _pathL; // cumulative length
        private double _totalPathL => _pathL.Last(); // total length

        private List<LatticePoint> _lattice; // path lattice points
        private List<Neuron> _slider; // slider red anchors, tumours and interpolations

        public void SetPath(List<Vector2> pathPoints) {
            _path = pathPoints.Copy();
            _diff = _path.Zip(_path.Skip(1), (x, y) => y - x).ToList();
            _diffL = _diff.Select(x => x.Length).ToList();
            double sum = 0;
            _pathL = new List<double> {sum};
            foreach (var l in _diffL) {
                sum += l;
                _pathL.Add(sum);
            }
            if (Math.Abs(sum) < Precision.DOUBLE_EPSILON) throw new InvalidOperationException("Zero length path.");
        }

        private Vector2 PositionAt(double x) {
            int n = _pathL.BinarySearch(x);
            if (n < 0) n = ~n - 1;
            if (n == -1) n += 1;
            if (n == _diff.Count) n -= 1;
            return _path[n] + _diff[n] / _diffL[n] * (x - _pathL[n]);
        }

        private static List<LatticePoint> LatticePoints(List<Vector2> path, double tolerance = 0.35) {
            var diff = path.Zip(path.Skip(1), (x, y) => y - x).ToList();
            var diffL = diff.Select(x => x.Length).ToList();
            double sum = 0;
            var pathL = new List<double> {sum};
            foreach (var l in diffL) {
                sum += l;
                pathL.Add(sum);
            }
            return LatticePoints(path, diff, diffL, pathL, tolerance);
        }

        private static List<LatticePoint> LatticePoints(List<Vector2> path, List<Vector2> diff,
            List<double> diffL, List<double> pathL, double tolerance = 0.35) { // tolerance >= sqrt(2)/4 may miss points
            var lattice = new List<LatticePoint>();

            for (var n = 0; n < diff.Count; n++) { // iterate through path segments
                var l = diffL[n]; // segment length
                if (Math.Abs(l) < Precision.DOUBLE_EPSILON) continue; // skip segment if degenerate
                var a = path[n]; // start point
                var d = diff[n]; // vector to end point
                var l2 = d.LengthSquared;
                var ax = Math.Abs(d[0]) < Math.Abs(d[1]) ? 1 : 0; // axis to iterate along
                var dr = Math.Sign(d[ax]); // direction of iteration
                for (var i = (int) Math.Round(a[ax]); i != (int) Math.Round(a[ax] + d[ax]) + dr; i += dr) { // major axis lattice coordinate
                    var s = (i - a[ax]) / d[ax]; // progress along segment in major axis
                    var r = a[1 - ax] + s * d[1 - ax]; // minor axis coordinate
                    var j = (int) Math.Round(r); // minor axis lattice coordinate
                    var k = ax == 1 ? new Vector2(j, i) : new Vector2(i, j); // lattice point
                    var t = MathHelper.Clamp(s + (j - r) * d[1 - ax] / l2, 0, 1); // projected progress along segment
                    var p = a + t * d; // projected point on segment
                    var x = pathL[n] + t * l; // total distance along path
                    var e = (k - p).Length; // error
                    var ep = (j - r) * d[ax] / l * (1 - 2 * ax); // right handed perpendicular error

                    if (e > tolerance) continue;
                    if (Math.Abs(t - 1) < Precision.DOUBLE_EPSILON && n + 1 < diff.Count) continue;
                    if (lattice.Count > 0 && k == lattice.Last().Pos) { // repeated point
                        if (e <= lattice.Last().Error)
                            lattice[lattice.Count - 1] = new LatticePoint(k, p, x, e, ep, n);
                    } else {
                        lattice.Add(new LatticePoint(k, p, x, e, ep, n));
                    }
                }
            }

            return lattice;
        }

        private void GetLatticePoints() {
            _lattice = LatticePoints(_path, _diff, _diffL, _pathL, 0.35);
        }

        private double NextCrossing(double start, double low, double high, out int side, double precision = 0.01, double resolution = 0.25) { // where it next crosses below a lower bound or above an upper bound
            double s = start;
            double ds = resolution;
            double x = _Pos(_MaxS);
            while (ds >= precision) {
                if (s + ds <= _MaxS) {
                    x = _Pos(s + ds);
                    if (low <= x && x <= high) {
                        s += ds;
                        continue;
                    }
                }
                ds /= 2;
            }
            s += ds;
            side = 0;
            if (x > high)
                side = 1;
            else if (x < low)
                side = -1;
            if (s + precision >= _MaxS)
                s = _MaxS;
            return s;
        }

        private void GetReds() {
            // version a.1, retarded because it handles negative v and starting at an arbitrary x, so it cant just use the already known anchor visitation order. also just not optimized or good in general.
            double t = 0;
            var x = PositionFunction(t) * _totalPathL;
            var xprev = x;
            var n = _lattice.Select(y => y.PathPosition).ToList().BinarySearch(x);
            if (n < 0) n = ~n;
            n = n != 0 && (n == _lattice.Count || x - _lattice[n - 1].PathPosition <= _lattice[n].PathPosition - x)
                ? n - 1
                : n; // closer to left?
            _slider = new List<Neuron> {new Neuron(_lattice[n])};
            while (t < MaxT) {
                xprev = x;
                t += 0.25; // 0.25 < 1 - sqrt(2)/2
                x = PositionFunction(t) * _totalPathL;
                if (n > 0 && x - _lattice[n - 1].PathPosition < _lattice[n].PathPosition - x) {
                    n -= 1;
                    _slider.Add(_lattice[n]);
                } else if (n <= _lattice.Count - 2 && _lattice[n + 1].PathPosition - x < x - _lattice[n].PathPosition) {
                    n += 1;
                    _slider.Add(_lattice[n]);
                }

                //if ((x - _lattice[n].x) * (xprev - _lattice[n].x) <= 0) {
                //    _reds[_reds.Count - 1].t = t; // dont use this
                //}
            }

            var i = 0;
            while (i < _slider.Count - 2) {
                // bad algorithm improve later
                i++;
                var rl = _slider[i - 1];
                var rm = _slider[i];
                var rr = _slider[i + 1];
                if (rm.Error < 0.125 || rm.Error < rl.Error && rm.Error < rr.Error) continue;
                if (Math.Abs(rm.ErrorPerp - rl.ErrorPerp - rr.ErrorPerp) > 0.25) {
                    _slider.RemoveAt(i);
                    i -= i > 1 ? 2 : 1;
                }

                //double dx = Math.Abs(rr.x - rl.x);
                //double dt = Math.Abs(rr.t - rl.t);
                //(dx / dt * (dt - dx) - 2) * (dt - dx); // dont know what tolerance to use but this might be a useful measurement of error
            }
        }

        private void GetInterpolation() {
            // version a.0, not even functional, just does linear between all
            _whites = new List<Interpolation>();
            double sum = 0;
            _interpL = new List<double> {sum};
            for (var n = 0; n < _slider.Count - 1; n++) {
                var a = _slider[n];
                var b = _slider[n + 1];
                _whites.Add(new Interpolation(a, b));
                sum += _whites.Last().Length;
                _interpL.Add(sum);
            }
        }

        private void GetTumours() {
            // version a.0, not functional, i just put some crap so u can test
            double t = 0;
            double d = 0;
            var x = PositionFunction(t) * _totalPathL;
            var n = 0;
            while (t < MaxT) {
                t += 0.0025;
                x = PositionFunction(t) * _totalPathL;
                if (n < _slider.Count - 1 &&
                    Math.Abs(_slider[n + 1].PathPosition - x) < Math.Abs(x - _slider[n].PathPosition)) {
                    d += _whites[n].Length + Math.Round(t - d + _whites[n].Length / 2);
                    _slider[n].Tumours.Add(new Vector2(Math.Round(t - d + _whites[n].Length / 2), 0));
                    n += 1;
                }
            }
        }

        private static List<Vector2> AnchorsList(List<Neuron> slider) {
            var anchors = new List<Vector2>();
            for (int n = 0; n < slider.Count; n++) {
                foreach (var t in slider[n].Dendrites) {
                    anchors.Add(slider[n].Nucleus.Pos);
                    anchors.Add(slider[n].Nucleus.Pos + t);
                    anchors.Add(slider[n].Nucleus.Pos);
                }
                anchors.AddRange(slider[n].Axon.Points);
            }
            return anchors;
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            GetReds();
            GetInterpolation();
            GetTumours();
            return AnchorsList(_slider);
        }

        internal struct LatticePoint {
            public Vector2 Pos; // lattice point
            public Vector2 PathPoint; // point on path
            public double PathPosition; // position along path
            public double Error; // error ||Pos - PathPoint||
            public double ErrorPerp; // perpendicular error (positive = right handed)
            public int SegmentIndex; // segment index
            public double Time; // placeholder
            public double Length; // placeholder

            public LatticePoint(Vector2 pos, Vector2 pathPoint, double pathPosition, double error,
                double errorPerp, int segmentIndex) {
                Pos = pos;
                PathPoint = pathPoint;
                PathPosition = pathPosition;
                Error = error;
                ErrorPerp = errorPerp;
                SegmentIndex = segmentIndex;
                Time = 0;
                Length = 0;
            }
        }

        internal class Neuron {
            public LatticePoint Nucleus; // start lattice point
            public List<Vector2> Dendrites; // tumour vectors (offset from main anchor)
            public BezierSubdivision Axon; // interpolation to next lattice point
            public Neuron Terminal; // end lattice point
            public double Length; // placeholder
            public double Error; // placeholder

            public Neuron(LatticePoint startLatticePoint) {
                Nucleus = startLatticePoint;
                Dendrites = new List<Vector2>();
                Axon = new BezierSubdivision(new List<Vector2> {startLatticePoint.Pos});
                Terminal = null;
                Length = 0;
                Error = 0;
            }
        }

    }
}