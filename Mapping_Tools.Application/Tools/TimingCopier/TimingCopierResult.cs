namespace Mapping_Tools.Application.Tools.TimingCopier;

/// <summary>Reports the target paths written by one Timing Copier run.</summary>
/// <param name="ProcessedPaths">The output paths in selection order.</param>
public sealed record TimingCopierResult(IReadOnlyList<string> ProcessedPaths)
{
    /// <summary>Gets the number of target beatmaps written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

