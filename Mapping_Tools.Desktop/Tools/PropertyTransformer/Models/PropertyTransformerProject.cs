using Mapping_Tools.Application.Tools.PropertyTransformer;

namespace Mapping_Tools.Desktop.Tools.PropertyTransformer.Models;

/// <summary>Stores Property Transformer presentation preferences alongside transformation inputs.</summary>
public sealed class PropertyTransformerProject : PropertyTransformerServiceOptions
{
    /// <summary>Gets or sets whether related time fields are edited as a synchronized group.</summary>
    public bool SyncTimeFields { get; set; }
}
