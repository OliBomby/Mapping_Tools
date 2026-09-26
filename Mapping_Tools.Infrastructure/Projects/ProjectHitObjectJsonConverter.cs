using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
///     Preserves the former line-backed HitObject shape in saved project JSON.
/// </summary>
internal sealed class ProjectHitObjectJsonConverter : JsonConverter
{
    private const int decode_format_version = 14;
    private const int encode_format_version = 128;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(HitObject);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not HitObject hitObject)
            throw new JsonSerializationException("A project hit object cannot be null.");

        writer.WriteStartObject();
        WriteLegacyTypeName(writer, serializer);

        writer.WritePropertyName("Line");
        writer.WriteValue(EncodeLine(hitObject));

        writer.WritePropertyName(nameof(hitObject.ActualNewCombo));
        serializer.Serialize(writer, hitObject.ActualNewCombo);
        writer.WritePropertyName(nameof(hitObject.ComboIndex));
        serializer.Serialize(writer, hitObject.ComboIndex);
        writer.WritePropertyName(nameof(hitObject.ColourIndex));
        serializer.Serialize(writer, hitObject.ColourIndex);
        WriteIfNotNull(writer, serializer, nameof(hitObject.Colour), hitObject.Colour);
        writer.WritePropertyName(nameof(hitObject.TemporalLength));
        serializer.Serialize(writer, hitObject.TemporalLength);
        writer.WritePropertyName(nameof(hitObject.SliderVelocity));
        serializer.Serialize(writer, hitObject.SliderVelocity);
        WriteIfNotNull(writer, serializer, nameof(hitObject.TimingPoint), hitObject.TimingPoint);
        WriteIfNotNull(writer, serializer, nameof(hitObject.HitsoundTimingPoint), hitObject.HitsoundTimingPoint);
        WriteIfNotNull(writer, serializer, nameof(hitObject.UnInheritedTimingPoint), hitObject.UnInheritedTimingPoint);

        writer.WriteEndObject();
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject json = JObject.Load(reader);
        HitObject hitObject = json.TryGetValue("Line", StringComparison.OrdinalIgnoreCase, out JToken? lineToken)
            ? DecodeLine(lineToken.Value<string>()
                         ?? throw new JsonSerializationException("A project hit-object Line must be a string."))
            : new HitObject();

        ReadIfPresent<bool>(json, serializer, nameof(hitObject.ActualNewCombo), value => hitObject.ActualNewCombo = value);
        ReadIfPresent<int>(json, serializer, nameof(hitObject.ComboIndex), value => hitObject.ComboIndex = value);
        ReadIfPresent<int>(json, serializer, nameof(hitObject.ColourIndex), value => hitObject.ColourIndex = value);
        ReadIfPresent<ComboColour?>(json, serializer, nameof(hitObject.Colour), value => hitObject.Colour = value);
        ReadIfPresent<double>(json, serializer, nameof(hitObject.TemporalLength), value => hitObject.TemporalLength = value);
        ReadIfPresent<double>(json, serializer, nameof(hitObject.SliderVelocity), value => hitObject.SliderVelocity = value);
        ReadIfPresent<TimingPoint?>(json, serializer, nameof(hitObject.TimingPoint), value => hitObject.TimingPoint = value);
        ReadIfPresent<TimingPoint?>(json, serializer, nameof(hitObject.HitsoundTimingPoint), value => hitObject.HitsoundTimingPoint = value);
        ReadIfPresent<TimingPoint?>(json, serializer, nameof(hitObject.UnInheritedTimingPoint), value => hitObject.UnInheritedTimingPoint = value);

        return hitObject;
    }

    private static string EncodeLine(HitObject hitObject)
    {
        Beatmap beatmap = new() { Version = encode_format_version };
        beatmap.HitObjects.Add(hitObject);

        string[] lines = new BeatmapEncoder().Encode(beatmap).Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
        int hitObjectsHeader = Array.IndexOf(lines, "[HitObjects]");
        if (hitObjectsHeader < 0 || hitObjectsHeader + 1 >= lines.Length || lines[hitObjectsHeader + 1].Length == 0)
            throw new JsonSerializationException("The project hit object could not be encoded.");

        return lines[hitObjectsHeader + 1];
    }

    private static HitObject DecodeLine(string line)
    {
        string document = $"osu file format v{decode_format_version}\r\n[Difficulty]\r\nSliderMultiplier: 1.4\r\n[HitObjects]\r\n{line}\r\n";
        try
        {
            return new BeatmapDecoder().Decode(document).HitObjects.Single();
        }
        catch (Exception exception) when (exception is not JsonSerializationException)
        {
            throw new JsonSerializationException("The project hit-object Line could not be decoded.", exception);
        }
    }

    private static void WriteLegacyTypeName(JsonWriter writer, JsonSerializer serializer)
    {
        if (serializer.TypeNameHandling == TypeNameHandling.None) return;

        string? assemblyName = null;
        string? typeName = null;
        serializer.SerializationBinder?.BindToName(typeof(HitObject), out assemblyName, out typeName);
        typeName ??= typeof(HitObject).FullName;
        assemblyName ??= typeof(HitObject).Assembly.GetName().Name;

        writer.WritePropertyName("$type");
        writer.WriteValue($"{typeName}, {assemblyName}");
    }

    private static void WriteIfNotNull(JsonWriter writer, JsonSerializer serializer, string propertyName, object? value)
    {
        if (value is null && serializer.NullValueHandling == NullValueHandling.Ignore) return;

        writer.WritePropertyName(propertyName);
        serializer.Serialize(writer, value);
    }

    private static void ReadIfPresent<T>(JObject json, JsonSerializer serializer, string propertyName, Action<T> assign)
    {
        if (!json.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out JToken? token)) return;

        T? value = token.ToObject<T>(serializer);
        if (value is not null) assign(value);
    }
}
