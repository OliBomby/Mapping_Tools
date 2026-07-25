namespace Mapping_Tools.ApplicationServices.Settings;

/// <summary>
/// Supplies machine-specific information needed to derive settings paths.
/// </summary>
public interface ISettingsPathEnvironment
{
    /// <summary>
    /// Gets the current operating-system username used in osu! config filenames.
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Attempts to locate the installed osu! directory.
    /// </summary>
    /// <returns>The installation directory, or <see langword="null"/> when it cannot be found.</returns>
    string? FindOsuInstallation();

    /// <summary>
    /// Reads the configured beatmap-directory value from an osu! user config.
    /// </summary>
    /// <param name="configPath">The osu! user configuration file.</param>
    /// <returns>The configured directory value, or <c>Songs</c> when unavailable.</returns>
    string GetBeatmapDirectory(string configPath);

    /// <summary>
    /// Creates a directory when it does not already exist.
    /// </summary>
    /// <param name="path">The directory that must exist.</param>
    void EnsureDirectoryExists(string path);
}
