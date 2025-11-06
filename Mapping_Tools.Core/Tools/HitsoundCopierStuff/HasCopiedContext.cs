using Mapping_Tools.Core.BeatmapHelper.Contexts;

namespace Mapping_Tools.Core.Tools.HitsoundCopierStuff;

/// <summary>
/// Indicates that an object has participated in hitsound copying.
/// </summary>
public class HasCopiedContext : IContext {
    /// <inheritdoc />
    public IContext Copy() {
        return (IContext) MemberwiseClone();
    }
}