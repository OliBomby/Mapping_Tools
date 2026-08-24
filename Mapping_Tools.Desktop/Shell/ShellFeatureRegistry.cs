namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Stores explicitly supplied feature registrations without reflecting over UI types.
/// </summary>
public sealed class ShellFeatureRegistry : IShellFeatureRegistry
{
    private readonly Dictionary<string, ShellFeatureRegistration> byId;

    /// <summary>
    ///     Builds a registry and rejects ambiguous identifiers.
    /// </summary>
    /// <param name="features">The ordered registrations.</param>
    public ShellFeatureRegistry(IEnumerable<ShellFeatureRegistration> features)
    {
        ArgumentNullException.ThrowIfNull(features);
        Features = features.ToArray();
        byId = new Dictionary<string, ShellFeatureRegistration>(StringComparer.OrdinalIgnoreCase);
        foreach (var feature in Features)
            if (!byId.TryAdd(feature.Id, feature))
                throw new ArgumentException(
                    $"Feature id '{feature.Id}' is registered more than once.",
                    nameof(features));

        if (Features.Count == 0) throw new ArgumentException("At least one shell feature must be registered.", nameof(features));
    }

    /// <inheritdoc />
    public IReadOnlyList<ShellFeatureRegistration> Features { get; }

    /// <inheritdoc />
    public ShellFeatureRegistration? Find(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return byId.GetValueOrDefault(id);
    }
}
