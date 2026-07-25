namespace Mapping_Tools.ApplicationServices.Platform;

public interface IApplicationDirectories
{
    string LocalApplicationData { get; }

    string ApplicationData { get; }

    string Exports { get; }

    string ConfigurationFile { get; }

    void EnsureCreated();
}
