namespace Mapping_Tools.ApplicationServices.Settings;

public interface ISettingsPathService
{
    SettingsPathResult ApplyDefaults(ApplicationSettings settings);
}

public sealed record SettingsPathResult(bool UsedFallbackOsuPath);
