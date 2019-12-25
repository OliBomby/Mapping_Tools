using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools {
    public class Sliderator {
        private List<Vector2> _path; // input path
        private List<Vector2> _diff; // path segments
        private List<double> _diffL; // length of segments
        private List<double> _pathL; // cumulative length
        private double _totalPathL => _pathL.Last(); // total length

        private List<LatticePoint> _lattice; // path lattice points
        private List<LatticePoint> _reds; // slider red anchors for growing tumours on
        private List<Interpolation> _whites; // slider white anchors for interpolating between reds
        private List<double> _interpL; // cumulative length of interpolations

        public double MaxT { get; set; }

        public delegate double PositionFunctionDelegate(double t);

        public PositionFunctionDelegate PositionFunction { get; set; }

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

        private static List<LatticePoint> LatticePoints(double tolerance, List<Vector2> path, List<Vector2> diff,
            List<double> diffL, List<double> pathL) {
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
            _lattice = LatticePoints(0.35, _path, _diff, _diffL, _pathL); // 0.35 < sqrt(2)/4
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
            _reds = new List<LatticePoint> {_lattice[n]};
            while (t < MaxT) {
                xprev = x;
                t += 0.0025; // 0.25 < 1 - sqrt(2)/2
                x = PositionFunction(t) * _totalPathL;
                if (n > 0 && x - _lattice[n - 1].PathPosition < _lattice[n].PathPosition - x) {
                    n -= 1;
                    _reds.Add(_lattice[n]);
                } else if (n <= _lattice.Count - 2 && _lattice[n + 1].PathPosition - x < x - _lattice[n].PathPosition) {
                    n += 1;
                    _reds.Add(_lattice[n]);
                }

                //if ((x - _lattice[n].x) * (xprev - _lattice[n].x) <= 0) {
                //    _reds[_reds.Count - 1].t = t; // dont use this
                //}
            }

            var i = 0;
            while (i < _reds.Count - 2) {
                // bad algorithm improve later
                i++;
                var rl = _reds[i - 1];
                var rm = _reds[i];
                var rr = _reds[i + 1];
                if (rm.Error < 0.125 || rm.Error < rl.Error && rm.Error < rr.Error) continue;
                if (Math.Abs(rm.ErrorPerp - rl.ErrorPerp - rr.ErrorPerp) > 0.25) {
                    _reds.RemoveAt(i);
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
            for (var n = 0; n < _reds.Count - 1; n++) {
                var a = _reds[n];
                var b = _reds[n + 1];
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
                if (n < _reds.Count - 1 &&
                    Math.Abs(_reds[n + 1].PathPosition - x) < Math.Abs(x - _reds[n].PathPosition)) {
                    d += _whites[n].Length + Math.Round(t - d + _whites[n].Length / 2);
                    _reds[n].Tumours.Add(new Vector2(Math.Round(t - d + _whites[n].Length / 2), 0));
                    n += 1;
                }
            }
        }

        private static List<Vector2> AnchorsList(List<LatticePoint> reds, List<Interpolation> whites) {
            var anchors = new List<Vector2>();

            for (var n = 0; n < reds.Count; n++) {
                foreach (var t in reds[n].Tumours) {
                    anchors.Add(reds[n].Pos);
                    anchors.Add(reds[n].Pos + t);
                    anchors.Add(reds[n].Pos);
                }
                if (n < whites.Count) {
                    anchors.Add(whites[n].StartPos);
                foreach (var a in whites[n].Anchors)
                    anchors.Add(a);
                anchors.Add(whites[n].EndPos);
                }
            }

            return anchors;
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            GetReds();
            GetInterpolation();
            GetTumours();
            return AnchorsList(_reds, _whites);
        }

        internal struct LatticePoint {
            public Vector2 Pos; // lattice point
            public Vector2 PathPoint; // point on path
            public double PathPosition; // position along path
            public double Error; // error ||k - p||
            public double ErrorPerp; // perpendicular error (positive = right handed)
            public int SegmentIndex; // segment index
            public double Time; // placeholder
            public double Length; // placeholder
            public List<Vector2> Tumours; // tumour vectors (offset from main anchor)

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
                Tumours = new List<Vector2>();
            }
        }

        internal struct Interpolation {
            public Vector2 StartPos; // start lattice point
            public Vector2 EndPos; // end lattice point
            public Vector2 StartPathPoint; // start point on path
            public Vector2 EndPathPoint; // end point on path
            public double Length; // length
            public double Error; // error
            public Vector2[] Anchors; // anchors

            public Interpolation(LatticePoint startLatticePoint, LatticePoint endLatticePoint) {
                StartPos = startLatticePoint.Pos;
                EndPos = endLatticePoint.Pos;
                StartPathPoint = startLatticePoint.PathPoint;
                EndPathPoint = endLatticePoint.PathPoint;
                Length = (EndPos - StartPos).Length;
                Error = 0;
                Anchors = new Vector2[0];
            }
        }

    }
}