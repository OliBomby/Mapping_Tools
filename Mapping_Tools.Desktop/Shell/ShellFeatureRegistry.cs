namespace Mapping_Tools.Desktop.Shell;

/// <summary>
/// Provides the deterministic feature list used by desktop navigation.
/// </summary>
public interface IShellFeatureRegistry
{
    /// <summary>Gets registrations in their declared navigation order.</summary>
    IReadOnlyList<ShellFeatureRegistration> Features { get; }

    /// <summary>Finds a feature by its stable identifier.</summary>
    /// <param name="id">The identifier to find.</param>
    /// <returns>The matching registration, or <see langword="null"/>.</returns>
    ShellFeatureRegistration? Find(string id);
}

/// <summary>
/// Stores explicitly supplied feature registrations without reflecting over UI types.
/// </summary>
public sealed class ShellFeatureRegistry : IShellFeatureRegistry
{
    private readonly Dictionary<string, ShellFeatureRegistration> _byId;

    /// <summary>
    /// Builds a registry and rejects ambiguous identifiers.
    /// </summary>
    /// <param name="features">The ordered registrations.</param>
    public ShellFeatureRegistry(IEnumerable<ShellFeatureRegistration> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        Features = features.ToArray();
        _byId = new Dictionary<string, ShellFeatureRegistration>(StringComparer.OrdinalIgnoreCase);
        foreach (ShellFeatureRegistration feature in Features)
        {
            if (!_byId.TryAdd(feature.Id, feature))
            {
                throw new ArgumentException(
                    $"Feature id '{feature.Id}' is registered more than once.",
                    nameof(features));
            }
        }

        if (Features.Count == 0)
        {
            throw new ArgumentException("At least one shell feature must be registered.", nameof(features));
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ShellFeatureRegistration> Features { get; }

    /// <inheritdoc/>
    public ShellFeatureRegistration? Find(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _byId.GetValueOrDefault(id);
    }
}
