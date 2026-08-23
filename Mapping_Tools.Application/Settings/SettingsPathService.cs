using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Derives missing osu!, Songs, config, and backup paths while preserving every
///     non-blank path explicitly chosen by the user.
/// </summary>
public sealed class SettingsPathService : ISettingsPathService
{
    private readonly IApplicationDirectories directories;
    private readonly ISettingsPathEnvironment environment;

    /// <summary>
    ///     Creates the path initializer.
    /// </summary>
    /// <param name="directories">Mapping Tools' owned filesystem locations.</param>
    /// <param name="environment">Machine-specific osu! discovery and filesystem access.</param>
    public SettingsPathService(
        IApplicationDirectories directories,
        ISettingsPathEnvironment environment)
    {
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    ///     Applies path defaults in dependency order: osu!, user config, Songs, then backups.
    /// </summary>
    /// <param name="settings">The settings instance to mutate.</param>
    /// <returns>Whether the conventional fallback osu! path had to be used.</returns>
    public SettingsPathResult ApplyDefaults(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        bool usedFallbackOsuPath = false;
        if (string.IsNullOrWhiteSpace(settings.OsuPath))
        {
            string? locatedOsuPath = environment.FindOsuInstallation();
            usedFallbackOsuPath = string.IsNullOrWhiteSpace(locatedOsuPath);
            settings.OsuPath = usedFallbackOsuPath
                ? Path.Combine(directories.LocalApplicationData, "osu!")
                : locatedOsuPath!;
        }

        if (string.IsNullOrWhiteSpace(settings.OsuConfigPath))
            settings.OsuConfigPath = Path.Combine(
                settings.OsuPath,
                $"osu!.{environment.UserName}.cfg");

        if (string.IsNullOrWhiteSpace(settings.SongsPath))
        {
            string beatmapDirectory = environment.GetBeatmapDirectory(settings.OsuConfigPath);
            settings.SongsPath = Path.Combine(settings.OsuPath, beatmapDirectory);
        }

        if (string.IsNullOrWhiteSpace(settings.BackupsPath)) settings.BackupsPath = Path.Combine(directories.ApplicationData, "Backups");

        environment.EnsureDirectoryExists(settings.BackupsPath);
        return new SettingsPathResult(usedFallbackOsuPath);
    }
}
