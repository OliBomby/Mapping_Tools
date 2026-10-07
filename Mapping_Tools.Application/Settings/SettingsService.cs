using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Migration.Contracts;

namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Implements startup settings orchestration without coupling callers to JSON,
///     the registry, or the filesystem.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsPathService paths;
    private readonly Func<ApplicationSettings> settingsFactory;
    private readonly ISettingsStore store;
    private readonly IApplicationDataMigrationService? migrationService;

    /// <summary>
    ///     Creates a settings coordinator.
    /// </summary>
    /// <param name="store">Persistence for the portable settings document.</param>
    /// <param name="paths">The service that completes machine-dependent paths.</param>
    public SettingsService(ISettingsStore store, ISettingsPathService paths)
        : this(store, paths, static () => new ApplicationSettings(), null)
    {
    }

    /// <summary>
    ///     Creates a settings coordinator with a factory for the concrete settings
    ///     document used by the active frontend.
    /// </summary>
    /// <param name="store">Persistence for the portable settings document.</param>
    /// <param name="paths">The service that completes machine-dependent paths.</param>
    /// <param name="settingsFactory">
    ///     Creates a clean settings document for first-run initialization. The
    ///     factory may return a frontend-specific subclass.
    /// </param>
    public SettingsService(
        ISettingsStore store,
        ISettingsPathService paths,
        Func<ApplicationSettings> settingsFactory)
        : this(store, paths, settingsFactory, null)
    {
    }

    /// <summary>
    ///     Creates a settings coordinator that runs pending application-data migration
    ///     before inspecting or creating the current settings file.
    /// </summary>
    /// <param name="store">Persistence for the portable settings document.</param>
    /// <param name="paths">The service that completes machine-dependent paths.</param>
    /// <param name="settingsFactory">Creates clean settings for first-run initialization.</param>
    /// <param name="migrationService">Copies settings and workspace data from the previous location.</param>
    public SettingsService(
        ISettingsStore store,
        ISettingsPathService paths,
        Func<ApplicationSettings> settingsFactory,
        IApplicationDataMigrationService? migrationService)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.settingsFactory = settingsFactory ?? throw new ArgumentNullException(nameof(settingsFactory));
        this.migrationService = migrationService;
    }

    /// <summary>
    ///     Loads persisted settings, or saves clean defaults before applying
    ///     machine-specific paths on first run.
    /// </summary>
    /// <returns>The initialized settings plus first-run and fallback status.</returns>
    public SettingsLoadResult LoadOrCreate()
    {
        bool migratedLegacyData = migrationService?.RequiresMigration == true;
        if (migratedLegacyData)
            migrationService!.CopyLegacyDataAsync().GetAwaiter().GetResult();

        bool wasCreated = !store.Exists;
        var settings = wasCreated
            ? settingsFactory()
            : store.Load();

        if (wasCreated) store.Save(settings);

        var pathResult = paths.ApplyDefaults(settings);
        if (migratedLegacyData) store.Save(settings);

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
