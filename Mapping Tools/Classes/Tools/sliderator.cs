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

        private LatticePoint GetNearestLatticePoint(double progression) {
            LatticePoint nearest = null;
            double bestError = double.PositiveInfinity;

            foreach (var latticePoint in _lattice) {
                // Simple squared sum error calculation with path position and perpendicular error
                var error = Math.Pow(latticePoint.PathPosition - progression, 2) + Math.Pow(latticePoint.ErrorPerp, 2);
                if (!(error < bestError)) continue;
                bestError = error;
                nearest = latticePoint;
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
            const double maxOvershot = 6;  // Max error in wantedLength
            const double epsilon = 0.01;  // Resolution for for speed differentiation
            const double deltaT = 0.01;  // Size of time step

            _slider = new List<Neuron>();

            double actualLength = 0;
            double nucleusTime = 0;
            double nucleusWantedLength = 0;
            int lastDirection = 1;
            Neuron currentNeuron = new Neuron(_lattice.First(), 0);
            for (double t = 0; t <= MaxT; t += deltaT) {
                var time = Math.Min(t, MaxT);
                var wantedLength = PositionFunction(time);  // Input is time in milliseconds and output is position in osu! pixels
                // var wantedPosition = PositionAt(wantedLength);

                var speed = (PositionFunction(time + epsilon) - wantedLength) / epsilon;
                var direction = Math.Sign(speed);
                var velocity = Math.Abs(speed);

                var nearestLatticePoint = GetNearestLatticePoint(wantedLength);

                if (nearestLatticePoint == null) {
                    throw new InvalidOperationException($"Can not find nearest lattice point for length ({wantedLength}).");
                }

                // Make a new neuron if the path turns around
                // The position of this turn-around is not entirely accurate because the actual turn-around happens somewhere in between the time steps
                // This is the cause behind most of the error compared to the expected total length
                if (direction != lastDirection) {
                    var newNeuron = new Neuron(nearestLatticePoint, time);
                    currentNeuron.Terminal = newNeuron;

                    currentNeuron.WantedLength = actualLength;
                    _slider.Add(currentNeuron);

                    currentNeuron = newNeuron;
                    nucleusWantedLength = wantedLength;
                    nucleusTime = time - deltaT;  // This subtraction is very important because this nucleus reset happens before actualLength gets calculated
                }

                actualLength = (time - nucleusTime) * Velocity;

                // Make a new neuron when the error in the length becomes too large
                if (Math.Abs(Math.Abs(wantedLength - nucleusWantedLength) - actualLength) > maxOvershot * velocity + currentNeuron.Error
                    && nearestLatticePoint != currentNeuron.Nucleus) {
                    var newNeuron = new Neuron(nearestLatticePoint, time);
                    currentNeuron.Terminal = newNeuron;

                    currentNeuron.WantedLength = actualLength;
                    _slider.Add(currentNeuron);

                    currentNeuron = newNeuron;
                    nucleusWantedLength = wantedLength;
                    nucleusTime = time;
                }

                lastDirection = direction;
            }
            // Need to add currentNeuron at the end otherwise the last neuron would get ignored
            currentNeuron.WantedLength = actualLength;
            currentNeuron.Terminal = new Neuron(GetNearestLatticePoint(PositionFunction(MaxT)), MaxT);
            _slider.Add(currentNeuron);

            double totalWantedLength = _slider.Sum(n => n.WantedLength);
            Console.WriteLine(@"Total wanted length: " + totalWantedLength);

            // Multiply with ratio to exactly match the expected total length
            var ratio = _MaxS / totalWantedLength;
            foreach (var neuron in _slider) {
                neuron.WantedLength *= ratio;
            }

            totalWantedLength = _slider.Sum(n => n.WantedLength);
            Console.WriteLine(@"Total wanted length after scale: " + totalWantedLength);
            Console.WriteLine(@"Expected total wanted length: " + _MaxS);

            Console.WriteLine(@"Number of neurons: " + _slider.Count);
        }

        private void GenerateAxons() {
            // Generate bezier points that approximate the paths between neurons
            foreach (var neuron in _slider.Where(n => n.Terminal != null)) {
                var firstPoint = neuron.Nucleus.Pos;
                var lastPoint = neuron.Terminal.Nucleus.Pos;
                var middlePoint = PositionAt((neuron.Nucleus.PathPosition + neuron.Terminal.Nucleus.PathPosition) / 2);
                var flatness = new BezierSubdivision(new List<Vector2> {firstPoint, middlePoint, lastPoint}).Flatness();

                double length;
                if (flatness < 0.25) {
                    neuron.Axon = new BezierSubdivision(new List<Vector2> {firstPoint, lastPoint});
                    length = Vector2.Distance(firstPoint, lastPoint);
                } else {
                    var middleFlatPoint = (firstPoint + lastPoint) / 2;
                    var newMiddlePoint = middleFlatPoint + (middlePoint - middleFlatPoint) * 2;

                    neuron.Axon = new BezierSubdivision(new List<Vector2> {firstPoint, newMiddlePoint, lastPoint});
                    length = neuron.Axon.SubdividedApproximationLength();
                }

                // Calculate lengths
                neuron.AxonLenth = length;
                neuron.DendriteLength = neuron.WantedLength - neuron.AxonLenth;
            }
        }

        private void GenerateDendrites() {
            double leftovers = 0;
            foreach (var neuron in _slider.Where(n => n.Terminal != null)) {
                var dendriteToAdd = neuron.DendriteLength + leftovers;
                while (dendriteToAdd > 1) {
                    var size = MathHelper.Clamp(Math.Floor(dendriteToAdd), 1, Math.Min(neuron.AxonLenth * 2, 12));
                    neuron.Dendrites.Add(new Vector2(size, 0));
                    dendriteToAdd -= size;
                }

                leftovers = dendriteToAdd;
            }
        }

        private List<Vector2> AnchorsList() {
            var anchors = new List<Vector2>();
            for (var index = 0; index < _slider.Count; index++) {
                var neuron = _slider[index];

                anchors.Add(neuron.Nucleus.Pos);
                if (index != 0) {
                    anchors.Add(neuron.Nucleus.Pos);
                }

                foreach (var t in neuron.Dendrites) {
                    anchors.Add(neuron.Nucleus.Pos + t);
                    anchors.Add(neuron.Nucleus.Pos);
                    anchors.Add(neuron.Nucleus.Pos);
                }

                anchors.AddRange(neuron.Axon.Points.GetRange(1, neuron.Axon.Points.Count - 2));
                if (index == _slider.Count - 1) {
                    anchors.Add(neuron.Axon.Points.Last());
                }
            }

            return anchors;
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            DoSlideration();
            GenerateAxons();
            GenerateDendrites();
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
            public double Time;

            public Neuron(LatticePoint startLatticePoint, double time) {
                Nucleus = startLatticePoint;
                Dendrites = new List<Vector2>();
                Axon = new BezierSubdivision(new List<Vector2> {startLatticePoint.Pos});
                Terminal = null;
                Length = 0;
                Error = 0;
                WantedLength = 0;
                DendriteLength = 0;
                AxonLenth = 0;
                Time = time;
            }
        }

    }
}