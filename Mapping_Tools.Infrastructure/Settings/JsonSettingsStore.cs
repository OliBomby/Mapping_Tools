using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Infrastructure.Settings;

/// <summary>
/// Reads and atomically replaces the legacy-compatible Mapping Tools JSON
/// configuration using a frontend-neutral settings model.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IApplicationDirectories _directories;
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Creates a store for the configuration path supplied by the application layout.
    /// </summary>
    /// <param name="directories">Provides the configuration path and required parent directories.</param>
    public JsonSettingsStore(IApplicationDirectories directories)
    {
        _directories = directories ?? throw new ArgumentNullException(nameof(directories));
        _options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _options.Converters.Add(new WindowBoundsJsonConverter());
        _options.Converters.Add(new RecentBeatmapJsonConverter());
        _options.Converters.Add(new JsonStringEnumConverter<ApplicationTheme>());
    }

    /// <inheritdoc/>
    public bool Exists => File.Exists(_directories.ConfigurationFile);

    /// <inheritdoc/>
    /// <exception cref="JsonException">The file is empty, malformed, or contains invalid legacy bounds.</exception>
    public ApplicationSettings Load()
    {
        string json = File.ReadAllText(_directories.ConfigurationFile);
        return JsonSerializer.Deserialize<ApplicationSettings>(json, _options)
            ?? throw new JsonException("The settings document contained no JSON value.");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Serialization first targets a sibling <c>.tmp</c> file, which is moved
    /// over the configuration only after the complete JSON has been written.
    /// </remarks>
    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _directories.EnsureCreated();

        string json = JsonSerializer.Serialize(settings, _options);
        string temporaryPath = _directories.ConfigurationFile + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(
                temporaryPath,
                _directories.ConfigurationFile,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed class WindowBoundsJsonConverter : JsonConverter<WindowBounds>
    {
        /// <summary>
        /// Parses the comma-separated <c>x,y,width,height</c> representation
        /// emitted for the legacy WPF <c>Rect</c> property.
        /// </summary>
        public override WindowBounds Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            string value = reader.GetString()
                ?? throw new JsonException("Window bounds cannot be null.");
            string[] parts = value.Split(',');
            if (parts.Length != 4)
            {
                throw new JsonException($"Invalid legacy window bounds value '{value}'.");
            }

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
        /// Writes bounds in the invariant comma-separated form understood by
        /// both the old Newtonsoft/WPF model and the new settings model.
        /// </summary>
        public override void Write(
            Utf8JsonWriter writer,
            WindowBounds value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(string.Join(
                ",",
                value.X.ToString(CultureInfo.InvariantCulture),
                value.Y.ToString(CultureInfo.InvariantCulture),
                value.Width.ToString(CultureInfo.InvariantCulture),
                value.Height.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private sealed class RecentBeatmapJsonConverter : JsonConverter<RecentBeatmap>
    {
        public override RecentBeatmap Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray ||
                !reader.Read() ||
                reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException(
                    "A recent beatmap must be a [path, display date] string array.");
            }

            string path = reader.GetString()
                ?? throw new JsonException("A recent beatmap path cannot be null.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException(
                    "A recent beatmap must include its display date as the second value.");
            }

            string displayDate = reader.GetString()
                ?? throw new JsonException("A recent beatmap display date cannot be null.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException(
                    "A recent beatmap must contain exactly two string values.");
            }

            return new RecentBeatmap(path, displayDate);
        }

        public override void Write(
            Utf8JsonWriter writer,
            RecentBeatmap value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Path);
            writer.WriteStringValue(value.DisplayDate);
            writer.WriteEndArray();
        }
    }
}
