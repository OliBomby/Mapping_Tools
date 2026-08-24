namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Provides the deterministic feature list used by desktop navigation.
/// </summary>
public interface IShellFeatureRegistry
{
    /// <summary>Gets registrations in their declared navigation order.</summary>
    IReadOnlyList<ShellFeatureRegistration> Features { get; }

    /// <summary>Finds a feature by its stable identifier.</summary>
    /// <param name="id">The identifier to find.</param>
    /// <returns>The matching registration, or <see langword="null" />.</returns>
    ShellFeatureRegistration? Find(string id);
}

