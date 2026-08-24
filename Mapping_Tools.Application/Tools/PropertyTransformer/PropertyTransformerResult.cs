using Mapping_Tools.Core.Tools.PropertyTransformer;

namespace Mapping_Tools.Application.Tools.PropertyTransformer;

/// <summary>Reports the paths transformed by one Property Transformer run.</summary>
/// <param name="ProcessedPaths">The output paths in selection order.</param>
public sealed record PropertyTransformerResult(IReadOnlyList<string> ProcessedPaths)
{
    /// <summary>Gets the number of documents transformed.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

