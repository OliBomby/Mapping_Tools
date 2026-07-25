using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;

namespace Mapping_Tools.Infrastructure.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly IApplicationDirectories _directories;
    private readonly JsonSerializerOptions _options;

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
    }

    public bool Exists => File.Exists(_directories.ConfigurationFile);

    public ApplicationSettings Load()
    {
        string json = File.ReadAllText(_directories.ConfigurationFile);
        return JsonSerializer.Deserialize<ApplicationSettings>(json, _options)
            ?? throw new JsonException("The settings document contained no JSON value.");
    }

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
}
