namespace Mapping_Tools.Core.Progress;

/// <summary>
///     Provides composition helpers for normalized progress reporting.
/// </summary>
public static class ProgressExtensions
{
    /// <summary>
    ///     Creates a progress receiver that maps normalized child progress into a
    ///     subrange of the receiver's normalized progress range.
    /// </summary>
    /// <param name="progress">The receiver for mapped progress values.</param>
    /// <param name="start">The inclusive normalized value at child progress zero.</param>
    /// <param name="end">The inclusive normalized value at child progress one.</param>
    /// <returns>A progress receiver that forwards values mapped into the requested range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="progress" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     The range is not finite, does not lie within zero through one, or ends before it starts.
    /// </exception>
    public static IProgress<double> MapTo(this IProgress<double> progress, double start, double end)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ValidateRange(start, end);
        return new MappedProgress(progress, start, end);
    }

    /// <summary>
    ///     Creates a progress receiver that maps one zero-based step into its
    ///     corresponding equal-sized subrange of the receiver's normalized range.
    /// </summary>
    /// <param name="progress">The receiver for mapped progress values.</param>
    /// <param name="step">The zero-based step index whose child progress is being reported.</param>
    /// <param name="totalSteps">The positive number of equal-sized steps.</param>
    /// <returns>A progress receiver mapped to the selected step's normalized range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="progress" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="step" /> is not between zero and one less than <paramref name="totalSteps" />, or
    ///     <paramref name="totalSteps" /> is not positive.
    /// </exception>
    public static IProgress<double> MapTo(this IProgress<double> progress, int step, int totalSteps)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (totalSteps <= 0 || step < 0 || step >= totalSteps)
            throw new ArgumentOutOfRangeException(
                nameof(step),
                "The step index must lie within the requested positive step count.");

        return progress.MapTo(
            step / (double)totalSteps,
            (step + 1) / (double)totalSteps);
    }

    /// <summary>
    ///     Reports completed steps as normalized progress.
    /// </summary>
    /// <param name="progress">The receiver for the normalized progress value.</param>
    /// <param name="step">The number of completed steps, from zero through <paramref name="totalSteps" />.</param>
    /// <param name="totalSteps">The positive total number of steps.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="step" /> is outside zero through <paramref name="totalSteps" />, or
    ///     <paramref name="totalSteps" /> is not positive.
    /// </exception>
    public static void Report(this IProgress<double> progress, int step, int totalSteps)
    {
        ArgumentNullException.ThrowIfNull(progress);
        if (totalSteps <= 0 || step < 0 || step > totalSteps)
            throw new ArgumentOutOfRangeException(
                nameof(step),
                "The completed step count must lie within the requested positive step count.");

        progress.Report(step / (double)totalSteps);
    }

    private static void ValidateRange(double start, double end)
    {
        if (!double.IsFinite(start) || !double.IsFinite(end) || start < 0 || end > 1 || start > end)
            throw new ArgumentOutOfRangeException(
                nameof(start),
                "A mapped progress range must be finite, ordered, and contained within zero through one.");
    }

    private sealed class MappedProgress(IProgress<double> destination, double start, double end) : IProgress<double>
    {
        public void Report(double value)
        {
            if (!double.IsFinite(value) || value is < 0 or > 1)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Progress must be a finite value from zero through one.");

            destination.Report(start + (end - start) * value);
        }
    }
}
