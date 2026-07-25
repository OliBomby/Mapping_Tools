namespace Mapping_Tools.ApplicationServices.Platform;

public interface IApplicationDirectories
{
    string ApplicationData { get; }

    string Exports { get; }

    void EnsureCreated();
}
