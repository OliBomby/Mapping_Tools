namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Describes the initialized settings returned at application startup.
/// </summary>
/// <param name="Settings">The loaded settings with path defaults applied.</param>
/// <param name="WasCreated">Whether no configuration existed and defaults were persisted.</param>
/// <param name="UsedFallbackOsuPath">Whether automatic osu! discovery failed.</param>
public sealed record SettingsLoadResult(
    ApplicationSettings Settings,
    bool WasCreated,
    bool UsedFallbackOsuPath);
