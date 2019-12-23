using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools {
    public class Sliderator {
        private List<Vector2> _diff;
        private List<double> _diffL;
        private List<double> _interpL;
        private List<LatticePoint> _lattice;

        private List<Vector2> _path;
        private List<double> _pathL;
        private List<LatticePoint> _reds;
        private List<List<Tumour>> _tumours;
        private List<Interpolation> _whites;

        private static double Tmax => 1000; // TODO: Connect this to UI


        private static double X(double t) {
            return t - Math.Pow(t, 2) / 400 +
                   Math.Pow(t, 3) / 600000; // t from 0 to 1000, x from 0 to 166.7,  max slope 1, has reverse
        }

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
            for (var n = 0; n < diff.Count; n++) {
                var l = diffL[n];
                if (Math.Abs(l) < Precision.DOUBLE_EPSILON) continue;
                var a = path[n];
                var d = diff[n];
                var l2 = d.LengthSquared;
                var ax = Math.Abs(d[0]) < Math.Abs(d[1]) ? 1 : 0; // axis to iterate along
                var dr = Math.Sign(d[ax]); // direction of iteration
                for (var i = (int) Math.Round(a[ax]); i != (int) Math.Round(a[ax] + d[ax]) + dr; i += dr) {
                    var s = (i - a[ax]) / d[ax];
                    var r = a[1 - ax] + s * d[1 - ax];
                    var j = (int) Math.Round(r);
                    var k = ax == 1 ? new Vector2(j, i) : new Vector2(i, j);
                    var t = MathHelper.Clamp(s + (j - r) * d[1 - ax] / l2, 0, 1);
                    var p = a + t * d;
                    var x = pathL[n] + t * l;
                    var e = (k - p).Length;
                    var es = Math.Sign((j - r) * dr * (1 - 2 * ax));
                    if (!(e < tolerance)) continue; // close enough
                    if (Math.Abs(t - 1) < Precision.DOUBLE_EPSILON && n + 1 < diff.Count) continue;
                    if (lattice.Count > 0 && k == lattice.Last().Pos) {
                        // repeated point
                        if (e <= lattice.Last().Error)
                            lattice[lattice.Count - 1] = new LatticePoint(k, p, x, 0, e, es, n);
                    } else {
                        lattice.Add(new LatticePoint(k, p, x, 0, e, es, n));
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
            var x = X(t);
            var xprev = x;
            var n = _lattice.Select(y => y.PathPosition).ToList().BinarySearch(x);
            if (n < 0) n = ~n;
            n = n != 0 && (n == _lattice.Count || x - _lattice[n - 1].PathPosition <= _lattice[n].PathPosition - x)
                ? n - 1
                : n; // closer to left?
            _reds = new List<LatticePoint> {_lattice[n]};
            while (t < Tmax) {
                xprev = x;
                t += 0.25; // 0.25 < 1 - sqrt(2)/2
                x = X(t);
                if (n != 0 && x - _lattice[n - 1].PathPosition < _lattice[n].PathPosition - x) {
                    n -= 1;
                    _reds.Add(_lattice[n]);
                } else if (n != _lattice.Count && _lattice[n + 1].PathPosition - x < x - _lattice[n].PathPosition) {
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
                if (Math.Abs(rm.Error * rm.ErrorSide - rl.Error * rl.ErrorSide - rr.Error * rr.ErrorSide) > 0.25) {
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
            _tumours = new List<List<Tumour>>();
            double t = 0;
            double d = 0;
            var x = X(t);
            var n = 0;
            while (t < Tmax) {
                t += 0.25;
                x = X(t);
                if (n < _reds.Count - 1 &&
                    Math.Abs(_reds[n + 1].PathPosition - x) < Math.Abs(x - _reds[n].PathPosition)) {
                    d += _whites[n].Length + Math.Round(t - d + _whites[n].Length / 2);
                    var redLatticePoint = _reds[n];
                    var whiteLatticePoint = redLatticePoint;
                    whiteLatticePoint.Pos =
                        redLatticePoint.Pos + new Vector2(Math.Round(t - d + _whites[n].Length / 2), 0);
                    _tumours.Add(new List<Tumour> {
                        new Tumour(redLatticePoint, whiteLatticePoint)
                    });
                    n += 1;
                }
            }
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            GetReds();
            GetInterpolation();
            GetTumours();
            var anchors = new List<Vector2>();
            for (var n = 0; n < _reds.Count - 1; n++) {
                anchors.Add(_reds[n].Pos);
                foreach (var t in _tumours[n].Where(t => t.RedLatticePoint.Pos != t.WhiteLatticePoint.Pos))
                    anchors.AddRange(new[] {t.WhiteLatticePoint.Pos, t.RedLatticePoint.Pos, t.RedLatticePoint.Pos});
                anchors.Add(_whites[n].StartLatticePoint.Pos);
                anchors.Add(_whites[n].EndLatticePoint.Pos);
                anchors.Add(_reds[n + 1].Pos);
            }

            return anchors;
        }

        internal struct LatticePoint {
            public Vector2 Pos; // lattice point
            public Vector2 PathPoint; // point on path
            public double PathPosition; // position along path
            public double Time; // time (when the slider passes it)
            public double Error; // error ||k - p||
            public int ErrorSide; // error side (1 = right handed)
            public int SegmentIndex; // segment index

            public LatticePoint(Vector2 pos, Vector2 pathPoint, double pathPosition, double time, double error,
                int errorSide, int segmentIndex) {
                Pos = pos;
                PathPoint = pathPoint;
                PathPosition = pathPosition;
                Time = time;
                Error = error;
                ErrorSide = errorSide;
                SegmentIndex = segmentIndex;
            }
        }

        internal struct Interpolation {
            public Vector2 StartPathPoint; // start point on path
            public Vector2 EndPathPoint; // end point on path
            public LatticePoint StartLatticePoint; // start lattice point
            public LatticePoint EndLatticePoint; // end lattice point
            public double Length; // length
            public double Error; // error
            public Vector2[] Anchors; // anchors

            public Interpolation(LatticePoint startLatticePoint, LatticePoint endLatticePoint) {
                StartPathPoint = startLatticePoint.PathPoint;
                EndPathPoint = endLatticePoint.PathPoint;
                StartLatticePoint = startLatticePoint;
                EndLatticePoint = endLatticePoint;
                Length = (EndLatticePoint.Pos - StartLatticePoint.Pos)
                    .Length; // replace with path approximator on {c, k, d} eventually
                Error = 0;
                Anchors = null;
            }
        }

        internal struct Tumour {
            public LatticePoint RedLatticePoint; // red lattice point
            public LatticePoint WhiteLatticePoint; // white lattice point
            public double Length; // length

            public Tumour(LatticePoint redLatticePoint, LatticePoint whiteLatticePoint) {
                RedLatticePoint = redLatticePoint;
                WhiteLatticePoint = whiteLatticePoint;
                Length = (WhiteLatticePoint.Pos - RedLatticePoint.Pos).Length;
            }
        }
    }
}