using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Infrastructure.Files;

public sealed class ApplicationDirectories : IApplicationDirectories
{
    public ApplicationDirectories()
        : this(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    public ApplicationDirectories(string localApplicationData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);

        ApplicationData = Path.Combine(
            Path.GetFullPath(localApplicationData),
            "Mapping Tools");
        Exports = Path.Combine(ApplicationData, "Exports");
    }

    public string ApplicationData { get; }

    public string Exports { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(ApplicationData);
        Directory.CreateDirectory(Exports);
    }
}
