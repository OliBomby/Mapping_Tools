using Mapping_Tools.ApplicationServices.Projects;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
/// Preserves the Newtonsoft type metadata and vector representation emitted by
/// legacy Mapping Tools project files, including domain types moved to Core.
/// </summary>
/// <remarks>
/// Project JSON historically records concrete CLR type names. This serializer
/// is intentionally limited to trusted local project files because enabling
/// that compatibility for untrusted documents would permit construction of
/// types named by the input.
/// </remarks>
public sealed class LegacyProjectJsonSerializer : IProjectSerializer
{
    private static readonly Type MigratedCoreMarker = typeof(Beatmap);

    /// <summary>
    /// Serializes the runtime object graph with legacy simple assembly names,
    /// indented formatting, omitted nulls, and ignored reference loops.
    /// </summary>
    /// <typeparam name="TProject">The concrete project model being persisted.</typeparam>
    /// <param name="project">The complete non-null project snapshot.</param>
    /// <returns>JSON compatible with the former WPF <c>ProjectManager</c>.</returns>
    public string Serialize<TProject>(TProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return JsonConvert.SerializeObject(project, CreateSettings());
    }

    /// <summary>
    /// Restores a trusted project document while redirecting legacy
    /// <c>Mapping Tools</c> domain type names to their current Core assembly.
    /// </summary>
    /// <typeparam name="TProject">The root project model expected by the feature.</typeparam>
    /// <param name="json">A complete legacy or newly written project document.</param>
    /// <returns>The reconstructed non-null project.</returns>
    /// <exception cref="ArgumentException"><paramref name="json"/> is blank.</exception>
    /// <exception cref="InvalidDataException">The JSON root resolves to null.</exception>
    public TProject Deserialize<TProject>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonConvert.DeserializeObject<TProject>(json, CreateSettings())
            ?? throw new InvalidDataException("The project document contained a JSON null root.");
    }

    private static JsonSerializerSettings CreateSettings()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            SerializationBinder = new LegacyProjectTypeBinder(),
            Converters = [new Vector2Converter()]
        };
    }

    private sealed class LegacyProjectTypeBinder : ISerializationBinder
    {
        private const string LegacyAssemblyName = "Mapping Tools";
        private readonly DefaultSerializationBinder _fallback = new();

        public Type BindToType(string? assemblyName, string typeName)
        {
            if (assemblyName == LegacyAssemblyName)
            {
                Type? migratedType = MigratedCoreMarker.Assembly.GetType(typeName);
                if (migratedType is not null)
                {
                    return migratedType;
                }
            }

            return _fallback.BindToType(assemblyName, typeName);
        }

        public void BindToName(
            Type serializedType,
            out string? assemblyName,
            out string? typeName)
        {
            if (serializedType.Assembly == MigratedCoreMarker.Assembly)
            {
                assemblyName = LegacyAssemblyName;
                typeName = serializedType.FullName;
                return;
            }

            _fallback.BindToName(serializedType, out assemblyName, out typeName);
        }
    }

    private sealed class Vector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2) || objectType == typeof(Vector2?);
        }

        public override void WriteJson(
            JsonWriter writer,
            object? value,
            JsonSerializer serializer)
        {
            Vector2 vector = (Vector2)(value
                ?? throw new JsonSerializationException("A Vector2 value cannot be null."));

            writer.WriteStartObject();
            writer.WritePropertyName("X");
            serializer.Serialize(writer, vector.X);
            writer.WritePropertyName("Y");
            serializer.Serialize(writer, vector.Y);
            writer.WriteEndObject();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            double x = default;
            double y = default;
            bool gotX = false;
            bool gotY = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    continue;
                }

                string? propertyName = reader.Value as string;
                if (!reader.Read())
                {
                    break;
                }

                switch (propertyName)
                {
                    case "X":
                        x = serializer.Deserialize<double>(reader);
                        gotX = true;
                        break;
                    case "Y":
                        y = serializer.Deserialize<double>(reader);
                        gotY = true;
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (!gotX || !gotY)
            {
                throw new InvalidDataException(
                    "A legacy Vector2 object must contain numeric X and Y properties.");
            }

            return new Vector2(x, y);
        }
    }
}
