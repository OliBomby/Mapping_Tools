using Mapping_Tools.Core.Tools.MetadataManager;

namespace Mapping_Tools.Application.Tools.MetadataManager;

/// <summary>Reports the final paths written by one Metadata Manager run.</summary>
/// <param name="ProcessedPaths">The output paths in target selection order.</param>
public sealed record MetadataManagerResult(IReadOnlyList<string> ProcessedPaths)
{
    /// <summary>Gets the number of beatmaps successfully written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

