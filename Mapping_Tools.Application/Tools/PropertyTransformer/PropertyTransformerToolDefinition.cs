namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>Provides the discoverable metadata for Property Transformer.</summary>
public static class PropertyTransformerToolDefinition
{
    /// <summary>Gets the stable Property Transformer metadata.</summary>
    public static ToolDefinition Definition { get; } = new(
        "property-transformer",
        "Property Transformer",
        "Multiply and add to timing, object, bookmark, and storyboard properties.",
        ["properties", "transform", "timing", "offset", "multiplier"]);
}
