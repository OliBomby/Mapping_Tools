using Mapping_Tools.Application.Localization;
using Mapping_Tools.Desktop.Localization;

namespace Mapping_Tools.Desktop.Shell;

/// <summary>
///     Stores validated feature registrations supplied by the composition root.
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
                    ApplicationText.Format(DesktopStrings.ShellFeatureRegistry_DuplicateFeatureId, feature.Id),
                    nameof(features));

        if (Features.Count == 0) throw new ArgumentException(DesktopStrings.ShellFeatureRegistry_AtLeastOneFeature, nameof(features));
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
