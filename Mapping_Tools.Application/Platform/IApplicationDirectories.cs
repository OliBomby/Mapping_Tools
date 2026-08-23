namespace Mapping_Tools.Application.Platform;

/// <summary>
///     Defines the stable filesystem locations owned by Mapping Tools.
/// </summary>
public interface IApplicationDirectories
{
    /// <summary>
    ///     Gets the operating-system local application-data root used to derive app paths.
    /// </summary>
    string LocalApplicationData { get; }

    /// <summary>
    ///     Gets the Mapping Tools data directory.
    /// </summary>
    string ApplicationData { get; }

    /// <summary>
    ///     Gets the default directory for generated maps and assets.
    /// </summary>
    string Exports { get; }

    /// <summary>
    ///     Gets the full path of the user settings JSON file.
    /// </summary>
    string ConfigurationFile { get; }

    /// <summary>
    ///     Creates the application-owned directories required for normal operation.
    /// </summary>
    void EnsureCreated();
}
