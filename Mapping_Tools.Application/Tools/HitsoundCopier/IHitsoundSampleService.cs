using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

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

    /// <summary>Exports generated sample requirements to the application's default Exports directory.</summary>
    /// <param name="schema">The output names and source transformations to render.</param>
    /// <param name="cancellationToken">Cancels export preparation or audio rendering.</param>
    Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default);
}
