using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Implements startup settings orchestration without coupling callers to JSON,
///     the registry, or the filesystem.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsPathService paths;
    private readonly ISettingsStore store;

    /// <summary>
    ///     Creates a settings coordinator.
    /// </summary>
    /// <param name="store">Persistence for the portable settings document.</param>
    /// <param name="paths">The service that completes machine-dependent paths.</param>
    public SettingsService(ISettingsStore store, ISettingsPathService paths)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    /// <summary>
    ///     Loads persisted settings, or saves clean defaults before applying
    ///     machine-specific paths on first run.
    /// </summary>
    /// <returns>The initialized settings plus first-run and fallback status.</returns>
    public SettingsLoadResult LoadOrCreate()
    {
        bool wasCreated = !store.Exists;
        var settings = wasCreated
            ? new ApplicationSettings()
            : store.Load();

        if (wasCreated) store.Save(settings);

        var pathResult = paths.ApplyDefaults(settings);
        return new SettingsLoadResult(
            settings,
            wasCreated,
            pathResult.UsedFallbackOsuPath);
    }

    /// <summary>
    ///     Persists the supplied settings without altering its path values.
    /// </summary>
    /// <param name="settings">The complete settings snapshot to persist.</param>
    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        store.Save(settings);
    }
}
