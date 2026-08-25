using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Images;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.SliderPicturator;

/// <summary>Runs the framework-independent Slider Picturator colour and path algorithm.</summary>
public static class SliderPicturatorEngine
{
    private const int osupx_between_rows = 960;
    private const double lighten_amount = 0.25;
    private const double darken_amount = 0.1;
    private const byte alpha = 180;
    private static int Snaptol => 96;

    /// <summary>Recolours an image and estimates the slider segments required to reproduce it.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="sliderColor">The slider track colour.</param>
    /// <param name="sliderBorder">The slider border colour.</param>
    /// <param name="backgroundColor">The colour composited below transparent pixels.</param>
    /// <param name="slider">Optional selected slider used to include sliderball motion in the estimate.</param>
    /// <param name="blackOff">Whether black pixels are excluded from matching.</param>
    /// <param name="borderOff">Whether the border colour is excluded from matching.</param>
    /// <param name="opaqueOff">Whether source alpha is ignored.</param>
    /// <param name="r">Whether red participates in matching.</param>
    /// <param name="g">Whether green participates in matching.</param>
    /// <param name="b">Whether blue participates in matching.</param>
    /// <param name="quality">The integer gradient quantization quality.</param>
    /// <returns>A recoloured image and the estimated segment count.</returns>
    public static (RgbaImage Image, long SegmentCount) Recolor(
        RgbaImage image,
        RgbaColour sliderColor,
        RgbaColour sliderBorder,
        RgbaColour backgroundColor,
        HitObject? slider = null,
        bool blackOff = false,
        bool borderOff = false,
        bool opaqueOff = false,
        bool r = true,
        bool g = true,
        bool b = true,
        int quality = 101)
    {
        ValidateImageAndQuality(image, quality);
        double[,] pixelDistances = CalculatePixelDistances(
            image, sliderColor, sliderBorder, backgroundColor, blackOff, borderOff, opaqueOff, r, g, b, quality);
        var result = image.Clone();
        var inner = GetOpaqueColor(GetOpaqueGradientColour(sliderColor, true), backgroundColor);
        var outer = GetOpaqueColor(GetOpaqueGradientColour(sliderColor, false), backgroundColor);
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
        {
            var source = image.GetPixel(x, y);
            if (!opaqueOff) source = GetOpaqueColor(source, backgroundColor);
            Vector3 colour = new(r ? source.R : 0, g ? source.G : 0, b ? source.B : 0);
            Vector3 innerVector = new(inner.R, inner.G, inner.B);
            Vector3 outerVector = new(outer.R, outer.G, outer.B);
            Vector3 borderVector = new(sliderBorder.R, sliderBorder.G, sliderBorder.B);
            double projectionLength = (innerVector - outerVector).Length;
            double gradientDistance = (colour - ClosestGradient(colour, outerVector, innerVector, projectionLength)).LengthSquared;
            double borderDistance = (colour - borderVector).LengthSquared;
            bool useBorder = !borderOff && gradientDistance >= borderDistance;
            bool useBlack = !blackOff && colour.LengthSquared < (useBorder ? borderDistance : gradientDistance);
            if (useBlack)
            {
                result.SetPixel(x, y, RgbaColour.FromRgb(0, 0, 0));
                continue;
            }

            if (useBorder)
            {
                result.SetPixel(x, y, RgbaColour.FromRgb(sliderBorder.R, sliderBorder.G, sliderBorder.B));
                continue;
            }

            var used = innerVector - pixelDistances[x, y] * (innerVector - outerVector);
            result.SetPixel(x, y, RgbaColour.FromRgb(
                (byte)Math.Clamp(Math.Round(used.X), 0, 255),
                (byte)Math.Clamp(Math.Round(used.Y), 0, 255),
                (byte)Math.Clamp(Math.Round(used.Z), 0, 255)));
        }

        long segments = CountSegments(pixelDistances, image.Width, image.Height);
        if (slider is { IsSlider: true })
        {
            int duration = (int)Math.Floor(slider.TemporalLength);
            const double circle_size = 10;
            const double object_radius = 1.00041 * (54.4 - 4.48 * circle_size);
            const double gpu = 65536;
            Vector2 topLeft = new(-104, -52);
            Vector2 start = new((float)Math.Ceiling(object_radius * 1.15) + topLeft.X, (float)Math.Ceiling(object_radius * 1.15) + topLeft.Y);
            Vector2 bottomRight = new((float)Math.Floor(osupx_between_rows * gpu - 1.15 * object_radius) + topLeft.X,
                (float)Math.Floor(osupx_between_rows * gpu - 1.15 * object_radius) + topLeft.Y);
            List<Vector2> perimeter =
            [
                start, new(start.X, start.Y), new(bottomRight.X, start.Y), bottomRight,
                new(bottomRight.X, start.Y), new(start.X, start.Y),
            ];
            double frameDistance = 2 * StableDistance(perimeter);
            double availableDistance = 2 * (bottomRight.X - 700);
            segments += 2L * ((int)Math.Floor(frameDistance / availableDistance) + 1) * duration + duration;
        }

        return (result, segments);
    }

    /// <summary>Converts an image into the linear slider path used by the legacy tool.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="sliderColor">The slider track colour.</param>
    /// <param name="sliderBorder">The slider border colour.</param>
    /// <param name="backgroundColor">The colour composited below transparent pixels.</param>
    /// <param name="circleSize">The beatmap circle size.</param>
    /// <param name="startPosition">The generated slider start position.</param>
    /// <param name="imagePosition">The image top-left position.</param>
    /// <param name="slider">Optional selected slider for sliderball motion.</param>
    /// <param name="resolutionY">The osu! window height used for image scaling.</param>
    /// <param name="gpuViewport">The selected GPU viewport size.</param>
    /// <param name="blackOff">Whether black pixels are excluded from matching.</param>
    /// <param name="borderOff">Whether the border colour is excluded from matching.</param>
    /// <param name="opaqueOff">Whether source alpha is ignored.</param>
    /// <param name="r">Whether red participates in matching.</param>
    /// <param name="g">Whether green participates in matching.</param>
    /// <param name="b">Whether blue participates in matching.</param>
    /// <param name="quality">The integer gradient quantization quality.</param>
    /// <returns>The generated linear anchors and distance travelled per millisecond.</returns>
    public static (List<Vector2> Path, double FrameDistance) Picturate(
        RgbaImage image, RgbaColour sliderColor, RgbaColour sliderBorder, RgbaColour backgroundColor,
        double circleSize, Vector2 startPosition, Vector2 imagePosition, HitObject? slider = null,
        double resolutionY = 1080, long gpuViewport = 16384, bool blackOff = false, bool borderOff = false,
        bool opaqueOff = false, bool r = true, bool g = true, bool b = true, int quality = 101)
    {
        ValidateImageAndQuality(image, quality);
        // startPos, startPosPic are in osupx
        var start = startPosition;
        var picturePosition = imagePosition;
        start.Round();
        picturePosition.Round();
        double radius = 1.00041 * (54.4 - 4.48 * circleSize);
        double[,] pixelDistances = CalculatePixelDistances(
            image, sliderColor, sliderBorder, backgroundColor, blackOff, borderOff, opaqueOff, r, g, b, quality);
        Vector2 imageTopLeft = new(-104, -52);
        Vector2 sliderTopLeft = new(Math.Ceiling(radius * 1.15) + imageTopLeft.X, Math.Ceiling(radius * 1.15) + imageTopLeft.Y);
        Vector2 sliderBottomRight = new(Math.Floor(osupx_between_rows * gpuViewport - 1.15 * radius) + imageTopLeft.X,
            Math.Floor(osupx_between_rows * gpuViewport - 1.15 * radius) + imageTopLeft.Y);
        picturePosition -= imageTopLeft;
        picturePosition *= (resolutionY - 16) / 480;
        picturePosition.Round();
        var imageStart = imageTopLeft + osupx_between_rows * picturePosition;

        Vector2[]? sliderBallPositions = null;
        Vector2[]? lastSegmentStarts = null;
        int duration = 0;
        // Handle sliderball control calculations
        if (slider is { IsSlider: true })
        {
            duration = (int)Math.Floor(slider.TemporalLength);
            sliderBallPositions = new Vector2[duration + 1];
            for (int i = 0; i <= duration; i++) sliderBallPositions[i] = slider.SliderPath.SliderballPositionAt(i, duration);

            // Before rounding sbPositions, calculate starting coordinate for each ms' final segment to make the sliderball rotate appropriately
            lastSegmentStarts = new Vector2[duration + 1];
            // We don't care about msLastSegStart[0] so we'll leave it at 0. Technically we could save one Vector2's worth of space here but it would make indexing harder to read than necessary.
            // Find the first angle - we can't calculate the angle between points that are the same, but the sliderball's rotation should be the same as it was before.
            double savedAngle = 0;
            for (int i = 1; i <= duration; i++)
                if (sliderBallPositions[0] != sliderBallPositions[i])
                    savedAngle = Math.Atan2(sliderBallPositions[i - 1].Y - sliderBallPositions[i].Y, sliderBallPositions[i - 1].X - sliderBallPositions[i].X);
            for (int i = 1; i <= duration; i++)
            {
                double angle = sliderBallPositions[i - 1] == sliderBallPositions[i]
                    ? savedAngle
                    : Math.Atan2(sliderBallPositions[i - 1].Y - sliderBallPositions[i].Y, sliderBallPositions[i - 1].X - sliderBallPositions[i].X);
                savedAngle = angle;
                lastSegmentStarts[i] = new Vector2((float)(Snaptol * Math.Cos(angle) + sliderBallPositions[i].Rounded().X),
                    (float)(Snaptol * Math.Sin(angle) + sliderBallPositions[i].Rounded().Y));
            }

            // Round all positions to float precision values
            for (int i = 0; i <= duration; i++)
            {
                sliderBallPositions[i].Round();
                sliderBallPositions[i] = new Vector2((float)sliderBallPositions[i].X, (float)sliderBallPositions[i].Y);
                lastSegmentStarts[i].Round();
                lastSegmentStarts[i] = lastSegmentStarts[i].X < sliderTopLeft.X || lastSegmentStarts[i].Y < sliderTopLeft.Y
                    ? new Vector2((float)(sliderBallPositions[i].Rounded().X + 60), (float)sliderBallPositions[i].Rounded().Y)
                    : new Vector2((float)lastSegmentStarts[i].X, (float)lastSegmentStarts[i].Y);
            }
        }

        int direction = -1;
        List<Vector2> path = [];
        List<List<Vector2>> paths = [];
        List<Vector2> current =
        [
            start, new(start.X, sliderTopLeft.Y), new(sliderBottomRight.X, sliderTopLeft.Y), sliderBottomRight,
            new(sliderBottomRight.X, sliderTopLeft.Y), sliderTopLeft,
        ];
        paths.Add([.. current]);
        current.Clear();
        // Move to the start of the image, avoiding sample points (could be done better)
        if (picturePosition.LengthSquared > 0) current.AddRange([new Vector2(sliderTopLeft.X, imageStart.Y), imageStart]);

        for (int y = 0; y < image.Height; y++)
        {
            direction = -direction;
            int x = direction == 1 ? 0 : image.Width - 1;
            int lastStartX = 0;
            int lastStartY = 0;
            int lastOffset = 0;
            double lastGradient = 0;
            while (direction == 1 ? x < image.Width : x >= 0)
            {
                int offset = 0;
                double gradient = 0;
                if (x + direction >= 0 && x + direction < image.Width)
                {
                    gradient = pixelDistances[x + direction, y] - pixelDistances[x, y];
                    offset = direction;
                    while (x + offset + direction >= 0
                           && x + offset + direction < image.Width
                           && Math.Abs(pixelDistances[x + offset + direction, y] - pixelDistances[x + offset, y] - gradient) <= 0.001) offset += direction;
                }

                int end = x + offset;
                double flatSlope = Math.Round(gradient * (offset + 0.5)) / ((offset + 0.5) * osupx_between_rows);
                double segmentSlope = flatSlope == 0 ? 0 : flatSlope / Math.Pow(1 - flatSlope * flatSlope, 0.5);
                int relativeX = -direction * osupx_between_rows / 4;
                int relativeY = (int)(segmentSlope * relativeX
                                      + Math.Pow(1 + segmentSlope * segmentSlope, 0.5) * radius * (pixelDistances[x, y] + gradient * (offset + 1) / 2)
                                      - segmentSlope * osupx_between_rows * (offset + 1) / 2);
                lastStartX = (int)(relativeX + osupx_between_rows * (x + 0.5) + imageStart.X);
                lastStartY = (int)(relativeY + osupx_between_rows * (y + 0.5) + imageStart.Y);
                lastOffset = offset;
                lastGradient = gradient;
                current.Add(new Vector2(lastStartX, lastStartY));
                current.Add(new Vector2(lastStartX + (offset + direction * 0.5) * osupx_between_rows,
                    Math.Round(lastStartY + gradient * offset)));
                x = end + direction;
            }

            current.Add(new Vector2(lastStartX + (lastOffset + 0.5) * osupx_between_rows,
                lastStartY + lastGradient * lastOffset + osupx_between_rows));
            if (direction == 1)
            {
                paths.Add([.. current]);
                current.Clear();
            }
        }

        paths.Add([.. current]);

        List<Vector2> totalPath = [];
        foreach (var item in paths) totalPath.AddRange(item);
        if (duration == 0) return (totalPath, 0);

        double totalDistance = StableDistance(totalPath);
        double frameDistance = 0;
        while (duration * frameDistance < totalDistance)
        {
            if (frameDistance > 0) path.RemoveRange(path.Count - 2, 2);
            path.AddRange(paths[0]);
            path.AddRange(paths[0]);
            path.Add(lastSegmentStarts![1]);
            path.Add(sliderBallPositions![1]);
            frameDistance = StableDistance(path) - Snaptol / 2d;
        }

        double currentDistance = 0;
        double correction = 0;
        int currentMillisecond = 2;
        List<Vector2> currentPath = [path[^1]];
        for (int i = 1; i < paths.Count && currentMillisecond < duration; i++)
        {
            current.Clear();
            current.Add(currentDistance > 0 ? paths[i - 1][^1] : path[^1]);
            current.AddRange(paths[i]);
            double currentPathDistance = StableDistance(current);
            var difference = current[^1] - lastSegmentStarts![currentMillisecond];
            if (currentDistance + currentPathDistance + Math.Abs(difference.X) + Math.Abs(difference.Y) + Snaptol > frameDistance)
            {
                difference = currentPath[^1] - lastSegmentStarts[currentMillisecond];
                currentDistance += Math.Abs(difference.X) + Math.Abs(difference.Y);
                currentPath.Add(new Vector2(currentPath[^1].X, lastSegmentStarts[currentMillisecond].Y));
                currentPath.Add(lastSegmentStarts[currentMillisecond]);
                currentDistance += Snaptol;
                double available = 2 * (sliderBottomRight.X - lastSegmentStarts[currentMillisecond].X);
                int repeats = (int)Math.Floor((frameDistance - currentDistance) / available);
                for (int j = 0; j < repeats; j++)
                {
                    currentPath.Add(new Vector2(sliderBottomRight.X, lastSegmentStarts[currentMillisecond].Y));
                    currentPath.Add(lastSegmentStarts[currentMillisecond]);
                    currentDistance += available;
                }

                currentPath.Add(new Vector2(lastSegmentStarts[currentMillisecond].X + (float)Math.Round((frameDistance - currentDistance + correction) / 2),
                    lastSegmentStarts[currentMillisecond].Y));
                currentPath.Add(lastSegmentStarts[currentMillisecond]);
                currentPath.Add(sliderBallPositions![currentMillisecond]);
                currentMillisecond++;
                correction += frameDistance - StableDistance(currentPath);
                currentPath.RemoveAt(0);
                path.AddRange([.. currentPath]);
                currentPath.Clear();
                currentPath.Add(path[^1]);
                currentPath.AddRange(paths[i]);
                currentDistance = StableDistance(currentPath);
            }
            else
            {
                currentDistance += currentPathDistance;
                currentPath.AddRange(paths[i]);
            }
        }

        // Add sliderball path once (if) you run out of image to get distance
        if (currentMillisecond < duration)
        {
            var difference = currentPath[^1] - lastSegmentStarts![currentMillisecond];
            currentDistance += Math.Abs(difference.X) + Math.Abs(difference.Y);
            currentPath.Add(new Vector2(currentPath[^1].X, lastSegmentStarts[currentMillisecond].Y));
            currentPath.Add(lastSegmentStarts[currentMillisecond]);
            currentDistance += Snaptol;
            double available = 2 * (sliderBottomRight.X - lastSegmentStarts[currentMillisecond].X);
            int repeats = (int)Math.Floor((frameDistance - currentDistance) / available);
            for (int j = 0; j < repeats; j++)
            {
                currentPath.Add(new Vector2(sliderBottomRight.X, lastSegmentStarts[currentMillisecond].Y));
                currentPath.Add(lastSegmentStarts[currentMillisecond]);
                currentDistance += available;
            }

            currentPath.Add(new Vector2(lastSegmentStarts[currentMillisecond].X + (float)Math.Round((frameDistance - currentDistance + correction) / 2),
                lastSegmentStarts[currentMillisecond].Y));
            currentPath.Add(lastSegmentStarts[currentMillisecond]);
            currentPath.Add(sliderBallPositions![currentMillisecond]);
            currentMillisecond++;
            correction += frameDistance - StableDistance(currentPath);
            currentPath.RemoveAt(0);
            path.AddRange([.. currentPath]);
            currentPath.Clear();
            currentPath.Add(path[^1]);
            // Next just spam segments to get length
            while (currentMillisecond < duration)
            {
                difference = currentPath[0] - lastSegmentStarts[currentMillisecond];
                double generousLength = Math.Abs(difference.X) + Math.Abs(difference.Y);
                available = 2 * (sliderBottomRight.X - lastSegmentStarts[currentMillisecond].X);
                // We are assuming all slider positions are in the top left quadrant of the box centered on
                // the top left sample point, so adding segments like this does not interfere with the picture.
                for (int j = 0; j < Math.Floor((frameDistance - Snaptol - generousLength) / available); j++)
                {
                    currentPath.Add(new Vector2(sliderBottomRight.X, lastSegmentStarts[currentMillisecond].Y));
                    currentPath.Add(lastSegmentStarts[currentMillisecond]);
                }

                currentPath.Add(new Vector2(lastSegmentStarts[currentMillisecond].X + (float)Math.Round((frameDistance - StableDistance(currentPath) - Snaptol + correction) / 2),
                    lastSegmentStarts[currentMillisecond].Y));
                currentPath.Add(lastSegmentStarts[currentMillisecond]);
                currentPath.Add(sliderBallPositions[currentMillisecond]);
                currentMillisecond++;
                correction += frameDistance - StableDistance(currentPath);
                currentPath.RemoveAt(0);
                path.AddRange([.. currentPath]);
                currentPath.Clear();
                currentPath.Add(path[^1]);
            }
        }

        return (path, frameDistance);
    }

    /// <summary>Applies one generated slider and its temporary velocity timing to a beatmap.</summary>
    /// <param name="beatmap">The mutable beatmap to update.</param>
    /// <param name="path">The generated linear slider anchors.</param>
    /// <param name="frameDistance">The target distance travelled per millisecond.</param>
    /// <param name="duration">The requested slider duration in milliseconds.</param>
    /// <param name="time">The requested start time in milliseconds.</param>
    /// <param name="sliderColor">The selected track colour.</param>
    /// <param name="borderColor">The selected border colour.</param>
    /// <param name="setBeatmapColors">Whether special slider colours are written.</param>
    /// <param name="setTrackColorOverride">Whether the track colour is written as a beatmap override.</param>
    public static void ApplyToBeatmap(Beatmap beatmap, IReadOnlyList<Vector2> path, double frameDistance, double duration,
        double time, RgbaColour sliderColor, RgbaColour borderColor, bool setBeatmapColors, bool setTrackColorOverride)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(path);
        if (path.Count < 2) throw new ArgumentException("A generated slider needs at least two anchors.", nameof(path));
        HitObject hitObject = new(time, 0, SampleSet.None, SampleSet.None)
        {
            IsCircle = false, IsSpinner = false, IsHoldNote = false, IsSlider = true,
            SliderVelocity = double.NaN,
        };
        int currentColourIndex = 0;
        int index = beatmap.HitObjects.Select(item => item.Time).ToList().BinarySearch(time);
        if (index < 0) index = ~index - 1;
        if (index >= 0) currentColourIndex = beatmap.HitObjects[index].ColourIndex;
        int foundColourIndex = beatmap.ComboColours.FindIndex(colour => colour.Color.R == sliderColor.R && colour.Color.G == sliderColor.G && colour.Color.B == sliderColor.B);
        if (foundColourIndex < 0) foundColourIndex = 0;
        hitObject.ComboSkip = foundColourIndex - currentColourIndex - 1;
        hitObject.SetAllCurvePoints(path.ToList());
        hitObject.SliderType = PathType.Linear;
        hitObject.PixelLength = StableDistance(path);
        beatmap.HitObjects.Add(hitObject);
        beatmap.SortHitObjects();

        var timing = beatmap.BeatmapTiming;
        var after = timing.GetRedlineAtTime(hitObject.Time).Copy();
        var on = after.Copy();
        after.Offset = hitObject.Time;
        on.Offset = hitObject.Time - 1;
        after.OmitFirstBarLine = true;
        on.OmitFirstBarLine = true;
        on.MpB = frameDistance == 0
            ? 100 * timing.SliderMultiplier * duration / hitObject.PixelLength
            : 100 * timing.SliderMultiplier / frameDistance;
        List<TimingPointChange> changes =
        [
            new(on, true, uninherited: true, omitFirstBarLine: true, fuzziness: Precision.DOUBLE_EPSILON),
            new(after, true, uninherited: true, omitFirstBarLine: true, fuzziness: Precision.DOUBLE_EPSILON),
        ];
        hitObject.Time -= 1;
        changes.AddRange(beatmap.HitObjects.Select(item => CreateVelocityChange(item, hitObject, timing)));
        TimingPointChange.Apply(timing, changes);
        if (setBeatmapColors)
        {
            if (setTrackColorOverride) beatmap.SpecialColours["SliderTrackOverride"] = new ComboColour(sliderColor.R, sliderColor.G, sliderColor.B);
            beatmap.SpecialColours["SliderBorder"] = new ComboColour(borderColor.R, borderColor.G, borderColor.B);
        }
    }

    private static TimingPointChange CreateVelocityChange(HitObject item, HitObject generated, Timing timing)
    {
        var point = timing.GetTimingPointAtTime(item.Time).Copy();
        point.MpB = item == generated ? item.SliderVelocity : timing.GetSvAtTime(item.Time);
        point.Offset = item.Time;
        return new TimingPointChange(point, true, fuzziness: Precision.DOUBLE_EPSILON);
    }

    private static RgbaColour GetOpaqueColor(RgbaColour top, RgbaColour bottom)
    {
        const double gamma = 1;
        double topOpacity = top.A / 255d;
        double bottomOpacity = bottom.A / 255d;
        double totalOpacity = topOpacity + bottomOpacity * (1 - topOpacity);
        if (totalOpacity == 0) return RgbaColour.FromRgb(0, 0, 0);

        byte mix(byte topChannel, byte bottomChannel) => (byte)Math.Round(Math.Pow(
            (Math.Pow(bottomChannel, gamma) * bottomOpacity * (1 - topOpacity) + Math.Pow(topChannel, gamma) * topOpacity) / totalOpacity,
            1 / gamma));

        return RgbaColour.FromArgb(255, mix(top.R, bottom.R), mix(top.G, bottom.G), mix(top.B, bottom.B));
    }

    private static RgbaColour GetOpaqueGradientColour(RgbaColour slider, bool inner)
    {
        return RgbaColour.FromArgb(
            alpha,
            (byte)Math.Min(255, inner ? slider.R * (1 + 0.5 * lighten_amount) + 255 * lighten_amount : slider.R / (1 + darken_amount)),
            (byte)Math.Min(255, inner ? slider.G * (1 + 0.5 * lighten_amount) + 255 * lighten_amount : slider.G / (1 + darken_amount)),
            (byte)Math.Min(255, inner ? slider.B * (1 + 0.5 * lighten_amount) + 255 * lighten_amount : slider.B / (1 + darken_amount)));
    }

    private static double[,] CalculatePixelDistances(RgbaImage image, RgbaColour sliderColor, RgbaColour border,
        RgbaColour background, bool blackOff, bool borderOff, bool opaqueOff, bool r, bool g, bool b, int quality)
    {
        var inner = GetOpaqueColor(GetOpaqueGradientColour(sliderColor, true), background);
        var outer = GetOpaqueColor(GetOpaqueGradientColour(sliderColor, false), background);
        Vector3 innerVector = new(inner.R, inner.G, inner.B);
        Vector3 outerVector = new(outer.R, outer.G, outer.B);
        Vector3 borderVector = new(border.R, border.G, border.B);
        var projectionVector = innerVector - outerVector;
        double projectionLength = projectionVector.Length;
        double[,] distances = new double[image.Width, image.Height];
        for (int x = 0; x < image.Width; x++)
        for (int y = 0; y < image.Height; y++)
        {
            var source = image.GetPixel(x, y);
            if (!opaqueOff) source = GetOpaqueColor(source, background);
            Vector3 colour = new(r ? source.R : 0, g ? source.G : 0, b ? source.B : 0);
            var closest = ClosestGradient(colour, outerVector, innerVector, projectionLength);
            double gradientDistance = (colour - closest).LengthSquared;
            double borderDistance = (colour - borderVector).LengthSquared;
            double blackDistance = colour.LengthSquared;
            // Test if border color would be better
            if (borderOff || gradientDistance < borderDistance)
                // Test if black would be better
                distances[x, y] = !blackOff && blackDistance < gradientDistance
                    ? 1.2
                    : Math.Round(quality * Math.Clamp(1 - (closest - outerVector).Length / projectionLength, 0, 1)) * (101d / quality) / 128;
            else
                // Test if black would be better
                distances[x, y] = !blackOff && blackDistance < borderDistance
                    ? 1.2
                    : 111d / 128;
        }

        return distances;
    }

    private static Vector3 ClosestGradient(Vector3 colour, Vector3 outer, Vector3 inner, double length)
    {
        var direction = inner - outer;
        var projection = Vector3.Dot(colour - outer, direction) / Vector3.Dot(direction, direction) * direction + outer;
        if (projection.X < outer.X) return outer;
        if (projection.X > inner.X) return inner;
        return projection;
    }

    private static long CountSegments(double[,] distances, int width, int height)
    {
        // Count segments
        long count = 0;
        int direction = -1;
        // In the below loop, gradientDist means something completely different from what it means in the above loop. Here, it is being used to mean the distance in the gradient between two or more points that are evenly distributed along the slider body
        for (int y = 0; y < height; y++)
        {
            direction = -direction;
            int x = direction == 1 ? 0 : width - 1;
            while (direction == 1 ? x < width : x >= 0)
            {
                int offset = 0;
                double gradient = 0;
                // Look for gradients
                if (x + direction >= 0 && x + direction < width)
                {
                    gradient = distances[x + direction, y] - distances[x, y];
                    offset = direction;
                    while (x + offset + direction >= 0
                           && x + offset + direction < width
                           && Math.Abs(distances[x + offset + direction, y] - distances[x + offset, y] - gradient) <= 0.001) offset += direction;
                }

                x += offset + direction;
                count += 2;
            }

            count++;
        }

        return count;
    }

    private static double StableDistance(IReadOnlyList<Vector2> points)
    {
        double length = 0;
        for (int i = 1; i < points.Count; i++)
        {
            float x = (float)Math.Round(points[i - 1].X) - (float)Math.Round(points[i].X);
            float y = (float)Math.Round(points[i - 1].Y) - (float)Math.Round(points[i].Y);
            float squared = x * x + y * y;
            length += (float)Math.Sqrt(squared);
        }

        return length;
    }

    private static void ValidateImageAndQuality(RgbaImage image, int quality)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (quality is < 1 or > 101) throw new ArgumentOutOfRangeException(nameof(quality));
    }
}
