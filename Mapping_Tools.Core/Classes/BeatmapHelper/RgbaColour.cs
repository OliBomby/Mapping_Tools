using System.Globalization;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Classes.BeatmapHelper;

/// <summary>
///     Stores an eight-bit alpha, red, green, and blue colour value in ARGB order.
/// </summary>
/// <param name="A">The alpha channel, where zero is transparent.</param>
/// <param name="R">The red channel.</param>
/// <param name="G">The green channel.</param>
/// <param name="B">The blue channel.</param>
[JsonConverter(typeof(RgbaColourJsonConverter))]
public readonly record struct RgbaColour(byte A, byte R, byte G, byte B)
{
    /// <summary>
    ///     Gets fully opaque white.
    /// </summary>
    public static RgbaColour White => FromRgb(255, 255, 255);

    /// <summary>
    ///     Creates a fully opaque colour from RGB channels.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <returns>An ARGB colour with alpha set to 255.</returns>
    public static RgbaColour FromRgb(byte r, byte g, byte b)
    {
        return new RgbaColour(255, r, g, b);
    }

    /// <summary>
    ///     Creates a colour from explicit ARGB channels.
    /// </summary>
    /// <param name="a">The alpha channel.</param>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <returns>A colour containing the supplied channels unchanged.</returns>
    public static RgbaColour FromArgb(byte a, byte r, byte g, byte b)
    {
        return new RgbaColour(a, r, g, b);
    }

    /// <summary>
    ///     Formats the colour as an eight-digit uppercase ARGB hexadecimal string.
    /// </summary>
    /// <returns><c>#AARRGGBB</c>.</returns>
    public override string ToString()
    {
        return $"#{A:X2}{R:X2}{G:X2}{B:X2}";
    }
}

internal sealed class RgbaColourJsonConverter : JsonConverter<RgbaColour>
{
    /// <summary>
    ///     Writes the colour using its stable <c>#AARRGGBB</c> representation.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The colour to serialize.</param>
    /// <param name="serializer">The serializer.</param>
    public override void WriteJson(JsonWriter writer, RgbaColour value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    /// <summary>
    ///     Reads either <c>#RRGGBB</c> or <c>#AARRGGBB</c>, treating six-digit values as opaque.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="objectType">The object type.</param>
    /// <param name="existingValue">The existing value.</param>
    /// <param name="hasExistingValue">The has existing value.</param>
    /// <param name="serializer">The serializer.</param>
    /// <returns>The parsed colour.</returns>
    public override RgbaColour ReadJson(JsonReader reader, Type objectType, RgbaColour existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String || reader.Value is not string value)
            throw new JsonSerializationException("Expected a colour string in #AARRGGBB format.");

        string hex = value.StartsWith('#') ? value[1..] : value;
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length == 8
            && uint.TryParse(hex, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out uint argb))
            return RgbaColour.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);

        string[] channels = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (channels.Length == 3
            && channels.All(channel => byte.TryParse(
                channel,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out _)))
            return RgbaColour.FromRgb(
                byte.Parse(channels[0], CultureInfo.InvariantCulture),
                byte.Parse(channels[1], CultureInfo.InvariantCulture),
                byte.Parse(channels[2], CultureInfo.InvariantCulture));

        throw new JsonSerializationException($"Invalid colour value '{value}'.");
    }
}
