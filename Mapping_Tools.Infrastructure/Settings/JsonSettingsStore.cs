using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Settings.Migrations;

namespace Mapping_Tools.Infrastructure.Settings;

/// <summary>
///     Reads versioned Mapping Tools settings and atomically writes the current
///     model-shaped JSON representation.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IApplicationDirectories directories;
    private readonly JsonSerializerOptions canonicalOptions;
    private readonly JsonSerializerOptions legacyOptions;
    private readonly Type settingsType;

    private const string schema = "mapping-tools.settings";

    /// <summary>
    ///     Creates a store for the preferences and legacy configuration paths
    ///     supplied by the application layout.
    /// </summary>
    /// <param name="directories">Provides both settings paths and required parent directories.</param>
    public JsonSettingsStore(IApplicationDirectories directories)
        : this(directories, typeof(ApplicationSettings))
    {
    }

    /// <summary>
    ///     Creates a store that materializes the supplied concrete settings type
    ///     while retaining the frontend-neutral persistence contract.
    /// </summary>
    /// <param name="directories">Provides the configuration path and required parent directories.</param>
    /// <param name="settingsType">
    ///     A non-abstract type derived from <see cref="ApplicationSettings" />.
    ///     Its inherited and declared public properties are persisted together.
    /// </param>
    public JsonSettingsStore(IApplicationDirectories directories, Type settingsType)
    {
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        ArgumentNullException.ThrowIfNull(settingsType);
        if (!typeof(ApplicationSettings).IsAssignableFrom(settingsType) || settingsType.IsAbstract)
            throw new ArgumentException(
                $"Settings type must be a non-abstract {nameof(ApplicationSettings)} subtype.",
                nameof(settingsType));

        this.settingsType = settingsType;
        canonicalOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        canonicalOptions.Converters.Add(new JsonStringEnumConverter<ApplicationTheme>());

        legacyOptions = new JsonSerializerOptions(canonicalOptions);
        legacyOptions.Converters.Add(new WindowBoundsJsonConverter());
        legacyOptions.Converters.Add(new RecentBeatmapJsonConverter());
    }

    /// <inheritdoc />
    public bool Exists => File.Exists(directories.PreferencesFile)
                          || File.Exists(directories.ConfigurationFile);

    /// <inheritdoc />
    /// <exception cref="JsonException">
    ///     The file is empty, malformed, contains an unsupported future version,
    ///     or contains invalid legacy bounds.
    /// </exception>
    public ApplicationSettings Load()
    {
        bool hasPreferences = File.Exists(directories.PreferencesFile);
        string sourcePath = hasPreferences
            ? directories.PreferencesFile
            : directories.ConfigurationFile;
        string json = File.ReadAllText(sourcePath);
        JsonObject document = ParseObject(json);
        if (!TryReadVersion(document, out int version))
        {
            ApplicationSettings legacySettings = Deserialize(json, legacyOptions);
            if (!hasPreferences) Save(legacySettings);
            return legacySettings;
        }

        string? documentSchema;
        try
        {
            documentSchema = document["$schema"]?.GetValue<string>();
        }
        catch (InvalidOperationException exception)
        {
            throw new JsonException("The settings document schema must be a string.", exception);
        }
        if (!string.Equals(documentSchema, schema, StringComparison.Ordinal))
            throw new JsonException($"The settings document has an unknown schema '{documentSchema}'.");

        int currentVersion = SettingsMigrationCatalog.CurrentVersion;
        if (version > currentVersion)
            throw new JsonException(
                $"The settings document uses unsupported version {version}; current version is {currentVersion}.");

        bool requiresRewrite = version < currentVersion;
        while (version < currentVersion)
        {
            int targetVersion = version + 1;
            SettingsMigrationCatalog.Get(targetVersion).Apply(document);
            version = targetVersion;
            document["$version"] = version;
        }

        ApplicationSettings settings = Deserialize(document.ToJsonString(), canonicalOptions);
        if (requiresRewrite || !hasPreferences) Save(settings);
        return settings;
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Serialization first targets a sibling <c>.tmp</c> file, which is moved
    ///     over <c>preferences.json</c> only after the complete JSON has been written.
    /// </remarks>
    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        directories.EnsureCreated();

        JsonObject document = (JsonSerializer.SerializeToNode(
                                   settings,
                                   settings.GetType(),
                                   canonicalOptions)
                               as JsonObject)
                               ?? throw new JsonException("The settings model did not serialize to a JSON object.");
        document["$schema"] = schema;
        document["$version"] = SettingsMigrationCatalog.CurrentVersion;

        string json = document.ToJsonString(canonicalOptions);
        PhysicalAtomicFileWriter.WriteText(
            directories.PreferencesFile,
            json,
            PhysicalAtomicFileWriter.Utf8WithoutBom);
    }

    private static JsonObject ParseObject(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonNode.Parse(json) as JsonObject
               ?? throw new JsonException("The settings document must contain a JSON object.");
    }

    private static bool TryReadVersion(JsonObject document, out int version)
    {
        JsonNode? value = document["$version"];
        if (value is null)
        {
            version = default;
            return false;
        }

        try
        {
            version = value.GetValue<int>();
            if (version < 1)
                throw new JsonException("The settings document version must be positive.");

            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            throw new JsonException("The settings document version must be an integer.", exception);
        }
    }

    private ApplicationSettings Deserialize(string json, JsonSerializerOptions serializerOptions)
    {
        return (JsonSerializer.Deserialize(json, settingsType, serializerOptions) as ApplicationSettings)
               ?? throw new JsonException("The settings document contained no JSON value.");
    }

    private sealed class WindowBoundsJsonConverter : JsonConverter<WindowBounds>
    {
        /// <summary>
        ///     Parses the comma-separated <c>x,y,width,height</c> representation
        ///     emitted for the legacy WPF <c>Rect</c> property.
        /// </summary>
        public override WindowBounds Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string value = reader.GetString()
                           ?? throw new JsonException("Window bounds cannot be null.");
            string[] parts = value.Split(',');
            if (parts.Length != 4) throw new JsonException($"Invalid legacy window bounds value '{value}'.");

            try
            {
                return new WindowBounds(
                    double.Parse(parts[0], CultureInfo.InvariantCulture),
                    double.Parse(parts[1], CultureInfo.InvariantCulture),
                    double.Parse(parts[2], CultureInfo.InvariantCulture),
                    double.Parse(parts[3], CultureInfo.InvariantCulture));
            }
            catch (Exception exception) when (
                exception is FormatException or OverflowException)
            {
                throw new JsonException(
                    $"Invalid legacy window bounds value '{value}'.",
                    exception);
            }
        }

        /// <summary>
        ///     Rejects writes because the comma-separated legacy shape is only
        ///     supported while reading the legacy configuration file.
        /// </summary>
        public override void Write(
            Utf8JsonWriter writer,
            WindowBounds value,
            JsonSerializerOptions options)
        {
            throw new NotSupportedException("Legacy settings values are read-only.");
        }
    }

    private sealed class RecentBeatmapJsonConverter : JsonConverter<RecentBeatmap>
    {
        public override RecentBeatmap Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray || !reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException(
                    "A recent beatmap must be a [path, display date] string array.");

            string path = reader.GetString()
                          ?? throw new JsonException("A recent beatmap path cannot be null.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                throw new JsonException(
                    "A recent beatmap must include its display date as the second value.");

            string displayDate = reader.GetString()
                                 ?? throw new JsonException("A recent beatmap display date cannot be null.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException(
                    "A recent beatmap must contain exactly two string values.");

            return new RecentBeatmap(path, displayDate);
        }

        public override void Write(
            Utf8JsonWriter writer,
            RecentBeatmap value,
            JsonSerializerOptions options)
        {
            throw new NotSupportedException("Legacy settings values are read-only.");
        }
    }
}
