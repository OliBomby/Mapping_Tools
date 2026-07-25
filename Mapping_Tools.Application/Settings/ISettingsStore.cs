namespace Mapping_Tools.ApplicationServices.Settings;

public interface ISettingsStore
{
    bool Exists { get; }

    ApplicationSettings Load();

    void Save(ApplicationSettings settings);
}
