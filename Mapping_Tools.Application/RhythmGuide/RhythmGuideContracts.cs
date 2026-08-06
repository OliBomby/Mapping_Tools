using Mapping_Tools.Core.Tools.RhythmGuide;

namespace Mapping_Tools.Application.RhythmGuide;

/// <summary>Represents the complete legacy-compatible Rhythm Guide project document.</summary>
public sealed class RhythmGuideProject
{
    public RhythmGuideOptions GuideGeneratorArgs { get; set; } = new();
}

/// <summary>Reports the destination and number of guide objects produced by one run.</summary>
public sealed record RhythmGuideResult(
    string ExportPath,
    int AddedObjectCount,
    RhythmGuideExportMode ExportMode);

/// <summary>Loads source state, applies the pure generator, and persists through safety boundaries.</summary>
public interface IRhythmGuideService
{
    Task<RhythmGuideResult> GenerateAsync(
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default);
}
