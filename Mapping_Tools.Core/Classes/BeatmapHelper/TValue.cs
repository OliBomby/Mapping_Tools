using System.Globalization;
using System.Text.RegularExpressions;

namespace Mapping_Tools.Core.Classes.BeatmapHelper;

/// <summary>
///     Helper class for a single string that can represent multiple data types.
///     Provides methods for converting data to and from string.
/// </summary>
#nullable disable
public class TValue
{
    /// <summary>
    ///     Creates an empty wrapper for serializers; <see cref="Value" /> remains unset.
    /// </summary>
    public TValue() { }

    /// <summary>
    ///     Wraps an existing file-format value without normalizing it.
    /// </summary>
    /// <param name="str">The raw value text.</param>
    public TValue(string str)
    {
        Value = str;
    }

    /// <summary>
    ///     Gets or sets the raw text exactly as stored after an osu! section key.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    ///     Parses or replaces <see cref="Value" /> as an invariant-culture integer.
    /// </summary>
    public int IntValue
    {
        get => GetInt();
        set => SetInt(value);
    }

    /// <summary>
    ///     Parses or replaces <see cref="Value" /> as an invariant-culture floating-point number.
    /// </summary>
    public double DoubleValue
    {
        get => GetDouble();
        set => SetDouble(value);
    }

    /// <summary>
    ///     Formats an arbitrary value with invariant culture and stores the result.
    /// </summary>
    /// <param name="value">The value to convert to file-format text.</param>
    public void SetValue(object value)
    {
        Value = value.ToInvariant();
    }

    /// <summary>
    ///     Parses the raw text as a signed invariant-culture integer.
    /// </summary>
    /// <returns>The parsed integer.</returns>
    public int GetInt()
    {
        return int.Parse(Value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Stores an integer using invariant culture.
    /// </summary>
    /// <param name="value">The integer to serialize.</param>
    public void SetInt(int value)
    {
        Value = value.ToInvariant();
    }

    /// <summary>
    ///     Checks whether the raw text is a single decimal digit with an optional minus sign.
    /// </summary>
    /// <returns><see langword="true" /> for the deliberately narrow one-digit format accepted by this legacy check.</returns>
    public bool IsInt()
    {
        return !string.IsNullOrEmpty(Value) && Regex.IsMatch(Value, @"^\-?[0-9]$");
    }

    /// <summary>
    ///     Parses the raw text as an invariant-culture floating-point value.
    /// </summary>
    /// <returns>The parsed value.</returns>
    public double GetDouble()
    {
        return double.Parse(Value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Stores a floating-point value using invariant culture.
    /// </summary>
    /// <param name="value">The number to serialize.</param>
    public void SetDouble(double value)
    {
        Value = value.ToInvariant();
    }

    /// <summary>
    ///     Checks whether the raw text contains a plain signed decimal without exponent notation.
    /// </summary>
    /// <returns><see langword="true" /> for the restricted decimal form used by beatmap values.</returns>
    public bool IsDouble()
    {
        return !string.IsNullOrEmpty(Value) && Regex.IsMatch(Value, @"^\-?[0-9]+(\.[0-9]+)?$");
    }

    /// <summary>
    ///     Parses a comma-separated list of invariant-culture floating-point values.
    /// </summary>
    /// <returns>A new list preserving the source order.</returns>
    public List<double> GetDoubleList()
    {
        return Value.Split(',').Select(v => double.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture)).ToList();
    }
}
