using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Infrastructure.Files;

public sealed class ApplicationDirectories : IApplicationDirectories
{
    public ApplicationDirectories()
        : this(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Mapping Tools")
    {
    }

    public ApplicationDirectories(
        string localApplicationData,
        string applicationFolderName = "Mapping Tools")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationFolderName);
        if (Path.IsPathRooted(applicationFolderName) ||
            applicationFolderName.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
        {
            throw new ArgumentException(
                "The application folder name must be a single relative path segment.",
                nameof(applicationFolderName));
        }

        LocalApplicationData = Path.GetFullPath(localApplicationData);
        ApplicationData = Path.Combine(LocalApplicationData, applicationFolderName);
        Exports = Path.Combine(ApplicationData, "Exports");
        ConfigurationFile = Path.Combine(ApplicationData, "config.json");
    }

    public string LocalApplicationData { get; }

    public string ApplicationData { get; }

    public string Exports { get; }

    public string ConfigurationFile { get; }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(ApplicationData);
        Directory.CreateDirectory(Exports);
    }
}
