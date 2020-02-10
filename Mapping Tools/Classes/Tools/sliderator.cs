using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.Tools {
    public class Sliderator {
        public delegate double PositionFunctionDelegate(double t);
        public PositionFunctionDelegate PositionFunction { get; set; } // position along slider, ms -> px
        public double MaxT { get; set; } // end time for PositionFunction, in ms
        public double Velocity { get; set; } // slider velocity, in px/ms

        private double _Pos(double s) => PositionFunction(Math.Min(s * Velocity, MaxT)); // normalized position function, px -> px
        private double _MaxS => MaxT * Velocity; // expected pixellength

        private List<Vector2> _path; // input path
        private List<Vector2> _diff; // path segments
        private List<double> _diffL; // length of segments
        private List<double> _pathL; // cumulative length
        private double _totalPathL => _pathL.Last(); // total length

        private List<LatticePoint> _lattice; // path lattice points
        private List<Neuron> _slider; // slider red anchors, tumours and interpolations

        public void SetPath(List<Vector2> pathPoints) {
            _path = new List<Vector2> {pathPoints.First()};
            _diff = new List<Vector2>();
            _diffL = new List<double>();
            double sum = 0;
            _pathL = new List<double> {sum};
            foreach (var p in pathPoints.Skip(1)) {
                var d = p - _path.Last();
                var dl = d.Length;
                if (dl < Precision.DOUBLE_EPSILON) continue;
                _path.Add(p);
                _diff.Add(d);
                _diffL.Add(dl);
                sum += dl;
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

        private LatticePoint GetNearestLatticePoint(double progression, double maxError) {
            LatticePoint nearest = null;
            double bestDist = double.PositiveInfinity;

            // TODO: Might want to change this for when there are no close lattice points with low enough error
            foreach (var latticePoint in _lattice.Where(l => l.Error <= maxError)) {
                var dist = Math.Abs(latticePoint.PathPosition - progression);
                if (dist < bestDist) {
                    bestDist = dist;
                    nearest = latticePoint;
                } else {
                    return nearest;
                }
            }

            return nearest;
        }

        private double NextCrossing(double start, double low, double high, out int side, double precision = 0.01, double resolution = 0.25) { // where it next crosses below a lower bound or above an upper bound
            double s = start;
            double ds = resolution;
            double x;
            do {
                x = _Pos(s + ds);
                if (low <= x && x <= high)
                    s += ds;
                else
                    ds /= 2;
            } while (ds >= precision && s < _MaxS);

            if (x > high) side = 1;
            else if (x < low) side = -1;
            else side = 0;
            return Math.Min(s + ds, _MaxS);
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

        private void GetReds() {
        }

        private void GetInterpolation() {
        }

        private void GetTumours() {
        }

        private void DoSlideration() {
            // These values are placeholders. Experimentation has to be done to find better parameters
            const double maxOvershot = 12;  // Max error in wantedLength
            const double maxVisualError = 1;  // Max error in lattice points
            const double epsilon = 0.01;  // Resolution for for speed differentiation
            const double deltaT = 0.01;  // Size of time step

            _slider = new List<Neuron>();

            double actualLength = 0;
            double nucleusTime = 0;
            int lastDirection = 1;
            Neuron currentNeuron = new Neuron(_lattice.First());
            for (double t = 0; t <= MaxT; t += deltaT) {
                var time = Math.Min(t, MaxT);
                var wantedProgression = PositionFunction(time);
                var wantedLength = wantedProgression * _totalPathL;
                // var wantedPosition = PositionAt(wantedLength);

                var speed = (PositionFunction(time + epsilon) * _totalPathL - wantedLength) / epsilon;
                var direction = Math.Sign(speed);
                var velocity = Math.Abs(speed);

                var nearestLatticePoint = GetNearestLatticePoint(wantedProgression, maxVisualError);

                if (nearestLatticePoint == null) {
                    throw new InvalidOperationException($"Can not find nearest lattice point for progression ({wantedProgression}) and maxError ({maxVisualError * velocity}).");
                }

                // Make a new neuron if the path turns around
                // The position of this turn-around is not entirely accurate because the actual turn-around happens somewhere in between the time steps
                // This is the cause behind most of the error compared to the expected total length
                if (direction != lastDirection) {
                    var newNeuron = new Neuron(nearestLatticePoint);
                    currentNeuron.Terminal = newNeuron;

                    currentNeuron.WantedLength = actualLength;
                    _slider.Add(currentNeuron);

                    currentNeuron = newNeuron;
                    nucleusTime = time - deltaT;  // This subtraction is very important because this nucleus reset happens before actualLength gets calculated
                }

                actualLength = (time - nucleusTime) * Velocity;

                // Make a new neuron when the error in the length becomes too large
                if (Math.Abs(Math.Abs(wantedLength - currentNeuron.Nucleus.PathPosition * _totalPathL) - actualLength) > maxOvershot * velocity) {
                    var newNeuron = new Neuron(nearestLatticePoint);
                    currentNeuron.Terminal = newNeuron;

                    currentNeuron.WantedLength = actualLength;
                    _slider.Add(currentNeuron);

                    currentNeuron = newNeuron;
                    nucleusTime = time;
                }

                lastDirection = direction;
            }
            // Need to add currentNeuron at the end otherwise the last neuron would get ignored
            currentNeuron.WantedLength = actualLength;
            _slider.Add(currentNeuron);

            double totalWantedLength = _slider.Sum(n => n.WantedLength);
            Console.WriteLine(@"Total wanted length: " + totalWantedLength);
            Console.WriteLine(@"Expected total wanted length: " + _MaxS);

            // Multiply with ratio to exactly match the expected total length
            var ratio = _MaxS / totalWantedLength;
            foreach (var neuron in _slider) {
                neuron.WantedLength *= ratio;
            }

            totalWantedLength = _slider.Sum(n => n.WantedLength);
            Console.WriteLine(@"Total wanted length after scale: " + totalWantedLength);
            Console.WriteLine(@"Expected total wanted length: " + _MaxS);
        }

        private List<Vector2> AnchorsList() {
            var anchors = new List<Vector2>();
            for (int n = 0; n < _slider.Count; n++) {
                foreach (var t in _slider[n].Dendrites) {
                    anchors.Add(_slider[n].Nucleus.Pos);
                    anchors.Add(_slider[n].Nucleus.Pos + t);
                    anchors.Add(_slider[n].Nucleus.Pos);
                }
                anchors.AddRange(_slider[n].Axon.Points);
            }
            return anchors;
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            DoSlideration();
            return AnchorsList();
        }

        internal class LatticePoint {
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
            public double WantedLength;
            public double DendriteLength;
            public double AxonLenth;

            public Neuron(LatticePoint startLatticePoint) {
                Nucleus = startLatticePoint;
                Dendrites = new List<Vector2>();
                Axon = new BezierSubdivision(new List<Vector2> {startLatticePoint.Pos});
                Terminal = null;
                Length = 0;
                Error = 0;
                WantedLength = 0;
                DendriteLength = 0;
                AxonLenth = 0;
            }
        }

    }
}