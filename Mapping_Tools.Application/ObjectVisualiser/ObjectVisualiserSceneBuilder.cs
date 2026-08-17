using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Classes.ObjectVisualiser;

namespace Mapping_Tools.Application.ObjectVisualiser;

/// <summary>Builds framework-neutral visualizer scenes from beatmap hit objects.</summary>
public static class ObjectVisualiserSceneBuilder
{
    /// <summary>Matches the legacy visualizer's maximum accepted slider pixel length.</summary>
    public const double MaxPixelLength = 1e6;

    /// <summary>Matches the legacy visualizer's maximum accepted slider segment count.</summary>
    public const double MaxSegmentCount = 1e6;

    /// <summary>Matches the legacy visualizer's hard maximum slider anchor count.</summary>
    public const int HardMaxAnchorCount = 5000;

    /// <summary>Builds a scene using the supplied Circle Size and optional stacked positions.</summary>
    /// <param name="hitObjects">The source hit objects in draw order.</param>
    /// <param name="circleSize">The beatmap Circle Size difficulty.</param>
    /// <param name="useStackedPositions">Whether stacked positions should replace base positions.</param>
    /// <param name="customPixelLength">An optional path length used when rebuilding every supported slider.</param>
    /// <returns>A scene containing supported circles, sliders, and spinners.</returns>
    public static ObjectVisualiserScene FromHitObjects(
        IEnumerable<HitObject> hitObjects,
        double circleSize,
        bool useStackedPositions = false,
        double? customPixelLength = null)
    {
        ArgumentNullException.ThrowIfNull(hitObjects);
        if (customPixelLength is not null &&
            (!double.IsFinite(customPixelLength.Value) || customPixelLength.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(customPixelLength));
        }

        double radius = Beatmap.GetHitObjectRadius(circleSize);
        var result = new List<ObjectVisualiserObject>();

        foreach (HitObject hitObject in hitObjects)
        {
            Vector2 position = useStackedPositions ? hitObject.StackedPos : hitObject.Pos;
            Vector2 shift = position - hitObject.Pos;
            if (hitObject.IsSlider)
            {
                result.Add(CreateSlider(hitObject, radius, shift, result.Count, customPixelLength));
            }
            else if (hitObject.IsSpinner)
            {
                result.Add(new ObjectVisualiserObject(
                    result.Count,
                    ObjectVisualiserObjectKind.Spinner,
                    position,
                    150,
                    comboIndex: hitObject.ComboIndex,
                    startsCombo: hitObject.ActualNewCombo));
            }
            else if (hitObject.IsCircle)
            {
                result.Add(new ObjectVisualiserObject(
                    result.Count,
                    ObjectVisualiserObjectKind.Circle,
                    position,
                    radius,
                    comboIndex: hitObject.ComboIndex,
                    startsCombo: hitObject.ActualNewCombo));
            }
        }

        return new ObjectVisualiserScene(result);
    }

    private static ObjectVisualiserObject CreateSlider(
        HitObject hitObject,
        double radius,
        Vector2 shift,
        int id,
        double? customPixelLength)
    {
        if (hitObject.PixelLength >= MaxPixelLength ||
            hitObject.CurvePoints is null || hitObject.CurvePoints.Count >= HardMaxAnchorCount)
        {
            return new ObjectVisualiserObject(id, ObjectVisualiserObjectKind.Slider, hitObject.Pos + shift, radius,
                comboIndex: hitObject.ComboIndex, startsCombo: hitObject.ActualNewCombo);
        }

        try
        {
            var sliderPath = customPixelLength is null
                ? hitObject.GetSliderPath()
                : new SliderPath(hitObject.SliderType, hitObject.GetAllCurvePoints().ToArray(), customPixelLength);
            if (sliderPath.CalculatedPath.Count > MaxSegmentCount)
            {
                return new ObjectVisualiserObject(id, ObjectVisualiserObjectKind.Slider, hitObject.Pos + shift, radius,
                    comboIndex: hitObject.ComboIndex, startsCombo: hitObject.ActualNewCombo);
            }

            var points = sliderPath.CalculatedPath.Select(point => point + shift).ToList();
            if (points.Count == 0 || points[0] != hitObject.Pos + shift)
            {
                points.Insert(0, hitObject.Pos + shift);
            }

            ObjectVisualiserPath path = new(points);
            return new ObjectVisualiserObject(
                id,
                ObjectVisualiserObjectKind.Slider,
                hitObject.Pos + shift,
                radius,
                path,
                sliderPath.ControlPoints.Select(point => point + shift),
                path.PositionAt(1),
                hitObject.ComboIndex,
                hitObject.ActualNewCombo);
        }
        catch
        {
            return new ObjectVisualiserObject(id, ObjectVisualiserObjectKind.Slider, hitObject.Pos + shift, radius,
                comboIndex: hitObject.ComboIndex, startsCombo: hitObject.ActualNewCombo);
        }
    }
}
