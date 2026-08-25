using Mapping_Tools.Core.Tools.PropertyTransformer;

namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>
///     Represents the complete Property Transformer project persisted by the shell.
/// </summary>
public sealed class PropertyTransformerProject : PropertyTransformerOptions
{
    /// <summary>Gets or sets whether all time-related fields stay synchronized in the editor.</summary>
    public bool SyncTimeFields { get; set; }

}
