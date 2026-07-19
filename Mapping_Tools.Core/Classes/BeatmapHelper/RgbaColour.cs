using Newtonsoft.Json;

namespace Mapping_Tools.Classes.BeatmapHelper;

[JsonConverter(typeof(RgbaColourJsonConverter))]
public readonly record struct RgbaColour(byte A, byte R, byte G, byte B) {
    public static RgbaColour White => FromRgb(255, 255, 255);
    public static RgbaColour FromRgb(byte r, byte g, byte b) => new(255, r, g, b);
    public static RgbaColour FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);
    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}

internal sealed class RgbaColourJsonConverter : JsonConverter<RgbaColour> {
    public override void WriteJson(JsonWriter writer, RgbaColour value, JsonSerializer serializer) {
        writer.WriteValue(value.ToString());
    }

    public override RgbaColour ReadJson(JsonReader reader, Type objectType, RgbaColour existingValue,
        bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType != JsonToken.String || reader.Value is not string value)
            throw new JsonSerializationException("Expected a colour string in #AARRGGBB format.");

        string hex = value.StartsWith('#') ? value[1..] : value;
        if (hex.Length == 6) hex = "FF" + hex;
        if (hex.Length != 8 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out uint argb))
            throw new JsonSerializationException($"Invalid colour value '{value}'.");

        return RgbaColour.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
    }
}
