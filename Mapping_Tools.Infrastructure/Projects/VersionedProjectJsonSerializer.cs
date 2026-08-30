using System.Text.Json.Nodes;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Infrastructure.Projects;

/// <summary>
///     Reads current project documents and legacy project files while writing
///     only the current stable, model-shaped format.
/// </summary>
public sealed class VersionedProjectJsonSerializer : IProjectSerializer
{
    private readonly LegacyProjectJsonReader legacyReader = new();

    /// <inheritdoc />
    public string Serialize<TProject>(TProject project)
    {
        return Serialize(ToolConfigSchema.Default, project);
    }

    /// <inheritdoc />
    public string Serialize<TProject>(ToolConfigSchema schema, TProject project)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return CanonicalProjectJsonSerializer.Serialize(schema, project);
    }

    /// <inheritdoc />
    public TProject Deserialize<TProject>(string json)
    {
        return Deserialize<TProject>(ToolConfigSchema.Default, json);
    }

    /// <inheritdoc />
    public TProject Deserialize<TProject>(ToolConfigSchema schema, string json)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JObject document = ParseObject(json);
        if (!TryReadVersion(document, out int version))
            return legacyReader.Read<TProject>(json);

        string? documentSchema = document["$schema"]?.Value<string>();
        if (!string.Equals(documentSchema, schema.Id, StringComparison.Ordinal)
            && !string.Equals(documentSchema, ToolConfigSchema.Default.Id, StringComparison.Ordinal))
            throw new JsonSerializationException(
                $"The project document has an unknown schema '{documentSchema}'.");

        int currentVersion = schema.CurrentVersion;
        if (version > currentVersion)
            throw new JsonSerializationException(
                $"The project document uses unsupported version {version}; current version is {currentVersion}.");

        while (version < currentVersion)
        {
            int targetVersion = version + 1;
            IConfigMigration migration = schema.Migrations.FirstOrDefault(
                candidate => candidate.ToVersion == targetVersion)
                ?? throw new InvalidDataException(
                    $"No configuration migration exists for schema '{schema.Id}' and target version {targetVersion}.");
            JsonObject migrated = JsonNode.Parse(document.ToString(Formatting.None)) as JsonObject
                                  ?? throw new JsonSerializationException(
                                      "The project document root must be a JSON object.");
            migration.Apply(migrated);
            document = JObject.Parse(migrated.ToJsonString());
            version = targetVersion;
            document["$version"] = version;
        }

        return CanonicalProjectJsonSerializer.Deserialize<TProject>(document);
    }

    private static JObject ParseObject(string json)
    {
        try
        {
            JToken token = JToken.Parse(json);
            if (token.Type == JTokenType.Null)
                throw new InvalidDataException("The project document contained a JSON null root.");

            return token as JObject
                   ?? throw new JsonSerializationException("The project document root must be a JSON object.");
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new JsonSerializationException("The project document was not a JSON object.", exception);
        }
    }

    private static bool TryReadVersion(JObject document, out int version)
    {
        JToken? value = document["$version"];
        if (value is null)
        {
            version = default;
            return false;
        }

        if (value.Type != JTokenType.Integer)
            throw new JsonSerializationException("The project document version must be an integer.");

        try
        {
            version = value.Value<int>();
        }
        catch (Exception exception) when (exception is FormatException or OverflowException)
        {
            throw new JsonSerializationException("The project document version must be an integer.", exception);
        }
        if (version < 1)
            throw new JsonSerializationException("The project document version must be positive.");

        return true;
    }
}
