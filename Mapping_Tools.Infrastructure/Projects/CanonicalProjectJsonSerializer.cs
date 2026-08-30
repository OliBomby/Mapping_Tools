using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Application.Projects.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Infrastructure.Projects;

internal static class CanonicalProjectJsonSerializer
{
    internal static string Serialize<TProject>(ToolConfigSchema schema, TProject project)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(project);

        JsonSerializer serializer = CreateSerializer();
        JObject document = JObject.FromObject(project, serializer);
        document.AddFirst(new JProperty("$version", schema.CurrentVersion));
        document.AddFirst(new JProperty("$schema", schema.Id));
        return document.ToString(Formatting.Indented);
    }

    internal static TProject Deserialize<TProject>(JObject document)
    {
        JsonSerializer serializer = CreateSerializer();
        return document.ToObject<TProject>(serializer)
               ?? throw new InvalidDataException("The project document contained a JSON null root.");
    }

    private static JsonSerializer CreateSerializer()
    {
        JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.Indented,
            Converters =
            [
                new CanonicalGeneratorSettingsDictionaryConverter(),
                new CanonicalGraphStateConverter(),
                new CanonicalRelevantObjectCollectionConverter(),
            ],
        });
        return serializer;
    }

    private sealed class CanonicalGeneratorSettingsDictionaryConverter : JsonConverter
    {
        private static readonly IReadOnlyDictionary<Type, string> generatorIds =
            new Dictionary<Type, string>
            {
                [typeof(AnchorPointGenerator)] = "anchor-point",
                [typeof(AngleBisectorGenerator)] = "angle-bisector",
                [typeof(AveragePointGenerator2)] = "average-point-2",
                [typeof(AveragePointGenerator3)] = "average-point-3",
                [typeof(CircleTangentGenerator)] = "circle-tangent",
                [typeof(EqualSpacingGenerator)] = "equal-spacing",
                [typeof(IntersectionGenerator)] = "intersection",
                [typeof(LastAnchorGenerator)] = "last-anchor",
                [typeof(LinearLineGenerator)] = "linear-line",
                [typeof(LineGenerator)] = "line",
                [typeof(ParallelismGenerator)] = "parallelism",
                [typeof(PerfectCircleBlanketGenerator)] = "perfect-circle-blanket",
                [typeof(PerfectCircleGenerator)] = "perfect-circle",
                [typeof(PerpendicularGenerator)] = "perpendicular",
                [typeof(PointBisectorGenerator)] = "point-bisector",
                [typeof(SameTransformGenerator2)] = "same-transform-2",
                [typeof(SameTransformGenerator3)] = "same-transform-3",
                [typeof(SameTransformGenerator3Reversed)] = "same-transform-3-reversed",
                [typeof(SameTransformGenerator4)] = "same-transform-4",
                [typeof(ScaleRotateGenerator)] = "scale-rotate",
                [typeof(SinglePointCircleGenerator)] = "single-point-circle",
                [typeof(SliderEndGenerator)] = "slider-end",
                [typeof(SliderPathGenerator)] = "slider-path",
                [typeof(SquareGenerator)] = "square",
                [typeof(SquareGenerator2)] = "square-2",
                [typeof(StartPointGenerator)] = "start-point",
                [typeof(SymmetryGenerator)] = "symmetry",
                [typeof(TangentCircleGenerator)] = "tangent-circle",
                [typeof(TriangleGenerator)] = "triangle",
                [typeof(TriangleGenerator2)] = "triangle-2",
            };

        private static readonly IReadOnlyDictionary<string, Type> generatorTypes = generatorIds
            .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<Type, string> settingsIds =
            new Dictionary<Type, string>
            {
                [typeof(GeneratorSettings)] = "default",
                [typeof(ScaleRotateGeneratorSettings)] = "scale-rotate",
                [typeof(SinglePointCircleGeneratorSettings)] = "single-point-circle",
                [typeof(SliderPathGeneratorSettings)] = "slider-path",
                [typeof(SymmetryGeneratorSettings)] = "symmetry",
            };

        private static readonly IReadOnlyDictionary<string, Type> settingsTypes = settingsIds
            .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<Type, GeneratorSettings>);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Dictionary<Type, GeneratorSettings> settings)
                throw new JsonSerializationException("Geometry Dashboard generator settings were invalid.");

            writer.WriteStartObject();
            foreach ((Type generatorType, GeneratorSettings generatorSettings) in settings)
            {
                if (!generatorIds.TryGetValue(generatorType, out string? generatorId))
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard generator '{generatorType.FullName}' has no stable identifier.");

                if (!settingsIds.TryGetValue(generatorSettings.GetType(), out string? settingsId))
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard settings type '{generatorSettings.GetType().FullName}' has no stable identifier.");

                JObject settingsJson = JObject.FromObject(generatorSettings, serializer);
                settingsJson.AddFirst(new JProperty("$kind", settingsId));
                writer.WritePropertyName(generatorId);
                settingsJson.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            Dictionary<Type, GeneratorSettings> settings = new();
            foreach (JProperty property in json.Properties())
            {
                if (!generatorTypes.TryGetValue(property.Name, out Type? generatorType))
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard generator identifier '{property.Name}' is unknown.");

                string? settingsId = property.Value["$kind"]?.Value<string>();
                Type settingsType = typeof(GeneratorSettings);
                if (settingsId is not null)
                {
                    if (!settingsTypes.TryGetValue(settingsId, out Type? knownSettingsType))
                        throw new JsonSerializationException(
                            $"The Geometry Dashboard settings identifier '{settingsId}' is unknown.");

                    settingsType = knownSettingsType;
                }

                settings[generatorType] = property.Value.ToObject(settingsType, serializer) as GeneratorSettings
                                           ?? throw new JsonSerializationException(
                                               $"The Geometry Dashboard settings for '{property.Name}' were null.");
            }

            return settings;
        }
    }

    private sealed class CanonicalGraphStateConverter : JsonConverter
    {
        private static readonly IReadOnlyDictionary<Type, string> interpolatorIds =
            new Dictionary<Type, string>
            {
                [typeof(SingleCurveInterpolator)] = "single-curve",
                [typeof(SingleCurveInterpolator2)] = "single-curve-2",
                [typeof(SingleCurveInterpolator3)] = "single-curve-3",
                [typeof(DoubleCurveInterpolator)] = "double-curve",
                [typeof(DoubleCurveInterpolator2)] = "double-curve-2",
                [typeof(DoubleCurveInterpolator3)] = "double-curve-3",
                [typeof(HalfSineInterpolator)] = "half-sine",
                [typeof(WaveInterpolator)] = "wave",
                [typeof(ParabolaInterpolator)] = "parabola",
            };

        private static readonly IReadOnlyDictionary<string, Type> interpolatorTypes = interpolatorIds
            .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(GraphState);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not GraphState graph)
                throw new JsonSerializationException("The graph state was invalid.");

            JObject json = new()
            {
                ["Anchors"] = new JArray(graph.Anchors.Select(anchor => SerializeAnchor(anchor, serializer))),
                ["MinX"] = graph.MinX,
                ["MinY"] = graph.MinY,
                ["MaxX"] = graph.MaxX,
                ["MaxY"] = graph.MaxY,
            };
            json.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            GraphState graph = new([], 0, 0, 1, 1)
            {
                MinX = json["MinX"]?.Value<double>() ?? 0,
                MinY = json["MinY"]?.Value<double>() ?? 0,
                MaxX = json["MaxX"]?.Value<double>() ?? 1,
                MaxY = json["MaxY"]?.Value<double>() ?? 1,
            };

            if (json["Anchors"] is JArray anchors)
                graph.Anchors = anchors
                    .Select(anchor => DeserializeAnchor(anchor, serializer))
                    .ToList();

            return graph;
        }

        private static JObject SerializeAnchor(GraphAnchor anchor, JsonSerializer serializer)
        {
            JObject json = new()
            {
                ["Pos"] = JObject.FromObject(anchor.Pos, serializer),
                ["Tension"] = anchor.Tension,
            };

            if (interpolatorIds.TryGetValue(anchor.Interpolator.GetType(), out string? interpolatorId))
            {
                JObject interpolator = JObject.FromObject(anchor.Interpolator, serializer);
                interpolator.AddFirst(new JProperty("$kind", interpolatorId));
                json["Interpolator"] = interpolator;
            }

            return json;
        }

        private static GraphAnchor DeserializeAnchor(JToken token, JsonSerializer serializer)
        {
            JObject json = token as JObject
                          ?? throw new JsonSerializationException("A graph anchor must be a JSON object.");
            Vector2 position = json["Pos"]?.ToObject<Vector2>(serializer)
                               ?? throw new JsonSerializationException("A graph anchor did not contain a position.");
            double tension = json["Tension"]?.Value<double>() ?? 0;
            IGraphInterpolator interpolator = new SingleCurveInterpolator();

            if (json["Interpolator"] is JObject interpolatorJson)
            {
                string? interpolatorId = interpolatorJson["$kind"]?.Value<string>();
                if (interpolatorId is not null && interpolatorTypes.TryGetValue(interpolatorId, out Type? interpolatorType))
                    interpolator = GraphInterpolatorCatalog.GetInterpolator(interpolatorType);
                else if (interpolatorId is not null)
                    throw new JsonSerializationException(
                        $"The graph interpolator identifier '{interpolatorId}' is unknown.");

                if (interpolatorJson["P"] is not null)
                    interpolator.P = interpolatorJson["P"]!.Value<double>();
            }

            return new GraphAnchor(position, interpolator, tension);
        }
    }

    private sealed class CanonicalRelevantObjectCollectionConverter : JsonConverter
    {
        private static readonly IReadOnlyDictionary<Type, string> objectIds =
            new Dictionary<Type, string>
            {
                [typeof(RelevantHitObject)] = "relevant-hit-object",
                [typeof(RelevantPoint)] = "relevant-point",
                [typeof(RelevantLine)] = "relevant-line",
                [typeof(RelevantCircle)] = "relevant-circle",
            };

        private static readonly IReadOnlyDictionary<string, Type> objectTypes = objectIds
            .ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(RelevantObjectCollection);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not RelevantObjectCollection collection)
                throw new JsonSerializationException("The Geometry Dashboard object collection was invalid.");

            writer.WriteStartObject();
            foreach ((Type objectType, List<IRelevantObject> objects) in collection)
            {
                if (!objectIds.TryGetValue(objectType, out string? objectId))
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard object type '{objectType.FullName}' has no stable identifier.");

                writer.WritePropertyName(objectId);
                writer.WriteStartArray();
                foreach (IRelevantObject relevantObject in objects)
                {
                    JObject objectJson = JObject.FromObject(relevantObject, serializer);
                    objectJson.WriteTo(writer);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            RelevantObjectCollection collection = new();
            foreach (JProperty property in json.Properties())
            {
                if (property.Name is "$schema" or "$version") continue;

                if (!objectTypes.TryGetValue(property.Name, out Type? relevantObjectType))
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard object identifier '{property.Name}' is unknown.");

                if (property.Value is not JArray objectsJson)
                    throw new JsonSerializationException(
                        $"The Geometry Dashboard object group '{property.Name}' must be a JSON array.");

                List<IRelevantObject> objects = [];
                foreach (JToken objectJson in objectsJson)
                {
                    objects.Add(objectJson.ToObject(relevantObjectType, serializer) as IRelevantObject
                                ?? throw new JsonSerializationException(
                                    $"The Geometry Dashboard object group '{property.Name}' contained a null object."));
                }

                collection[relevantObjectType] = objects;
            }

            return collection;
        }
    }
}
