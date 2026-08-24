using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Application.Settings.Contracts;

/// <summary>
///     Coordinates settings persistence with machine-specific path initialization.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    ///     Loads existing settings or persists a clean default document on first run,
    ///     then applies in-memory path defaults.
    /// </summary>
    /// <returns>The initialized settings and details of creation and path fallback.</returns>
    SettingsLoadResult LoadOrCreate();

    /// <summary>
    ///     Persists the supplied settings document.
    /// </summary>
    /// <param name="settings">The complete settings snapshot to store.</param>
    void Save(ApplicationSettings settings);
}

