using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Implements the legacy-compatible Mapping Tools layout beneath the
///     operating system's local application-data directory.
/// </summary>
public sealed class ApplicationDirectories : IApplicationDirectories
{
    /// <summary>
    ///     Uses the current user's local application-data directory and the
    ///     production <c>Mapping Tools</c> folder name.
    /// </summary>
    public ApplicationDirectories()
        : this(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    /// <summary>
    ///     Builds application paths beneath a caller-supplied root, which also
    ///     supports isolated renderer and test hosts.
    /// </summary>
    /// <param name="localApplicationData">The platform local-data root.</param>
    /// <param name="applicationFolderName">A single relative directory name for the application.</param>
    /// <exception cref="ArgumentException">
    ///     The application folder name is rooted or contains a directory separator.
    /// </exception>
    public ApplicationDirectories(
        string localApplicationData,
        string applicationFolderName = "Mapping Tools")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationData);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationFolderName);
        if (Path.IsPathRooted(applicationFolderName)
            || applicationFolderName.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
            >= 0)
            throw new ArgumentException(
                "The application folder name must be a single relative path segment.",
                nameof(applicationFolderName));

        LocalApplicationData = Path.GetFullPath(localApplicationData);
        ApplicationData = Path.Combine(LocalApplicationData, applicationFolderName);
        Exports = Path.Combine(ApplicationData, "Exports");
        ConfigurationFile = Path.Combine(ApplicationData, "config.json");
    }

    /// <summary>
    ///     <inheritdoc />
    public string LocalApplicationData { get; }

    /// <summary>
    ///     <inheritdoc />
    public string ApplicationData { get; }

    /// <summary>
    ///     <inheritdoc />
    public string Exports { get; }

    /// <summary>
    ///     <inheritdoc />
    public string ConfigurationFile { get; }

    /// <summary>
    ///     <inheritdoc />
    public void EnsureCreated()
    {
        Directory.CreateDirectory(ApplicationData);
        Directory.CreateDirectory(Exports);
    }
}
