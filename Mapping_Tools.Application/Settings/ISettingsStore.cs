namespace Mapping_Tools.ApplicationServices.Settings;

/// <summary>
/// Persists the portable settings document without applying machine defaults.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Gets whether a settings document currently exists.
    /// </summary>
    bool Exists { get; }

    /// <summary>
    /// Deserializes the existing settings document.
    /// </summary>
    /// <returns>The persisted settings exactly as stored.</returns>
    ApplicationSettings Load();

    /// <summary>
    /// Replaces the persisted settings document.
    /// </summary>
    /// <param name="settings">The settings snapshot to persist.</param>
    void Save(ApplicationSettings settings);
}
