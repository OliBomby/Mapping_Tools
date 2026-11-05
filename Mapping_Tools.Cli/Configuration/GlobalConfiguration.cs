using System.Globalization;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Cli.Commands;
using Mapping_Tools.Cli.Localization;
using Mapping_Tools.Infrastructure.Files;
using Newtonsoft.Json.Linq;

namespace Mapping_Tools.Cli.Configuration;

/// <summary>Loads and persists the CLI-only allowlist of global defaults from a TOML file.</summary>
/// <param name="path">Configuration-file path, made absolute when this instance is created.</param>
/// <remarks>
/// Beatmap targets, object selections, overwrite permission, and reload requests are not persistable defaults.
/// Paths read from the file resolve relative to its directory; paths entered through config set resolve relative to
/// the current directory.
/// </remarks>
internal sealed class GlobalConfiguration(string path)
{
    private readonly PhysicalBeatmapsetFileSystem files = new();

    /// <summary>Persistable option names and their expected value types.</summary>
    internal static IReadOnlyDictionary<string, Type> Options { get; } = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [CliOptions.OsuPath.Name] = typeof(string),
        [CliOptions.SongsPath.Name] = typeof(string),
        [CliOptions.OsuConfig.Name] = typeof(string),
        [CliOptions.BackupPath.Name] = typeof(string),
        [CliOptions.MakeBackups.Name] = typeof(bool),
        [CliOptions.MaxBackups.Name] = typeof(int),
        [CliOptions.EditorReader.Name] = typeof(BeatmapLiveStateReadingMode),
        [CliOptions.CurrentBeatmapFetching.Name] = typeof(CurrentBeatmapFetchingMode),
        [CliOptions.EditorReloadMode.Name] = typeof(EditorReloadMode),
        [CliOptions.AutoDetectLazerExternalEdit.Name] = typeof(bool),
    };

    /// <summary>The default application-data <c>cli.toml</c> path, independent of Desktop preferences.</summary>
    internal static string DefaultPath => Path.Combine(new ApplicationDirectories().ApplicationData, "cli.toml");

    /// <summary>The absolute file selected for this configuration instance.</summary>
    internal string FilePath { get; } = Path.GetFullPath(path);

    /// <summary>Reads, validates, and normalizes saved defaults without changing the file.</summary>
    /// <param name="required">Whether a missing explicitly selected file is an error.</param>
    /// <returns>Normalized saved values, or an empty object when an optional file is absent.</returns>
    /// <exception cref="IOException">The file cannot be read or contains invalid persisted settings.</exception>
    internal JObject Load(bool required = false)
    {
        if (!files.FileExists(FilePath) && !required)
            return new JObject();

        try
        {
            var document = TomlValue.ParseDocument(files.ReadAllText(FilePath));
            JObject result = new();

            foreach (var property in document.Properties())
                result[property.Name] = Normalize(property.Name, property.Value, Path.GetDirectoryName(FilePath)!);

            return result;
        }
        catch (ArgumentException exception)
        {
            throw new IOException(ApplicationText.Format(CliStrings.Config_InvalidFile, FilePath, exception.Message), exception);
        }
    }

    /// <summary>Validates a config-set value and resolves relative paths against the current directory.</summary>
    /// <param name="name">Persistable option key.</param>
    /// <param name="text">Shell-supplied value, using TOML syntax for booleans and integers.</param>
    /// <returns>The normalized value ready for persistence.</returns>
    /// <exception cref="ArgumentException">The key, type, enum choice, or TOML value is invalid.</exception>
    internal static JToken ParseValue(string name, string text)
    {
        Type type = GetOptionType(name);
        JToken value = type == typeof(string) || type.IsEnum ? new JValue(text) : TomlValue.Parse(text);

        return Normalize(name, value, Environment.CurrentDirectory);
    }

    private static JToken Normalize(string name, JToken value, string baseDirectory)
    {
        Type type = GetOptionType(name);

        if (type == typeof(bool) && value.Type == JTokenType.Boolean)
            return value.DeepClone();

        if (type == typeof(int) && value.Type == JTokenType.Integer
            && long.TryParse(value.ToString(), CultureInfo.InvariantCulture, out long integer)
            && integer is >= int.MinValue and <= int.MaxValue)
            return new JValue((int)integer);

        if ((type == typeof(string) || type.IsEnum) && value.Type == JTokenType.String)
        {
            string text = value.Value<string>()!;

            if (type.IsEnum)
            {
                string? choice = Enum.GetNames(type).FirstOrDefault(candidate =>
                    OptionSchema.Kebab(candidate) == text || candidate.Equals(text, StringComparison.OrdinalIgnoreCase));

                if (choice is null)
                    throw new ArgumentException(ApplicationText.Format(
                        CliStrings.InvalidEnum, name, string.Join(", ", Enum.GetNames(type).Select(OptionSchema.Kebab))));

                return new JValue(OptionSchema.Kebab(choice));
            }

            if (name == CliOptions.OsuPath.Name
                || name == CliOptions.SongsPath.Name
                || name == CliOptions.OsuConfig.Name
                || name == CliOptions.BackupPath.Name)
                return new JValue(string.IsNullOrWhiteSpace(text) ? "" : Path.GetFullPath(text, baseDirectory));

            return new JValue(text);
        }

        throw new ArgumentException(ApplicationText.Format(CliStrings.Config_InvalidType, name, type.Name));
    }

    private static Type GetOptionType(string name) => Options.TryGetValue(name, out var type)
        ? type
        : throw new ArgumentException(ApplicationText.Format(CliStrings.Config_UnknownSetting, name));

    /// <summary>Overwrites the selected configuration file with normalized values in stable key order.</summary>
    /// <param name="document">Caller-validated allowlisted values; omitted keys remain unset.</param>
    /// <param name="cancellationToken">Cancels before the destination is opened.</param>
    /// <returns>A task completing after the TOML document is written.</returns>
    internal Task SaveAsync(JObject document, CancellationToken cancellationToken)
    {
        string text = string.Join(Environment.NewLine, document.Properties()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => property.Name + TomlValue.ASSIGNMENT_SEPARATOR + TomlValue.Format(property.Value)));

        files.EnsureDirectoryExists(Path.GetDirectoryName(FilePath)!);
        cancellationToken.ThrowIfCancellationRequested();
        files.WriteAllText(FilePath, text + Environment.NewLine);

        return Task.CompletedTask;
    }

    /// <summary>Builds displayable platform defaults without loading saved CLI or Desktop settings.</summary>
    /// <returns>Built-in adapters and backup preferences, with paths left unresolved until invocation setup.</returns>
    internal static JObject Defaults()
    {
        ApplicationSettings settings = new()
        {
            MakePeriodicBackups = false,
            BeatmapLiveStateReading = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()
                ? BeatmapLiveStateReadingMode.EditorReader
                : BeatmapLiveStateReadingMode.Disabled,
            EditorReload = OperatingSystem.IsWindows()
                ? EditorReloadMode.SimulatedKeypress
                : EditorReloadMode.Mtipc,
        };

        return new JObject
        {
            [CliOptions.OsuPath.Name] = settings.OsuPath,
            [CliOptions.SongsPath.Name] = settings.SongsPath,
            [CliOptions.OsuConfig.Name] = settings.OsuConfigPath,
            [CliOptions.BackupPath.Name] = settings.BackupsPath,
            [CliOptions.MakeBackups.Name] = settings.MakeBackups,
            [CliOptions.MaxBackups.Name] = settings.MaxBackupFiles,
            [CliOptions.EditorReader.Name] = OptionSchema.Kebab(settings.BeatmapLiveStateReading.ToString()),
            [CliOptions.CurrentBeatmapFetching.Name] = OptionSchema.Kebab(settings.CurrentBeatmapFetching.ToString()),
            [CliOptions.EditorReloadMode.Name] = OptionSchema.Kebab(settings.EditorReload.ToString()),
            [CliOptions.AutoDetectLazerExternalEdit.Name] = settings.AutoDetectLazerExternalEdit,
        };
    }
}
