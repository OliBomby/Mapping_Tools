namespace Mapping_Tools.ApplicationServices.Settings;

public interface ISettingsService
{
    SettingsLoadResult LoadOrCreate();

    void Save(ApplicationSettings settings);
}

public sealed record SettingsLoadResult(
    ApplicationSettings Settings,
    bool WasCreated,
    bool UsedFallbackOsuPath);
