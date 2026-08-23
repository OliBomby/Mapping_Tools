namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Completes missing filesystem settings from the current machine environment.
/// </summary>
public interface ISettingsPathService
{
    /// <summary>
    ///     Mutates only blank path properties, then ensures the backup directory exists.
    /// </summary>
    /// <param name="settings">The settings document to complete.</param>
    /// <returns>Information about fallbacks used while resolving paths.</returns>
    SettingsPathResult ApplyDefaults(ApplicationSettings settings);
}

/// <summary>
///     Reports noteworthy fallback behavior from settings path resolution.
/// </summary>
/// <param name="UsedFallbackOsuPath">
///     Whether osu! discovery failed and the conventional local-app-data path was used.
/// </param>
public sealed record SettingsPathResult(bool UsedFallbackOsuPath);
