using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Represents the complete Hitsound Copier state persisted by the shell.</summary>
public sealed class HitsoundCopierProject : HitsoundCopierOptions
{
}

/// <summary>Reports the target maps and deterministic hitsound changes from a run.</summary>
/// <param name="ProcessedPaths">Target map paths successfully saved.</param>
/// <param name="MatchedHitsoundCount">The number of source events matched to target events.</param>
/// <param name="GeneratedSampleCount">The number of new sample entries created.</param>
/// <param name="MutedEdgeCount">The number of target edge events muted by the filter.</param>
/// <param name="SampleSchema">The generated sample requirements.</param>
public sealed record HitsoundCopierResult(
    IReadOnlyList<string> ProcessedPaths,
    int MatchedHitsoundCount,
    int GeneratedSampleCount,
    int MutedEdgeCount,
    SampleSchema SampleSchema)
{
    /// <summary>Gets the number of target maps written.</summary>
    public int ProcessedCount => ProcessedPaths.Count;
}

/// <summary>Provides mapset sample discovery and custom-index assignment behind a file/audio port.</summary>
public interface IHitsoundSampleService
{
    /// <summary>Finds canonical audio files by extensionless beatmap sample name.</summary>
    /// <param name="directory">The mapset directory containing the source or target samples.</param>
    /// <param name="cancellationToken">Cancels file inspection.</param>
    /// <returns>A case-insensitive extensionless-path mapping.</returns>
    Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a custom sample assignment from source files already validated by the adapter.
    /// </summary>
    /// <param name="directory">The target mapset directory.</param>
    /// <param name="sourceFilenames">Beatmap-relative filenames played by the source event.</param>
    /// <param name="firstSamples">Canonical files discovered in the mapset.</param>
    /// <param name="role">The generated role, such as <c>slidertick</c>.</param>
    /// <param name="sampleSet">The source sample family.</param>
    /// <param name="startIndex">The first custom index to consider.</param>
    /// <param name="existingSchema">Previously assigned samples whose indices must not be reused.</param>
    /// <returns>An assignment, or <see langword="null" /> when no source file is available.</returns>
    HitsoundSampleAssignment? TryCreateAssignment(
        string directory,
        IReadOnlyList<string> sourceFilenames,
        IReadOnlyDictionary<string, string> firstSamples,
        string role,
        SampleSet sampleSet,
        int startIndex,
        SampleSchema existingSchema);

    /// <summary>Publishes generated sample requirements through the platform audio/file adapter.</summary>
    /// <param name="schema">The generated sample requirements.</param>
    /// <param name="cancellationToken">Cancels export preparation.</param>
    Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default);
}

/// <summary>Copies hitsounds through the shared editing gateway.</summary>
public interface IHitsoundCopierService
{
    /// <summary>Copies source hitsounds to each vertical-bar-separated target path.</summary>
    /// <param name="options">The complete source, selection, matching, and filter state.</param>
    /// <param name="progress">Reports aggregate target completion.</param>
    /// <param name="cancellationToken">Cancels loading, transformation, backup, or saving.</param>
    /// <returns>The target paths and change summary.</returns>
    Task<HitsoundCopierResult> CopyAsync(
        HitsoundCopierOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
