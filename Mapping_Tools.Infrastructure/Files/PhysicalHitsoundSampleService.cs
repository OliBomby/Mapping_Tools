using Mapping_Tools.Application.HitsoundCopier;
using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier;

namespace Mapping_Tools.Infrastructure.Files;

/// <summary>
///     Supplies physical sample paths for Hitsound Copier while leaving audio decoding and
///     generation behind the Application port for the later audio wave.
/// </summary>
public sealed class PhysicalHitsoundSampleService : IHitsoundSampleService
{
    private readonly IMapCleanerSampleService _sampleAnalyzer;

    /// <summary>Creates the physical sample adapter.</summary>
    /// <param name="sampleAnalyzer">The existing mapset audio file analyzer.</param>
    public PhysicalHitsoundSampleService(IMapCleanerSampleService sampleAnalyzer)
    {
        _sampleAnalyzer = sampleAnalyzer ?? throw new ArgumentNullException(nameof(sampleAnalyzer));
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        return _sampleAnalyzer.AnalyzeAsync(directory, true, cancellationToken);
    }

    /// <inheritdoc />
    public HitsoundSampleAssignment? TryCreateAssignment(
        string directory,
        IReadOnlyList<string> sourceFilenames,
        IReadOnlyDictionary<string, string> firstSamples,
        string role,
        SampleSet sampleSet,
        int startIndex,
        SampleSchema existingSchema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(sourceFilenames);
        ArgumentNullException.ThrowIfNull(firstSamples);
        ArgumentNullException.ThrowIfNull(existingSchema);
        List<SampleGeneratingArgs> source = [];
        foreach (string filename in sourceFilenames)
        {
            string path = Path.IsPathRooted(filename)
                ? filename
                : Path.Combine(directory, filename);
            string extensionless = Path.Combine(
                Path.GetDirectoryName(path) ?? directory,
                Path.GetFileNameWithoutExtension(path));
            if (firstSamples.TryGetValue(extensionless, out string? canonical))
                source.Add(new SampleGeneratingArgs(canonical));
            else if (File.Exists(path)) source.Add(new SampleGeneratingArgs(path));
        }

        if (source.Count == 0) return null;

        var existingKeys = existingSchema.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool added = existingSchema.AddHitsound(
            source,
            role,
            sampleSet,
            out int index,
            out var assignedSet,
            startIndex);
        SampleSchema addedSchema = new();
        if (added)
            foreach (string key in existingSchema.Keys.Where(key => !existingKeys.Contains(key)))
                addedSchema.Add(key, existingSchema[key]);

        return new HitsoundSampleAssignment(index, assignedSet, addedSchema);
    }

    /// <inheritdoc />
    public Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        cancellationToken.ThrowIfCancellationRequested();
        // Core now preserves the exact source mix and custom index. Actual waveform
        // rendering/export remains an explicit audio adapter owned by Wave 9.
        return Task.CompletedTask;
    }
}
