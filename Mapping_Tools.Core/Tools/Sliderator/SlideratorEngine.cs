using System.Globalization;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.Tools.Sliderator.Models;

namespace Mapping_Tools.Core.Tools.Sliderator;

/// <summary>
///     Applies Sliderator's graph-driven geometry and beatmap export rules to a
///     mutable Core beatmap without depending on a UI framework or a filesystem.
/// </summary>
public static class SlideratorEngine
{
    /// <summary>
    ///     Generates and exports one slider or stream from the selected source slider.
    /// </summary>
    /// <param name="beatmap">The mutable target beatmap.</param>
    /// <param name="sourceSlider">The imported slider whose path is previewed and transformed.</param>
    /// <param name="options">The complete graph and export settings.</param>
    /// <param name="progress">Optional normalized progress receiver.</param>
    /// <param name="cancellationToken">Cancels before expensive generation steps and writes.</param>
    /// <returns>Output dimensions and whether source geometry was reused.</returns>
    /// <exception cref="ArgumentException">A setting or generated value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The source is not a slider or its path is empty.</exception>
    public static SlideratorApplyResult Apply(
        Beatmap beatmap,
        HitObject sourceSlider,
        SlideratorEngineOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(beatmap);
        ArgumentNullException.ThrowIfNull(sourceSlider);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options, sourceSlider);

        var graph = options.GraphState;
        Func<double, double> positionFunction;
        bool constantVelocity;
        // Make a position function for Sliderator
        if (options.GraphModeSetting == SlideratorGraphMode.Velocity)
        {
            // We convert the graph GetValue function to a function that works like ms -> px
            positionFunction = milliseconds =>
                graph.GetIntegral(0, milliseconds * options.BeatsPerMinute / 60000) * GetSvGraphMultiplier(options) * options.PixelLength;
            constantVelocity = Precision.AlmostEquals(graph.GetMaxValue(), graph.GetMinValue());
        }
        else
        {
            // We convert the graph GetValue function to a function that works like ms -> px
            positionFunction = milliseconds =>
                graph.GetValue(milliseconds * options.BeatsPerMinute / 60000) * options.PixelLength;
            constantVelocity = Precision.AlmostEquals(graph.GetMaxDerivative(), graph.GetMinDerivative());
        }

        // Test if the function is a constant velocity
        bool simplifyShape = options.ExportAsNormal
                             && !options.ExportAsInvisibleSlider
                             && !options.ExportAsStream
                             && constantVelocity
                             && Precision.AlmostEquals(
                                 options.PixelLength / options.GraphBeats / options.GlobalSv / 100,
                                 options.NewVelocity);

        double velocity = options.NewVelocity;
        velocity = -100
                   / float.Parse(
                       (-100 / velocity).ToInvariant(),
                       CultureInfo.InvariantCulture);
        double svGraphMultiplier = GetSvGraphMultiplier(options);
        double newVelocity = velocity * svGraphMultiplier * options.PixelLength * options.BeatsPerMinute / 60000;
        var newSliderType = PathType.Bezier;
        double newLength = velocity * svGraphMultiplier * options.PixelLength * options.GraphBeats;
        double deltaT = 60000 / options.BeatsPerMinute / options.BeatSnapDivisor;
        bool delegateToBpm = options.DelegateToBpm || options.ExportAsInvisibleSlider;
        bool removeSliderTicks = options.RemoveSliderTicks || options.ExportAsInvisibleSlider;
        progress?.Report(0.1);
        cancellationToken.ThrowIfCancellationRequested();

        List<Vector2> generated = [];
        SlideratorPathGenerator sliderator = new()
        {
            PositionFunction = positionFunction,
            MaxT = options.GraphBeats / options.BeatsPerMinute * 60000,
            Velocity = newVelocity,
            MinDendriteLength = options.MinDendrite,
        };

        if (!simplifyShape)
        {
            SliderPath sourcePath = new(
                sourceSlider.SliderType,
                sourceSlider.GetAllCurvePoints().ToArray(),
                GetMaxCompletion(options) * options.PixelLength);
            List<Vector2> path = [];
            sourcePath.GetPathToProgress(path, 0, 1);
            progress?.Report(0.2);
            cancellationToken.ThrowIfCancellationRequested();
            sliderator.SetPath(path);

            if (options.ExportAsStream)
            {
                generated = sliderator.SliderateStream(deltaT);
            }
            else if (options.ExportAsInvisibleSlider)
            {
                int duration = (int)Math.Round(sliderator.MaxT);
                var sliderballPositions = new Vector2[duration + 1];
                for (int index = 0; index <= duration; index++)
                    sliderballPositions[index] = sourcePath.SliderballPositionAt(
                        (int)Math.Round(duration * positionFunction(index) / sourcePath.Distance),
                        duration);

                (var controlPoints, double frameDistance) = SliderInvisiblator.Invisiblate(
                    duration,
                    sliderballPositions,
                    options.GlobalSv);
                generated.AddRange(controlPoints);
                newSliderType = PathType.Linear;
                newVelocity = frameDistance;
                newLength = HitObject.QuickCalculateLength(controlPoints) * 2;
            }
            else
            {
                generated = sliderator.Sliderate();
                newLength = sliderator.MaxS;
                if (!double.IsFinite(newLength))
                    throw new ArgumentException(
                        "Encountered unexpected values from Sliderator. Please check your input.");
            }

            if (generated.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
                throw new ArgumentException(
                    "Encountered NaN coordinates. Please check your input.");
        }

        progress?.Report(0.6);
        cancellationToken.ThrowIfCancellationRequested();
        // Get hit object that might be present at the export time or make a new one
        var hitObjectHere = beatmap.HitObjects.FirstOrDefault(hitObject => Math.Abs(options.ExportTime - hitObject.Time) < 5)
                            ?? new HitObject(options.ExportTime, 0, SampleSet.None, SampleSet.None);
        // Clone the hit object to not affect the already existing hit object instance with changes
        HitObject clone = new(hitObjectHere.GetLine())
        {
            IsCircle = options.ExportAsStream,
            IsSpinner = false,
            IsHoldNote = false,
            IsSlider = !options.ExportAsStream,
        };

        progress?.Report(0.7);
        cancellationToken.ThrowIfCancellationRequested();
        int objectCount;
        if (!options.ExportAsStream)
        {
            // Exporting as a slider
            if (simplifyShape)
            {
                clone.SetAllCurvePoints(sourceSlider.GetAllCurvePoints());
                clone.SliderType = sourceSlider.SliderType;
                // The velocity is constant, so you can simplify to the original slider shape
            }
            else
            {
                clone.SetAllCurvePoints(generated);
                clone.SliderType = newSliderType;
            }

            clone.PixelLength = newLength;
            if (delegateToBpm && removeSliderTicks)
                // Remove repeats for NaN SV sliders to prevent gamebreaking
                clone.Repeat = 1;

            // Convert px/ms to SV
            double newVelocitySv = newVelocity / (svGraphMultiplier * options.PixelLength * options.BeatsPerMinute / 60000);
            clone.SliderVelocity = -100 / newVelocitySv;
            if (options.ExportModeSetting == SlideratorExportMode.Add)
            {
                // Add hit object
                beatmap.HitObjects.Add(clone);
            }
            else
            {
                beatmap.HitObjects.Remove(hitObjectHere);
                beatmap.HitObjects.Add(clone);
            }

            SliderVelocityFixer.Fix(
                beatmap,
                [clone],
                delegateToBpm,
                removeSliderTicks,
                cancellationToken);
            objectCount = 1;
        }
        else
        {
            // Add hit objects
            if (options.ExportModeSetting == SlideratorExportMode.Override) beatmap.HitObjects.Remove(hitObjectHere);

            double time = options.ExportTime;
            objectCount = 0;
            foreach (var position in generated)
            {
                clone.Pos = position;
                clone.Time = time;
                beatmap.HitObjects.Add(clone);
                objectCount++;
                clone = new HitObject(clone.GetLine())
                {
                    IsCircle = true,
                    IsSpinner = false,
                    IsHoldNote = false,
                    IsSlider = false,
                    NewCombo = false,
                };
                time += deltaT;
            }
        }

        progress?.Report(0.8);
        beatmap.SortHitObjects();
        progress?.Report(1);
        return new SlideratorApplyResult(newLength, newVelocity, simplifyShape, objectCount);
    }

    /// <summary>Calculates the normalized graph-to-slider conversion multiplier.</summary>
    /// <param name="options">The graph and map settings.</param>
    /// <returns>Multiplier from graph completion units to slider pixels.</returns>
    public static double GetSvGraphMultiplier(SlideratorEngineOptions options)
    {
        return 100 * options.GlobalSv / options.PixelLength;
    }

    /// <summary>Calculates the maximum preview completion represented by the graph.</summary>
    /// <param name="options">The graph and map settings.</param>
    /// <returns>The largest graph completion value.</returns>
    public static double GetMaxCompletion(SlideratorEngineOptions options)
    {
        return options.GraphModeSetting == SlideratorGraphMode.Velocity
            ? options.GraphState.GetMaxIntegral() * GetSvGraphMultiplier(options)
            : options.GraphState.GetMaxValue();
    }

    /// <summary>Calculates the minimum preview completion represented by the graph.</summary>
    /// <param name="options">The graph and map settings.</param>
    /// <returns>The smallest graph completion value.</returns>
    public static double GetMinCompletion(SlideratorEngineOptions options)
    {
        return options.GraphModeSetting == SlideratorGraphMode.Velocity
            ? options.GraphState.GetMinIntegral() * GetSvGraphMultiplier(options)
            : options.GraphState.GetMinValue();
    }

    /// <summary>Calculates the maximum absolute SV represented by the graph.</summary>
    /// <param name="options">The graph and map settings.</param>
    /// <returns>The largest absolute SV value.</returns>
    public static double GetMaximumVelocity(SlideratorEngineOptions options)
    {
        return options.GraphModeSetting == SlideratorGraphMode.Velocity
            ? Math.Max(Math.Abs(options.GraphState.GetMaxValue()), Math.Abs(options.GraphState.GetMinValue()))
            : Math.Max(Math.Abs(options.GraphState.GetMaxDerivative()), Math.Abs(options.GraphState.GetMinDerivative())) / GetSvGraphMultiplier(options);
    }

    /// <summary>Validates the domain settings required before generation.</summary>
    /// <param name="options">The complete Sliderator settings.</param>
    /// <param name="sourceSlider">The imported source object.</param>
    /// <exception cref="ArgumentException">A setting is outside the legacy contract.</exception>
    /// <exception cref="InvalidOperationException">The source is not a slider.</exception>
    public static void Validate(SlideratorEngineOptions options, HitObject sourceSlider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(sourceSlider);
        if (!sourceSlider.IsSlider) throw new InvalidOperationException("Sliderator requires a slider source object.");

        if (!Enum.IsDefined(options.ExportModeSetting) || !Enum.IsDefined(options.GraphModeSetting))
            throw new ArgumentException("Sliderator contains an unknown export or graph mode.", nameof(options));

        if (GetMinCompletion(options) < -1E-4) throw new ArgumentException("Negative position is illegal.", nameof(options));

        double maximumVelocity = options.NewVelocity;
        if (options.ExportAsNormal && double.IsInfinity(maximumVelocity)) throw new ArgumentException("Infinite slope on the path is illegal.", nameof(options));

        if (options.ExportAsNormal && maximumVelocity > options.VelocityLimit + Precision.DOUBLE_EPSILON)
            throw new ArgumentException(
                "A velocity faster than the SV limit is illegal. Please check your graph or increase the SV limit.",
                nameof(options));

        if (!double.IsFinite(options.BeatsPerMinute) || Math.Abs(options.BeatsPerMinute) < Precision.DOUBLE_EPSILON)
            throw new ArgumentException("The beats per minute field has an illegal value", nameof(options));

        if (!double.IsFinite(options.GraphBeats) || Math.Abs(options.GraphBeats) < Precision.DOUBLE_EPSILON)
            throw new ArgumentException("The beat length field has an illegal value", nameof(options));

        if (!double.IsFinite(options.GlobalSv) || Math.Abs(options.GlobalSv) < Precision.DOUBLE_EPSILON)
            throw new ArgumentException("The global SV field has an illegal value", nameof(options));

        if (!double.IsFinite(options.PixelLength)
            || options.PixelLength <= 0
            || !double.IsFinite(options.NewVelocity)
            || !double.IsFinite(options.MinDendrite)
            || options.MinDendrite <= 0
            || options.BeatSnapDivisor is < 1 or > 16)
            throw new ArgumentException("Sliderator contains an illegal numeric setting.", nameof(options));
    }
}
