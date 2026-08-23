using Mapping_Tools.Core.Classes.MathUtil;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>Builds a linear slider abusing osu!'s slider-end snapping for invisible output.</summary>
public static class SliderInvisiblator
{
    /// <summary>Gets the stable slider-end snapping distance used by osu!stable.</summary>
    public static int Snaptol => 96;

    /// <summary>
    ///     Generates control points that reproduce one rounded sliderball position per millisecond.
    /// </summary>
    /// <param name="duration">The slider duration in milliseconds.</param>
    /// <param name="sliderballPositions">The desired position at each millisecond.</param>
    /// <param name="globalSv">The beatmap global slider multiplier.</param>
    /// <returns>The generated control points and the serialized frame distance.</returns>
    /// <remarks>The input position array is rounded in place to match stable's integer sliderball positions.</remarks>
    /// <exception cref="ArgumentException">The position array is too short for the duration.</exception>
    public static (Vector2[] ControlPoints, double FrameDistance) Invisiblate(
        int duration,
        Vector2[] sliderballPositions,
        double globalSv = 1.4)
    {
        ArgumentNullException.ThrowIfNull(sliderballPositions);
        if (duration < 1 || sliderballPositions.Length < duration + 1)
            throw new ArgumentException(
                "Invisible slider output requires one sliderball position per millisecond.",
                nameof(sliderballPositions));

        // Before rounding sbPositions, calculate starting coordinate for each ms' final segment to make the sliderball rotate appropriately
        var finalSegmentStarts = new Vector2[duration + 1];
        double savedAngle = 0;
        // We don't care about msLastSegStart[0] so we'll leave it at 0. Technically we could save one Vector2's worth of space here but it would make indexing harder to read than necessary.
        // Find the first angle - we can't calculate the angle between points that are the same, but the sliderball's rotation should be the same as it was before.
        for (int index = 1; index <= duration; index++)
            if (sliderballPositions[0] != sliderballPositions[index])
                savedAngle = Math.Atan2(
                    sliderballPositions[index - 1].Y - sliderballPositions[index].Y,
                    sliderballPositions[index - 1].X - sliderballPositions[index].X);

        for (int index = 1; index <= duration; index++)
        {
            double angle = sliderballPositions[index - 1] == sliderballPositions[index]
                ? savedAngle
                : Math.Atan2(
                    sliderballPositions[index - 1].Y - sliderballPositions[index].Y,
                    sliderballPositions[index - 1].X - sliderballPositions[index].X);
            savedAngle = angle;
            finalSegmentStarts[index] = new Vector2(
                (float)(Snaptol * Math.Cos(angle) + sliderballPositions[index].Rounded().X),
                (float)(Snaptol * Math.Sin(angle) + sliderballPositions[index].Rounded().Y));
        }

        // Round all positions to float precision values
        for (int index = 0; index < sliderballPositions.Length; index++)
        {
            sliderballPositions[index].Round();
            sliderballPositions[index] = new Vector2(
                (float)sliderballPositions[index].X,
                (float)sliderballPositions[index].Y);
        }

        var controlPoints = new Vector2[8 + 4 * (duration - 1)];
        Vector2 maxXY = new(768, 412);
        List<Vector2> currentPath = [];
        // First ms travel adds SNAPTOL
        currentPath.Add(sliderballPositions[0]);
        currentPath.Add(new Vector2(67141632 + maxXY.X, sliderballPositions[0].Y));
        currentPath.Add(new Vector2(67141632 + maxXY.X, 33587200 - Snaptol / 6f + maxXY.Y));
        currentPath.Add(new Vector2(67141632 + maxXY.X, finalSegmentStarts[1].Y));
        currentPath.Add(finalSegmentStarts[1]);
        currentPath.Add(sliderballPositions[1]);

        // The precision of bpm calculation might be important when trying to be this precise with virtual sliderball position. Although the bpm is stored as a G17, it's written to the .osu as a G15 because that's the default for ToString().
        // So we will be using G15 to not fuck people over in the editor as they use this tool and continue mapping.
        double frameDistance = OsuStableDistance(currentPath) - 2 * Snaptol / 3d;
        double mpb = 100 * globalSv / frameDistance;
        mpb = double.Parse(mpb.ToString());
        frameDistance = 100 * globalSv / mpb;
        currentPath.ToArray().CopyTo(controlPoints, 0);

        int controlPointIndex = 6;
        double correction = 0;
        for (int index = 2; index <= duration; index++)
        {
            currentPath.Clear();
            // The first point on this path is the last point of the previous path
            currentPath.Add(sliderballPositions[index - 1]);
            // verticalTravel tells us how far down we need to go before going over and back up
            double verticalTravel = correction
                                    + frameDistance
                                    - (Math.Abs(sliderballPositions[index - 1].X - finalSegmentStarts[index].X)
                                        + sliderballPositions[index - 1].Y
                                        - finalSegmentStarts[index].Y
                                        + Snaptol);
            currentPath.Add(new Vector2(
                sliderballPositions[index - 1].X,
                (float)(sliderballPositions[index - 1].Y + verticalTravel / 2)));
            if (sliderballPositions[index - 1].X != finalSegmentStarts[index].X)
                currentPath.Add(new Vector2(
                    finalSegmentStarts[index].X,
                    (float)(sliderballPositions[index - 1].Y + verticalTravel / 2)));

            currentPath.Add(finalSegmentStarts[index]);
            currentPath.Add(sliderballPositions[index]);
            // Here we calculate what osu! finds for the distance travelled here, so that we can correct for it on the next iteration.
            correction += frameDistance - OsuStableDistance(currentPath);
            // Copy curMsPath into controlPoints. We use ctrlPtIdx-1 because we have the last point of the previous path in this path as well.
            currentPath.ToArray().CopyTo(controlPoints, controlPointIndex - 1);
            // Update ctrlPtIdx
            controlPointIndex += currentPath.Count - 1;
        }

        var output = new Vector2[controlPointIndex + 2];
        Array.Copy(controlPoints, output, controlPointIndex);
        // Add extra segment of length 0 to end for sliderend snapping abuse
        var lastPoint = sliderballPositions[duration];
        output[controlPointIndex] = lastPoint;
        output[controlPointIndex + 1] = lastPoint;
        return (output, frameDistance);
    }

    private static double OsuStableDistance(IReadOnlyList<Vector2> controlPoints)
    {
        double length = 0;
        for (int index = 1; index < controlPoints.Count; index++)
        {
            var previous = controlPoints[index - 1];
            var current = controlPoints[index];
            float x = (float)Math.Round(previous.X) - (float)Math.Round(current.X);
            float y = (float)Math.Round(previous.Y) - (float)Math.Round(current.Y);
            length += (float)Math.Sqrt(x * x + y * y);
        }

        return length;
    }
}
