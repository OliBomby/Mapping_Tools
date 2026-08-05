using System.ComponentModel.DataAnnotations;

namespace Mapping_Tools.Application.Interactions.Validation;

/// <summary>
/// Requires a comparable value to fall within an inclusive, strongly typed range.
/// </summary>
/// <typeparam name="T">The ordered value type accepted by the rule.</typeparam>
public sealed class InclusiveRangeAttribute<T> : ValidationAttribute
    where T : IComparable<T>
{
    /// <summary>
    /// Creates an inclusive range and rejects an inverted pair of boundaries.
    /// </summary>
    /// <param name="minimum">The smallest accepted value.</param>
    /// <param name="maximum">The largest accepted value.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="minimum"/> is greater than <paramref name="maximum"/>.
    /// </exception>
    public InclusiveRangeAttribute(T minimum, T maximum)
    {
        if (minimum.CompareTo(maximum) > 0)
        {
            throw new ArgumentException(
                "The minimum cannot be greater than the maximum.",
                nameof(minimum));
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Gets the smallest accepted value.
    /// </summary>
    public T Minimum { get; }

    /// <summary>
    /// Gets the largest accepted value.
    /// </summary>
    public T Maximum { get; }

    /// <inheritdoc/>
    public override bool IsValid(object? value) =>
        value is T typedValue
        && typedValue.CompareTo(Minimum) >= 0
        && typedValue.CompareTo(Maximum) <= 0;
}
