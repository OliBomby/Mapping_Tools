namespace Mapping_Tools.ApplicationServices.Settings;

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsStore _store;
    private readonly ISettingsPathService _paths;

    public SettingsService(ISettingsStore store, ISettingsPathService paths)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public SettingsLoadResult LoadOrCreate()
    {
        bool wasCreated = !_store.Exists;
        ApplicationSettings settings = wasCreated
            ? new ApplicationSettings()
            : _store.Load();

        if (wasCreated)
        {
            _store.Save(settings);
        }

        SettingsPathResult pathResult = _paths.ApplyDefaults(settings);
        return new SettingsLoadResult(
            settings,
            wasCreated,
            pathResult.UsedFallbackOsuPath);
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _store.Save(settings);
    }
}
