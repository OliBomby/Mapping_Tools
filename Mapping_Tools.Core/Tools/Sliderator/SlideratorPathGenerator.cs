using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>
/// Converts a distance-parametrized slider path into osu! anchors whose
/// travelled distance follows a requested position function.
/// </summary>
public sealed class SlideratorPathGenerator
{
    /// <summary>Gets or sets the position function in pixels for a time in milliseconds.</summary>
    public Func<double, double> PositionFunction { get; set; } = static _ => 0;

    /// <summary>Gets or sets the duration in milliseconds over which the function is evaluated.</summary>
    public double MaxT { get; set; }

    /// <summary>Gets or sets the constant slider travel rate in pixels per millisecond.</summary>
    public double Velocity { get; set; }

    /// <summary>Gets or sets the minimum tumour/dendrite length.</summary>
    public double MinDendriteLength { get; set; } = 1;

    /// <summary>Gets the expected output pixel length.</summary>
    public double MaxS => MaxT * Velocity;

    private List<Vector2> path = [];
    private List<Vector2> diff = [];
    private List<double> angle = [];
    private List<double> diffL = [];
    private List<double> pathL = [];
    private List<LatticePoint> lattice = [];
    private List<Neuron> slider = [];

    /// <summary>Sets the sampled source path and removes consecutive duplicates.</summary>
    /// <param name="pathPoints">Ordered source points with non-zero total length.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pathPoints"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="pathPoints"/> is empty.</exception>
    /// <exception cref="InvalidOperationException">The source has zero length.</exception>
    public void SetPath(IReadOnlyList<Vector2> pathPoints)
    {
        ArgumentNullException.ThrowIfNull(pathPoints);
        if (pathPoints.Count == 0)
        {
            throw new ArgumentException("The sliderator path cannot be empty.", nameof(pathPoints));
        }

        path = [pathPoints[0]];
        diff = [];
        angle = [];
        diffL = [];
        double sum = 0;
        pathL = [0];
        foreach (Vector2 point in pathPoints.Skip(1))
        {
            Vector2 delta = point - path[^1];
            double length = delta.Length;
            if (length < Precision.DoubleEpsilon)
            {
                continue;
            }

            path.Add(point);
            diff.Add(delta);
            angle.Add(delta.Theta);
            diffL.Add(length);
            sum += length;
            pathL.Add(sum);
        }

        if (Math.Abs(sum) < Precision.DoubleEpsilon)
        {
            throw new InvalidOperationException("Zero length path.");
        }

        // Add last member again so these lists have the same number of elements as path
        diff.Add(diff[^1]);
        angle.Add(angle[^1]);
        diffL.Add(diffL[^1]);
    }

    /// <summary>Generates a variable-velocity slider path.</summary>
    /// <returns>The generated osu! control points.</returns>
    public List<Vector2> Sliderate()
    {
        GetLatticePoints();
        GenerateNeurons();
        GenerateAxons();
        GenerateDendrites();
        return AnchorsList();
    }

    /// <summary>Samples the source path at stream tick intervals.</summary>
    /// <param name="deltaT">The interval in milliseconds between stream objects.</param>
    /// <returns>Rounded control points for the stream.</returns>
    public List<Vector2> SliderateStream(double deltaT)
    {
        if (!double.IsFinite(deltaT) || deltaT <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaT));
        }

        List<Vector2> points = [];
        for (double time = 0; time <= MaxT + Precision.DoubleEpsilon; time += deltaT)
        {
            points.Add(PositionAt(PositionFunction(time)).Rounded());
        }

        return points;
    }

    private Vector2 PositionAt(double distance)
    {
        int index = pathL.BinarySearch(distance);
        if (index < 0)
        {
            index = ~index - 1;
        }

        if (index == -1)
        {
            index = 0;
        }

        if (index == diff.Count)
        {
            index--;
        }

        return path[index] + diff[index] / diffL[index] * (distance - pathL[index]);
    }

    private LatticePoint GetNearestLatticePoint(double pathPosition)
    {
        int left = 0;
        int right = lattice.Count - 1;
        while (right - left > 1)
        {
            int middle = (left + right) / 2;
            if (lattice[middle].PathPosition > pathPosition)
            {
                right = middle;
            }
            else
            {
                left = middle;
            }
        }

        return Math.Abs(pathPosition - lattice[left].PathPosition) <
               Math.Abs(pathPosition - lattice[right].PathPosition)
            ? lattice[left]
            : lattice[right];
    }

    private static List<LatticePoint> CreateLatticePoints(
        List<Vector2> path,
        List<Vector2> diff,
        List<double> diffL,
        List<double> pathL,
        double tolerance = 0.35)
    {
        List<LatticePoint> points = [];
        // Iterate through path segments -1 because the last one is repeated
        for (int index = 0; index < diff.Count - 1; index++)
        {
            double length = diffL[index];
            if (Math.Abs(length) < Precision.DoubleEpsilon)
            {
                // Skip segment if degenerate
                continue;
            }

            Vector2 start = path[index];
            Vector2 delta = diff[index];
            double lengthSquared = delta.LengthSquared;
            int majorAxis = Math.Abs(delta[0]) < Math.Abs(delta[1]) ? 1 : 0;
            int direction = Math.Sign(delta[majorAxis]);
            for (int i = (int)Math.Round(start[majorAxis]);
                 i != (int)Math.Round(start[majorAxis] + delta[majorAxis]) + direction;
                 i += direction)
            {
                double progress = (i - start[majorAxis]) / delta[majorAxis];
                double minor = start[1 - majorAxis] + progress * delta[1 - majorAxis];
                int j = (int)Math.Round(minor);
                Vector2 latticePoint = majorAxis == 1 ? new(j, i) : new(i, j);
                double projected = MathHelper.Clamp(
                    progress + (j - minor) * delta[1 - majorAxis] / lengthSquared,
                    0,
                    1);
                Vector2 projectedPoint = start + projected * delta;
                double position = pathL[index] + projected * length;
                double error = (latticePoint - projectedPoint).Length;
                double perpendicularError = (j - minor) * delta[majorAxis] / length * (1 - 2 * majorAxis);

                if (error > tolerance ||
                    Math.Abs(projected - 1) < Precision.DoubleEpsilon && index + 1 < diff.Count)
                {
                    continue;
                }

                if (points.Count > 0 && latticePoint == points[^1].Pos)
                {
                    if (error <= points[^1].Error)
                    {
                        points[^1] = new LatticePoint(
                            latticePoint,
                            projectedPoint,
                            position,
                            error,
                            perpendicularError,
                            index,
                            projected);
                    }
                }
                else
                {
                    points.Add(new LatticePoint(
                        latticePoint,
                        projectedPoint,
                        position,
                        error,
                        perpendicularError,
                        index,
                        projected));
                }
            }
        }

        return points;
    }

    private void GetLatticePoints() => lattice = CreateLatticePoints(path, diff, diffL, pathL);

    private double GetSpeedAtTime(double time, double epsilon) =>
        (PositionFunction(time + epsilon) - PositionFunction(time)) / epsilon;

    private void GenerateNeurons()
    {
        // These values are placeholders. Experimentation has to be done to find better parameters
        const double maxOvershot = 32;
        const double epsilon = 0.01;
        const double deltaT = 0.02;
        slider = [];

        double actualLength = 0;
        double nucleusTime = 0;
        double nucleusWantedLength = 0;
        int lastDirection = 1;
        Neuron current = new(lattice.First(), 0);
        for (double time = 0; time <= MaxT; time += deltaT)
        {
            double clampedTime = Math.Min(time, MaxT);
            double wantedLength = PositionFunction(clampedTime);
            // Input is time in milliseconds and output is position in osu! pixels
            double speed = (PositionFunction(clampedTime + epsilon) - wantedLength) / epsilon;
            int direction = Math.Sign(speed);
            double velocity = Math.Abs(speed);
            LatticePoint nearest = GetNearestLatticePoint(wantedLength);

            if (direction * lastDirection < 0 || direction == 0 && lastDirection != 0)
            {
                // Make a new neuron if the path turns around
                // The position of this turn-around is not entirely accurate because the actual turn-around happens somewhere in between the time steps
                // This is the cause behind most of the error compared to the expected total length
                Neuron next = new(nearest, clampedTime);
                current.Terminal = next;
                current.WantedLength += actualLength;
                slider.Add(current);
                current = next;
                nucleusWantedLength = wantedLength;
                nucleusTime = clampedTime - deltaT;
            }

            actualLength = (clampedTime - nucleusTime) * Velocity;
            double lengthError = Math.Abs(Math.Abs(wantedLength - nucleusWantedLength) - actualLength) - current.Error;
            // Make a new neuron when the error in the length becomes too large
            if (lengthError > Math.Max(MinDendriteLength, velocity * maxOvershot) ||
                nearest.Error < 0.05 && lengthError > Math.Max(MinDendriteLength, velocity * MinDendriteLength))
            {
                if ((nearest.Pos - current.Nucleus.Pos).LengthSquared > 0.1)
                {
                    Neuron next = new(nearest, clampedTime);
                    current.Terminal = next;
                    current.WantedLength += actualLength;
                    slider.Add(current);
                    current = next;
                    nucleusWantedLength = wantedLength;
                    nucleusTime = clampedTime;
                }
                else
                {
                    // Pretend to add a new neuron but merged with the current one
                    current.WantedLength += actualLength;
                    nucleusWantedLength = wantedLength;
                    nucleusTime = clampedTime;
                }
            }

            lastDirection = direction;
        }

        // Need to add currentNeuron at the end otherwise the last neuron would get ignored
        current.WantedLength += actualLength;
        LatticePoint finalPoint = GetNearestLatticePoint(PositionFunction(MaxT));
        Neuron last = new(finalPoint, MaxT);
        current.Terminal = last;
        slider.Add(current);
        slider.Add(last);

        double totalWantedLength = slider.Sum(neuron => neuron.WantedLength);
        // Multiply with ratio to exactly match the expected total length
        double ratio = MaxS / totalWantedLength;
        foreach (Neuron neuron in slider)
        {
            neuron.WantedLength *= ratio;
        }
    }

    private void GenerateAxons()
    {
        PathGenerator pathGenerator = new(path, diff, angle, diffL, pathL);
        // Generate bezier points that approximate the paths between neurons
        foreach (Neuron neuron in slider.Where(neuron => neuron.Terminal is not null))
        {
            Vector2 firstPoint = neuron.Nucleus.Pos;
            Vector2 lastPoint = neuron.Terminal!.Nucleus.Pos;
            List<Vector2> generated = pathGenerator.GeneratePath(
                    neuron.Nucleus.SegmentIndex + neuron.Nucleus.SegmentProgress,
                    neuron.Terminal.Nucleus.SegmentIndex + neuron.Terminal.Nucleus.SegmentProgress)
                .ToList();
            if (generated.Count < 2)
            {
                generated = [firstPoint, lastPoint];
            }
            else
            {
                generated[0] = firstPoint;
                generated[^1] = lastPoint;
            }

            neuron.Axon = new BezierSubdivision(generated);
            // Calculate lengths
            neuron.AxonLength = PathGenerator.CalculatePathLength(generated);
            neuron.DendriteLength = neuron.WantedLength - neuron.AxonLength;
        }
    }

    private Vector2 NearbyNonZeroDiff(int index)
    {
        Vector2 result = Vector2.UnitX;
        for (int offset = 0; offset < 10; offset++)
        {
            result = diff[MathHelper.Clamp(index + offset, 0, diff.Count - 1)];
            if (Math.Abs(result.X) > Precision.DoubleEpsilon || Math.Abs(result.Y) > Precision.DoubleEpsilon)
            {
                return result;
            }
        }

        return result;
    }

    private void GenerateDendrites()
    {
        double leftovers = 0;
        foreach (Neuron neuron in slider.Where(neuron => neuron.Terminal is not null))
        {
            // Find angles for the neuron and the terminal to point the dendrites towards
            int direction = Math.Sign(neuron.Terminal!.Nucleus.PathPosition - neuron.Nucleus.PathPosition);
            direction = direction == 0 ? 1 : direction;
            Vector2 firstDirection = direction * NearbyNonZeroDiff(neuron.Nucleus.SegmentIndex).Normalized();
            Vector2 secondDirection = -direction * NearbyNonZeroDiff(neuron.Terminal.Nucleus.SegmentIndex).Normalized();
            // Do an even split of dendrites between this neuron and the terminal
            double dendriteToAdd = neuron.DendriteLength + leftovers;
            // Find the time at which the position function goes in between the neuron and the terminal
            double width = neuron.Terminal.Time - neuron.Time;
            double axonWidth = neuron.AxonLength / Velocity;
            double middleTime = BinarySearchUtil.DoubleBinarySearch(
                neuron.Time,
                neuron.Terminal.Time,
                0.01,
                time => PositionFunction(time) <=
                        (neuron.Nucleus.PathPosition + neuron.Terminal.Nucleus.PathPosition) / 2);
            // Calculate the distribution of dendrites to let the axon pass through the middle at the same time as the position funciton does
            double leftPortion = Precision.AlmostEquals(width, axonWidth)
                ? 0.5
                : MathHelper.Clamp(
                    (2 * (middleTime - neuron.Time) - axonWidth) /
                    (2 * (width - axonWidth)),
                    0,
                    1);
            double rightPortion = 1 - leftPortion;
            double leftLength = dendriteToAdd * leftPortion;
            double rightLength = dendriteToAdd * rightPortion;
            double speedLeft = GetSpeedAtTime(neuron.Time + leftLength / Velocity / 2, 0.01);
            double speedRight = GetSpeedAtTime(neuron.Terminal.Time - rightLength / Velocity / 2, 0.01);
            // Get the speeds at the times of the dendrites to give the dendrites appriopriate lengths to the speed at the time
            rightLength += AddDendriteLength(
                neuron,
                leftLength,
                firstDirection,
                MinDendriteLength,
                4 * Math.Pow(speedLeft * 2, 2));
            leftovers = AddDendriteLength(
                neuron.Terminal,
                rightLength,
                secondDirection,
                MinDendriteLength,
                4 * Math.Pow(speedRight * 2, 2));
        }
    }

    private static double AddDendriteLength(
        Neuron neuron,
        double length,
        Vector2 direction,
        double minLength,
        double maxLength)
    {
        while (length > minLength)
        {
            double size = MathHelper.Clamp(
                Math.Floor(length),
                Math.Max(minLength, 1),
                Math.Min(maxLength, 12));
            Vector2 dendrite = (direction * size).Rounded();
            double dendriteLength = dendrite.Length;
            while (dendriteLength > 12)
            {
                // Shorten dendrites longer than 12 pixels to keep dendrites invisible
                size -= 0.5;
                dendrite = (direction * size).Rounded();
                dendriteLength = dendrite.Length;
            }

            if (dendriteLength < 1)
            {
                // Prevent any dendrites shorter than 1 to never get an infinite loop
                dendrite = Vector2.UnitX;
            }

            neuron.Dendrites.Add(dendrite);
            length -= dendrite.Length;
        }

        return length;
    }

    private List<Vector2> AnchorsList()
    {
        List<Vector2> anchors = [];
        for (int index = 0; index < slider.Count; index++)
        {
            Neuron neuron = slider[index];
            anchors.Add(neuron.Nucleus.Pos);
            if (index != 0)
            {
                anchors.Add(neuron.Nucleus.Pos);
            }

            foreach (Vector2 dendrite in neuron.Dendrites)
            {
                anchors.Add(neuron.Nucleus.Pos + dendrite);
                anchors.Add(neuron.Nucleus.Pos);
                anchors.Add(neuron.Nucleus.Pos);
            }

            if (index != slider.Count - 1)
            {
                anchors.AddRange(neuron.Axon.Points.GetRange(1, neuron.Axon.Points.Count - 2));
            }
        }

        anchors.RemoveAt(anchors.Count - 1);
        return anchors;
    }

    private sealed class LatticePoint
    {
        internal LatticePoint(
            Vector2 pos,
            Vector2 pathPoint,
            double pathPosition,
            double error,
            double errorPerp,
            int segmentIndex,
            double segmentProgress)
        {
            Pos = pos;
            PathPoint = pathPoint;
            PathPosition = pathPosition;
            Error = error;
            ErrorPerp = errorPerp;
            SegmentIndex = segmentIndex;
            SegmentProgress = segmentProgress;
        }

        internal Vector2 Pos { get; }
        internal Vector2 PathPoint { get; }
        internal double PathPosition { get; }
        internal double Error { get; }
        internal double ErrorPerp { get; }
        internal int SegmentIndex { get; }
        internal double SegmentProgress { get; }
    }

    private sealed class Neuron
    {
        internal Neuron(LatticePoint nucleus, double time)
        {
            Nucleus = nucleus;
            Dendrites = [];
            Axon = new BezierSubdivision([nucleus.Pos]);
            Time = time;
        }

        internal LatticePoint Nucleus { get; }
        internal List<Vector2> Dendrites { get; }
        internal BezierSubdivision Axon { get; set; }
        internal Neuron? Terminal { get; set; }
        internal double WantedLength { get; set; }
        internal double DendriteLength { get; set; }
        internal double AxonLength { get; set; }
        internal double Error { get; set; }
        internal double Time { get; }
    }
}
