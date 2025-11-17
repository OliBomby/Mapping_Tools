using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.ToolHelpers;

/// <summary>
/// This class can generate bezier anchors which approximate arbitrary paths
/// </summary>
public class PathGenerator {
    private List<Vector2> _pathPoints = null!; // input path
    private List<Vector2> _deltas = null!; // path segments
    private List<double> _angles = null!; // path segment angles
    private List<double> _deltaLengths = null!; // length of segments
    private List<double> _cumulativeLengths = null!; // cumulative length

    public PathGenerator(List<Vector2> newPathPoints) {
        SetPath(newPathPoints);
    }

    public void SetPath(List<Vector2> newPathPoints) {
        _pathPoints = [newPathPoints[0]];
        _deltas = [];
        _angles = [];
        _deltaLengths = [];
        double sum = 0;
        _cumulativeLengths = [sum];

        foreach (var point in newPathPoints.Skip(1)) {
            var delta = point - _pathPoints.Last();
            var dist = delta.Length;

            if (dist < Precision.DoubleEpsilon) continue;

            _pathPoints.Add(point);
            _deltas.Add(delta);
            _angles.Add(delta.Theta);
            _deltaLengths.Add(dist);
            sum += dist;
            _cumulativeLengths.Add(sum);
        }

        // Add last member again so these lists have the same number of elements as path
        _deltas.Add(_deltas.Last());
        _angles.Add(_angles.Last());
        _deltaLengths.Add(_deltaLengths.Last());
    }

    /// <summary>
    /// Generates anchors which approximate the entire path
    /// </summary>
    /// <param name="maxAngle"></param>
    /// <returns></returns>
    public IEnumerable<Vector2> GeneratePath(double maxAngle = Math.PI * 1 / 4) {
        return GeneratePath(0, _pathPoints.Count - 1, maxAngle);
    }

    /// <summary>
    /// Generates anchors which approximate the path between the given indices
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="maxAngle"></param>
    /// <param name="approximationMode"></param>
    /// <returns></returns>
    public IEnumerable<Vector2> GeneratePath(double startIndex, double endIndex,
        double maxAngle = Math.PI * 1 / 4, ApproximationMode approximationMode = ApproximationMode.Best) {
        var segments = GetNonInflectionSegments(startIndex, endIndex, maxAngle);

        foreach (var segment in segments) {
            var p1 = GetContinuousPosition(segment.Item1);
            var p2 = GetContinuousPosition(segment.Item2);

            yield return p1;

            Vector2? middle;
            switch (approximationMode) {
                case ApproximationMode.TangentIntersection:
                    middle = TangentIntersectionApproximation(segment.Item1, segment.Item2);
                    break;
                case ApproximationMode.DoubleMiddle:
                    middle = DoubleMiddleApproximation(segment.Item1, segment.Item2);
                    break;
                case ApproximationMode.Best:
                    middle = BestApproximation(segment.Item1, segment.Item2);
                    break;
                default:
                    middle = null;
                    break;
            }

            if (middle.HasValue) {
                yield return middle.Value;
            }

            yield return p2;
        }
    }

    private Vector2? BestApproximation(double startIndex, double endIndex) {
        // Make sure start index is before end index
        // The results will be the same for flipped indices
        if (startIndex > endIndex) {
            (endIndex, startIndex) = (startIndex, endIndex);
        }

        var p1 = GetContinuousPosition(startIndex);
        var p2 = GetContinuousPosition(endIndex);

        const int numTestPoints = 100;
        var labels = _pathPoints.GetRange((int) startIndex, (int) Math.Ceiling(endIndex) - (int) startIndex + 1);

        Vector2?[] middles = [
            TangentIntersectionApproximation(startIndex, endIndex),
            DoubleMiddleApproximation(startIndex, endIndex),
        ];

        Vector2? bestMiddle = null;
        double bestLoss = double.PositiveInfinity;

        foreach (var middle in middles) {
            var bezier = new BezierCurveQuadric(p1, p2, middle ?? (p2 - p1) / 2);

            var interpolatedPoints = new Vector2[numTestPoints];
            for (int i = 0; i < numTestPoints; i++) {
                double t = (double) i / (numTestPoints - 1);
                interpolatedPoints[i] = bezier.CalculatePoint(t);
            }

            var loss = SliderPathUtil.CalculateLoss(interpolatedPoints, labels);

            if (!(loss < bestLoss)) {
                continue;
            }

            bestLoss = loss;
            bestMiddle = middle;
        }

        return bestMiddle;
    }

    private Vector2? TangentIntersectionApproximation(double startIndex, double endIndex) {
        var p1 = GetContinuousPosition(startIndex);
        var p2 = GetContinuousPosition(endIndex);

        var a1 = GetContinuousAngle(startIndex);
        var a2 = GetContinuousAngle(endIndex - 2 * Precision.DoubleEpsilon);

        if (Math.Abs(GetSmallestAngle(a1, a2)) < 0.1) {
            return null;
        }

        var t1 = new Line2(p1, a1);
        var t2 = new Line2(p2, a2);

        var middleAnchor = Line2.Intersection(t1, t2);
        if (middleAnchor != Vector2.NaN && Vector2.DistanceSquared(p1, middleAnchor) > 0.5 && Vector2.DistanceSquared(p2, middleAnchor) > 0.5) {
            return middleAnchor;
        }

        return null;
    }

    private Vector2? DoubleMiddleApproximation(double startIndex, double endIndex) {
        var p1 = GetContinuousPosition(startIndex);
        var p2 = GetContinuousPosition(endIndex);

        var d1 = GetContinuousDistance(startIndex);
        var d2 = GetContinuousDistance(endIndex);
        var middleIndex = GetIndexAtDistance((d1 + d2) / 2);

        var averagePoint = (p1 + p2) / 2;
        var middlePoint = GetContinuousPosition(middleIndex);

        if (Vector2.DistanceSquared(averagePoint, middlePoint) < 0.1) {
            return null;
        }

        var doubleMiddlePoint = averagePoint + (middlePoint - averagePoint) * 2;

        return doubleMiddlePoint;
    }

    /// <summary>
    /// Calculates the indices of sub-ranges such that the sub-ranges have no inflection points or sharp curves inside.
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="maxAngle"></param>
    /// <returns></returns>
    public List<Tuple<double, double>> GetNonInflectionSegments(double startIndex, double endIndex, double maxAngle = Math.PI * 1 / 4) {
        int dir = Math.Sign(endIndex - startIndex);

        if (dir == 0) {
            return [new Tuple<double, double>(startIndex, endIndex)];
        }

        // If the direction is reversed, just swap the start and end index and then reverse the result at the end
        if (dir == -1) {
            (endIndex, startIndex) = (startIndex, endIndex);
        }

        endIndex = MathHelper.Clamp(endIndex, 0, _angles.Count - 1);

        int startIndexInt = (int) Math.Ceiling(startIndex);
        int endIndexInt = (int) Math.Floor(endIndex);

        double lastAngleChange = 0;
        var lastAngle = GetContinuousAngle(startIndex);

        double startSubRange = startIndex;
        double subRangeAngleChange = 0;
        List<Tuple<double, double, double>> subRanges = [];
        // Loop through the whole path and divide it into sub-ranges at every inflection point
        //Console.WriteLine($"Iterating from {startIndexInt} to {endIndexInt}");
        for (int i = startIndexInt; i <= endIndexInt; i++) {
            var pos = _pathPoints[i];
            var angle = _angles[i];
            var angleChange = GetSmallestAngle(angle, lastAngle);
            //Console.WriteLine("Angle change: " + angleChange);

            // Check for inflection point or red anchors
            if ((angleChange * lastAngleChange < -Precision.DoubleEpsilon && Math.Abs(startSubRange - i) > 1)
                || ((pos - pos.Rounded()).LengthSquared < Precision.DoubleEpsilon && Math.Abs(angleChange) > Precision.DoubleEpsilon)) {
                subRanges.Add(new Tuple<double, double, double>(startSubRange, i, subRangeAngleChange));

                //Console.WriteLine($"Adding segment for inflection point or red: {startSubRange} to {i}");
                //Console.WriteLine($"Found inflection point or red anchor: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                startSubRange = i;
                subRangeAngleChange = -Math.Abs(angleChange); // Negate the angle change because this point invalidates the angle
            } else if (angleChange == 0 && lastAngleChange != 0) {
                subRanges.Add(new Tuple<double, double, double>(startSubRange, i, subRangeAngleChange));

                //Console.WriteLine($"Adding segment for start zero angle change: {startSubRange} to {i}");
                //Console.WriteLine($"start of zero angle change: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                startSubRange = i;
                subRangeAngleChange = -Math.Abs(angleChange); // Negate the angle change because this point invalidates the angle
            } else if (angleChange != 0 && lastAngleChange == 0 && i - 1 >= startSubRange) {
                // Extra check to prevent subranges going backwards with i - 1
                // Place on the previous index for symmetry with the part going into the zero chain
                subRanges.Add(new Tuple<double, double, double>(startSubRange, i - 1, 0));

                //Console.WriteLine($"Adding segment for end zero angle change: {startSubRange} to {i}");
                //Console.WriteLine($"end of zero angle change: {angleChange}, {lastAngleChange}, {pos}, {angleChange * lastAngleChange}");

                startSubRange = i - 1;
            }

            subRangeAngleChange += Math.Abs(angleChange);

            lastAngle = angle;
            lastAngleChange = angleChange;
        }

        if (Math.Abs(startSubRange - endIndex) > Precision.DoubleEpsilon) {
            subRanges.Add(new Tuple<double, double, double>(startSubRange, endIndex, subRangeAngleChange));
        }

        // Remove all sub-ranges which start and end on the same index or start at a later index
        subRanges.RemoveAll(s => s.Item1 >= s.Item2);

        List<Tuple<double, double>> segments = [];
        // Divide each sub-range into evenly spaced segments which have an aggregate angle change less than the max
        foreach (var subRange in subRanges) {
            int numSegments = (int) Math.Floor(subRange.Item3 / maxAngle) + 1;
            //Console.WriteLine("Num segments: " + numSegments);
            //Console.WriteLine("sub-range angle: " + subRange.Item3);
            double maxSegmentAngle = subRange.Item3 / numSegments;

            int segmentStartIndexInt = (int) Math.Ceiling(subRange.Item1);
            int segmentEndIndexInt = (int) Math.Floor(subRange.Item2);

            lastAngle = GetContinuousAngle(subRange.Item1);

            double startSegment = subRange.Item1;
            double segmentAngleChange = 0;
            // Loop through the sub-range and count the angle change to make even divisions of the angle
            //Console.WriteLine($"Iterating subrange from {segmentStartIndexInt} to {segmentEndIndexInt}");
            for (int i = segmentStartIndexInt; i <= segmentEndIndexInt; i++) {
                var angle = _angles[i];
                var angleChange = GetSmallestAngle(angle, lastAngle);

                segmentAngleChange += Math.Abs(angleChange);

                if (segmentAngleChange > maxSegmentAngle + Precision.DoubleEpsilon) {
                    segments.Add(new Tuple<double, double>(startSegment, i));
                    //Console.WriteLine($"Adding segment for angle: {startSegment} to {i}");

                    startSegment = i;
                    segmentAngleChange -= maxSegmentAngle;
                }

                lastAngle = angle;
            }

            if (Math.Abs(startSegment - subRange.Item2) > Precision.DoubleEpsilon) {
                segments.Add(new Tuple<double, double>(startSegment, subRange.Item2));
                //Console.WriteLine($"Adding segment at the end: {startSegment}, {subRange.Item2}");
            }
        }

        // Reverse the result
        if (dir == -1) {
            List<Tuple<double, double>> reversedSegments = new List<Tuple<double, double>>(segments.Count);

            for (int i = segments.Count - 1; i >= 0; i--) {
                var s = segments[i];
                reversedSegments.Add(new Tuple<double, double>(s.Item2, s.Item1));
            }

            return reversedSegments;
        }

        return segments;
    }

    public Vector2 GetContinuousPosition(double index) {
        int segmentIndex = (int) Math.Floor(index);
        double segmentProgression = index - segmentIndex;

        return Math.Abs(segmentProgression) < Precision.DoubleEpsilon ? _pathPoints[segmentIndex] :
            Math.Abs(segmentProgression - 1) < Precision.DoubleEpsilon ? _pathPoints[segmentIndex + 1] :
            Vector2.Lerp(_pathPoints[segmentIndex], _pathPoints[segmentIndex + 1], segmentProgression);
    }

    public double GetContinuousAngle(double index) {
        int segmentIndex = MathHelper.Clamp((int) Math.Floor(index + Precision.DoubleEpsilon), 0, _angles.Count - 1);

        return _angles[segmentIndex];
    }

    public double GetContinuousDistance(double index) {
        int segmentIndex = (int) Math.Floor(index);
        double segmentProgression = index - segmentIndex;

        return Math.Abs(segmentProgression) < Precision.DoubleEpsilon ? _cumulativeLengths[segmentIndex] :
            Math.Abs(segmentProgression - 1) < Precision.DoubleEpsilon ? _cumulativeLengths[segmentIndex + 1] :
            (1 - segmentProgression) * _cumulativeLengths[segmentIndex] + segmentProgression * _cumulativeLengths[segmentIndex + 1];
    }

    public double GetIndexAtDistance(double distance) {
        var index = _cumulativeLengths.BinarySearch(distance);
        if (index >= 0) {
            return index;
        }

        var i2 = ~index;
        var i1 = i2 - 1;
        var d1 = _cumulativeLengths[i1];
        var d2 = _cumulativeLengths[i2];

        return (distance - d1) / (d2 - d1) + i1;
    }

    private static double Modulo(double a, double n) {
        return a - Math.Floor(a / n) * n;
    }

    private static double GetSmallestAngle(double a1, double a2) {
        return Modulo(a2 - a1 + Math.PI, 2 * Math.PI) - Math.PI;
    }

    public static double CalculatePathLength(List<Vector2> anchors) {
        double length = 0;

        int start = 0;
        int end = 0;

        for (int i = 0; i < anchors.Length(); ++i) {
            end++;

            if (i != anchors.Length() - 1 && anchors[i] != anchors[i + 1]) {
                continue;
            }

            List<Vector2> cpSpan = anchors.GetRange(start, end - start);
            length += new BezierSubdivision(cpSpan).SubdividedApproximationLength();
            start = end;
        }

        return length;
    }

    public enum ApproximationMode {
        TangentIntersection,
        DoubleMiddle,
        Best,
    }
}