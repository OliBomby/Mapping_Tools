namespace Mapping_Tools.Application.Settings.Models;

/// <summary>
///     Reports noteworthy fallback behavior from settings path resolution.
/// </summary>
/// <param name="UsedFallbackOsuPath">
///     Whether osu! discovery failed and the conventional local-app-data path was used.
/// </param>
public sealed record SettingsPathResult(bool UsedFallbackOsuPath);
