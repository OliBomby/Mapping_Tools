namespace Mapping_Tools.ApplicationServices.Settings;

public interface ISettingsPathEnvironment
{
    string UserName { get; }

    string? FindOsuInstallation();

    string GetBeatmapDirectory(string configPath);

    void EnsureDirectoryExists(string path);
}
