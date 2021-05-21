using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public class Sliderator {
        public PositionFunctionDelegate PositionFunction { get; set; } // position along slider, ms -> px
        public double MaxT { get; set; } // end time for PositionFunction, in ms
        public double Velocity { get; set; } // slider velocity, in px/ms

        public double MinDendriteLength { get; set; } = 1;

        private double _Pos(double s) => PositionFunction(Math.Min(s * Velocity, MaxT)); // normalized position function, px -> px
        public double MaxS => MaxT * Velocity; // expected pixellength

        private List<Vector2> _path; // input path
        private List<Vector2> _diff; // path segments
        private List<double> _angle; // path segment angles
        private List<double> _diffL; // length of segments
        private List<double> _pathL; // cumulative length
        private double _totalPathL => _pathL.Last(); // total length

        private List<LatticePoint> _lattice; // path lattice points
        private List<Neuron> _slider; // slider red anchors, tumours and interpolations

        public void SetPath(List<Vector2> pathPoints) {
            _path = new List<Vector2> {pathPoints.First()};
            _diff = new List<Vector2>();
            _angle = new List<double>();
            _diffL = new List<double>();
            double sum = 0;
            _pathL = new List<double> {sum};
            foreach (var p in pathPoints.Skip(1)) {
                var d = p - _path.Last();
                var dl = d.Length;
                if (dl < Precision.DOUBLE_EPSILON) continue;
                _path.Add(p);
                _diff.Add(d);
                _angle.Add(d.Theta);
                _diffL.Add(dl);
                sum += dl;
                _pathL.Add(sum);
            }

            if (Math.Abs(sum) < Precision.DOUBLE_EPSILON) throw new InvalidOperationException("Zero length path.");

            // Add last member again so these lists have the same number of elements as path
            _diff.Add(_diff.Last());
            _angle.Add(_angle.Last());
            _diffL.Add(_diffL.Last());
        }

        private Vector2 PositionAt(double x) {
            int n = _pathL.BinarySearch(x);
            if (n < 0) n = ~n - 1;
            if (n == -1) n += 1;
            if (n == _diff.Count) n -= 1;
            return _path[n] + _diff[n] / _diffL[n] * (x - _pathL[n]);
        }

        private LatticePoint GetNearestLatticePoint(double pathPosition) {
            int l = 0;
            int r = _lattice.Count - 1;

            while (r - l > 1) {
                var i = (l + r) / 2;
                if (_lattice[i].PathPosition > pathPosition) {
                    r = i;
                } else {
                    l = i;
                }
            }

            return Math.Abs(pathPosition - _lattice[l].PathPosition) < Math.Abs(pathPosition - _lattice[r].PathPosition)
                ? _lattice[l]
                : _lattice[r];
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
            } while (ds >= precision && s < MaxS);

            if (x > high) side = 1;
            else if (x < low) side = -1;
            else side = 0;
            return Math.Min(s + ds, MaxS);
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

            for (int n = 0; n < diff.Count - 1; n++) { // iterate through path segments -1 because the last one is repeated
                var l = diffL[n]; // segment length
                if (Math.Abs(l) < Precision.DOUBLE_EPSILON) continue; // skip segment if degenerate
                var a = path[n]; // start point
                var d = diff[n]; // vector to end point
                var l2 = d.LengthSquared;
                var ax = Math.Abs(d[0]) < Math.Abs(d[1]) ? 1 : 0; // axis to iterate along
                var dr = Math.Sign(d[ax]); // direction of iteration
                for (int i = (int) Math.Round(a[ax]); i != (int) Math.Round(a[ax] + d[ax]) + dr; i += dr) { // major axis lattice coordinate
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
                            lattice[lattice.Count - 1] = new LatticePoint(k, p, x, e, ep, n, t);
                    } else {
                        lattice.Add(new LatticePoint(k, p, x, e, ep, n, t));
                    }
                }
            }

            return lattice;
        }

        private void GetLatticePoints() {
            _lattice = LatticePoints(_path, _diff, _diffL, _pathL, 0.35);
        }

        private double GetSpeedAtTime(double time, double epsilon) {
            return (PositionFunction(time + epsilon) - PositionFunction(time)) / epsilon;
        }

        private void GenerateNeurons() {
            // These values are placeholders. Experimentation has to be done to find better parameters
            const double maxOvershot = 32;  // Max error in wantedLength
            const double epsilon = 0.01;  // Resolution for for speed differentiation
            const double deltaT = 0.02;  // Size of time step

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

                // Make a new neuron if the path turns around
                // The position of this turn-around is not entirely accurate because the actual turn-around happens somewhere in between the time steps
                // This is the cause behind most of the error compared to the expected total length
                if (direction * lastDirection < 0) {  // Do this to exclude 0 direction sign
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
                var lengthError = Math.Abs(Math.Abs(wantedLength - nucleusWantedLength) - actualLength) - currentNeuron.Error;

                if (lengthError > Math.Max(MinDendriteLength, velocity * maxOvershot) || 
                    nearestLatticePoint.Error < 0.05 && lengthError > Math.Max(MinDendriteLength, velocity * MinDendriteLength)) {
                    if ((nearestLatticePoint.Pos - currentNeuron.Nucleus.Pos).LengthSquared > 0.1) {
                        var newNeuron = new Neuron(nearestLatticePoint, time);
                        currentNeuron.Terminal = newNeuron;

                        currentNeuron.WantedLength += actualLength;
                        _slider.Add(currentNeuron);

                        currentNeuron = newNeuron;
                        nucleusWantedLength = wantedLength;
                        nucleusTime = time;
                    } else {
                        // Pretend to add a new neuron but merged with the current one
                        currentNeuron.WantedLength += actualLength;

                        nucleusWantedLength = wantedLength;
                        nucleusTime = time;
                    }
                }

                lastDirection = direction == 0 ? lastDirection : direction;  // Not update last direction if direction is 0
            }
            // Need to add currentNeuron at the end otherwise the last neuron would get ignored
            currentNeuron.WantedLength += actualLength;
            var lastNeuron = new Neuron(GetNearestLatticePoint(PositionFunction(MaxT)), MaxT);
            currentNeuron.Terminal = lastNeuron;
            _slider.Add(currentNeuron);
            _slider.Add(lastNeuron);

            double totalWantedLength = _slider.Sum(n => n.WantedLength);
            //Console.WriteLine(@"Total wanted length: " + totalWantedLength);

            // Multiply with ratio to exactly match the expected total length
            var ratio = MaxS / totalWantedLength;
            foreach (var neuron in _slider) {
                neuron.WantedLength *= ratio;
            }

            //totalWantedLength = _slider.Sum(n => n.WantedLength);
            //Console.WriteLine(@"Total wanted length after scale: " + totalWantedLength);
            //Console.WriteLine(@"Expected total wanted length: " + MaxS);

            //Console.WriteLine(@"Number of neurons: " + _slider.Count);
            //foreach (var neuron in _slider) {
            //    Console.WriteLine(neuron.Nucleus.Pos);
            //    Console.WriteLine(neuron.WantedLength);
            //}
        }

        private void GenerateAxons() {
            var pathGenerator = new PathGenerator(_path, _diff, _angle, _diffL, _pathL);

            // Generate bezier points that approximate the paths between neurons
            foreach (var neuron in _slider.Where(n => n.Terminal != null)) {
                var firstPoint = neuron.Nucleus.Pos;
                var lastPoint = neuron.Terminal.Nucleus.Pos;

               var path = pathGenerator.GeneratePath(neuron.Nucleus.SegmentIndex + neuron.Nucleus.SegmentProgress,
                    neuron.Terminal.Nucleus.SegmentIndex + neuron.Terminal.Nucleus.SegmentProgress).ToList();

                if (path.Count < 2) {
                    path = new List<Vector2> {firstPoint, lastPoint};
                } else {
                    path[0] = firstPoint;
                    path[path.Count - 1] = lastPoint;
                }

                neuron.Axon = new BezierSubdivision(path);

                // Calculate lengths
                neuron.AxonLenth = PathGenerator.CalculatePathLength(path);
                neuron.DendriteLength = neuron.WantedLength - neuron.AxonLenth;
            }
        }

        private Vector2 NearbyNonZeroDiff(int index) {
            Vector2 diff = Vector2.UnitX;
            for (int i = 0; i < 10; i++) {
                diff = _diff[MathHelper.Clamp(index + i, 0, _diff.Count - 1)];
                if (Math.Abs(diff.X) > Precision.DOUBLE_EPSILON || Math.Abs(diff.Y) > Precision.DOUBLE_EPSILON) {
                    return diff;
                }
            }

            return diff;
        }

        private void GenerateDendrites() {
            double leftovers = 0;
            //double totalDendriteToAddLength = 0;
            foreach (var neuron in _slider.Where(n => n.Terminal != null)) {
                // Find angles for the neuron and the terminal to point the dendrites towards
                var dir = Math.Sign(neuron.Terminal.Nucleus.PathPosition - neuron.Nucleus.PathPosition);
                dir = dir == 0 ? 1 : dir;  // Let dir not be zero

                var dendriteDir1 = dir * NearbyNonZeroDiff(neuron.Nucleus.SegmentIndex).Normalized();
                var dendriteDir2 = -dir * NearbyNonZeroDiff(neuron.Terminal.Nucleus.SegmentIndex).Normalized();

                // Do an even split of dendrites between this neuron and the terminal
                var dendriteToAdd = neuron.DendriteLength + leftovers;

                // Find the time at which the position function goes in between the neuron and the terminal
                var width = neuron.Terminal.Time - neuron.Time;
                var axonWidth = neuron.AxonLenth / Velocity;
                var middleTime = BinarySearchUtil.DoubleBinarySearch(neuron.Time, neuron.Terminal.Time, 0.01,
                    d => PositionFunction(d) <= (neuron.Nucleus.PathPosition + neuron.Terminal.Nucleus.PathPosition) / 2);

                // Calculate the distribution of dendrites to let the axon pass through the middle at the same time as the position funciton does
                var leftPortion = MathHelper.Clamp((2 * (middleTime - neuron.Time) - axonWidth) / (2 * (width - axonWidth)), 0, 1);
                var rightPortion = 1 - leftPortion;

                var dendriteToAddLeft = dendriteToAdd * leftPortion;
                var dendriteToAddRight = dendriteToAdd * rightPortion;

                //totalDendriteToAddLength += dendriteToAddLeft;
                //totalDendriteToAddLength += dendriteToAddRight;

                // Get the speeds at the times of the dendrites to give the dendrites appriopriate lengths to the speed at the time
                var speedLeft = GetSpeedAtTime(neuron.Time + dendriteToAddLeft / Velocity / 2, 0.01);
                var speedRight = GetSpeedAtTime(neuron.Terminal.Time - dendriteToAddRight / Velocity / 2, 0.01);

                //Console.WriteLine($@"Adding {dendriteToAddLeft + dendriteToAddRight} length to dendrites!");
                //var beforeLength = neuron.Dendrites.Sum(d => d.Length) + neuron.Terminal.Dendrites.Sum(d => d.Length);

                dendriteToAddRight += AddDendriteLength(neuron, dendriteToAddLeft, dendriteDir1, MinDendriteLength, 4 * Math.Pow(speedLeft * 2, 2));
                leftovers = AddDendriteLength(neuron.Terminal, dendriteToAddRight, dendriteDir2, MinDendriteLength, 4 * Math.Pow(speedRight * 2, 2));

                //var afterLength = neuron.Dendrites.Sum(d => d.Length) + neuron.Terminal.Dendrites.Sum(d => d.Length);
                //Console.WriteLine($@"Actually added {afterLength - beforeLength} length to dendrites and got {leftovers} leftover!");
            }

            /*double actualLength = 0;
            double wantedLength = 0;
            double axonLength = 0;
            double dendriteLength = 0;
            foreach (var n in _slider) {
                actualLength += n.AxonLenth + n.Dendrites.Sum(d => d.Length);
                wantedLength += n.WantedLength;
                axonLength += n.AxonLenth;
                dendriteLength += n.DendriteLength;
            }
            Console.WriteLine("Path length: " + actualLength);
            Console.WriteLine("Wanted length: " + wantedLength);
            Console.WriteLine("Axon length: " + axonLength);
            Console.WriteLine("Dendrite length: " + dendriteLength);
            Console.WriteLine("DendriteToAdd length: " + totalDendriteToAddLength);*/
        }
        
        private static double AddDendriteLength(Neuron neuron, double length, Vector2 dir, double minLength, double maxLength) {
            while (length > minLength) {
                var size = MathHelper.Clamp(Math.Floor(length), Math.Max(minLength, 1), Math.Min(maxLength, 12));

                var dendrite = (dir * size).Rounded();
                var dendriteLength = dendrite.Length;

                // Shorten dendrites longer than 12 pixels to keep dendrites invisible
                while (dendriteLength > 12) {
                    size -= 0.5;
                    dendrite = (dir * size).Rounded();
                    dendriteLength = dendrite.Length;
                }

                // Prevent any dendrites shorter than 1 to never get an infinite loop
                if (dendriteLength < 1) {
                    dendrite = Vector2.UnitX;
                }

                neuron.Dendrites.Add(dendrite);
                length -= dendrite.Length;
            }

            return length;
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

                if (index != _slider.Count - 1) {
                    anchors.AddRange(neuron.Axon.Points.GetRange(1, neuron.Axon.Points.Count - 2));
                }
            }

            anchors.RemoveAt(anchors.Count - 1);

            return anchors;
        }

        public List<Vector2> Sliderate() {
            GetLatticePoints();
            GenerateNeurons();
            GenerateAxons();
            GenerateDendrites();
            return AnchorsList();
        }
         
        public List<Vector2> SliderateStream(double deltaT) {
            var points = new List<Vector2>();

            for (double t = 0; t <= MaxT + Precision.DOUBLE_EPSILON; t += deltaT) {
                points.Add(PositionAt(PositionFunction(t)).Rounded());
            }

            return points;
        }

        internal class LatticePoint {
            public Vector2 Pos; // lattice point
            public Vector2 PathPoint; // point on path
            public double PathPosition; // position along path
            public double Error; // error ||Pos - PathPoint||
            public double ErrorPerp; // perpendicular error (positive = right handed)
            public int SegmentIndex; // segment index
            public double SegmentProgress; // projected progress on segment
            public double Time; // placeholder
            public double Length; // placeholder

            public LatticePoint(Vector2 pos, Vector2 pathPoint, double pathPosition, double error,
                double errorPerp, int segmentIndex, double segmentProgress) {
                Pos = pos;
                PathPoint = pathPoint;
                PathPosition = pathPosition;
                Error = error;
                ErrorPerp = errorPerp;
                SegmentIndex = segmentIndex;
                SegmentProgress = segmentProgress;
                Time = 0;
                Length = 0;
            }

            public int NearestPathIndex => SegmentProgress > 0.5 ? SegmentIndex + 1 : SegmentIndex;
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
