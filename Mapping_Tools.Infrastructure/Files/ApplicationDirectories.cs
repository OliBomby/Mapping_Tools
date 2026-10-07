using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Implements the Mapping Tools layout beneath the platform's application-data
///     directory and exposes the previous local-data location for migration.
/// </summary>
public sealed class ApplicationDirectories : IApplicationDirectories
{
    /// <summary>
    ///     Uses the current user's platform application-data directory for current
    ///     data and local application data as the legacy migration source.
    /// </summary>
    public ApplicationDirectories()
        : this(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData,
                Environment.SpecialFolderOption.Create),
            legacyApplicationDataRoot: Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify))
    {
    }

    /// <summary>
    ///     Builds application paths beneath caller-supplied roots, which also
    ///     supports isolated renderer and test hosts.
    /// </summary>
    /// <param name="applicationDataRoot">The platform application-data root for current data.</param>
    /// <param name="applicationFolderName">A single relative directory name for the application.</param>
    /// <param name="legacyApplicationDataRoot">
    ///     The platform local-data root used to find data from the previous client.
    ///     When omitted, the current and legacy directories share the supplied root.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     The application folder name is rooted or contains a directory separator.
    /// </exception>
    public ApplicationDirectories(
        string applicationDataRoot,
        string applicationFolderName = "Mapping Tools",
        string? legacyApplicationDataRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationFolderName);
        if (Path.IsPathRooted(applicationFolderName)
            || applicationFolderName.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
            >= 0)
            throw new ArgumentException(
                "The application folder name must be a single relative path segment.",
                nameof(applicationFolderName));

        string legacyRoot = legacyApplicationDataRoot ?? applicationDataRoot;
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyRoot);

        LocalApplicationData = Path.GetFullPath(legacyRoot);
        LegacyApplicationData = Path.Combine(LocalApplicationData, applicationFolderName);
        ApplicationData = Path.Combine(Path.GetFullPath(applicationDataRoot), applicationFolderName);
        Exports = Path.Combine(ApplicationData, "Exports");
        ConfigurationFile = Path.Combine(ApplicationData, "config.json");
    }

    /// <inheritdoc />
    public string LocalApplicationData { get; }

    /// <inheritdoc />
    public string LegacyApplicationData { get; }

    /// <inheritdoc />
    public string ApplicationData { get; }

    /// <inheritdoc />
    public string Exports { get; }

    /// <inheritdoc />
    public string ConfigurationFile { get; }

    /// <inheritdoc />
    public void EnsureCreated()
    {
        Directory.CreateDirectory(ApplicationData);
        Directory.CreateDirectory(Exports);
    }
}
