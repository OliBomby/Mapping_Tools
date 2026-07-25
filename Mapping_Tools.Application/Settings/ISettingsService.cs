namespace Mapping_Tools.ApplicationServices.Settings;

/// <summary>
/// Coordinates settings persistence with machine-specific path initialization.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads existing settings or persists a clean default document on first run,
    /// then applies in-memory path defaults.
    /// </summary>
    /// <returns>The initialized settings and details of creation and path fallback.</returns>
    SettingsLoadResult LoadOrCreate();

    /// <summary>
    /// Persists the supplied settings document.
    /// </summary>
    /// <param name="settings">The complete settings snapshot to store.</param>
    void Save(ApplicationSettings settings);
}

/// <summary>
/// Describes the initialized settings returned at application startup.
/// </summary>
/// <param name="Settings">The loaded settings with path defaults applied.</param>
/// <param name="WasCreated">Whether no configuration existed and defaults were persisted.</param>
/// <param name="UsedFallbackOsuPath">Whether automatic osu! discovery failed.</param>
public sealed record SettingsLoadResult(
    ApplicationSettings Settings,
    bool WasCreated,
    bool UsedFallbackOsuPath);
