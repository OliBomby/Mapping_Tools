namespace Mapping_Tools.Application.Platform;

/// <summary>
///     Defines the stable filesystem locations owned by Mapping Tools.
/// </summary>
public interface IApplicationDirectories
{
    /// <summary>
    ///     Gets the operating-system local application-data root used by platform integrations.
    /// </summary>
    string LocalApplicationData { get; }

    /// <summary>
    ///     Gets the legacy Mapping Tools data directory used as a migration source.
    /// </summary>
    string LegacyApplicationData { get; }

    /// <summary>
    ///     Gets the current Mapping Tools data directory beneath the platform's application-data root.
    /// </summary>
    string ApplicationData { get; }

    /// <summary>
    ///     Gets the default directory for generated maps and assets.
    /// </summary>
    string Exports { get; }

    /// <summary>
    ///     Gets the full path of the current Mapping Tools configuration JSON file.
    /// </summary>
    string ConfigurationFile { get; }

    /// <summary>
    ///     Creates the application-owned directories required for normal operation.
    /// </summary>
    void EnsureCreated();
}
