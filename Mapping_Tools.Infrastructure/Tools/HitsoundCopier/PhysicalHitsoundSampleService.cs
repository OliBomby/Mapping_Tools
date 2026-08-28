using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Infrastructure.Tools.HitsoundCopier;

/// <summary>
///     Supplies physical sample paths for Hitsound Copier and exports generated sample
///     schemas through the shared audio services.
/// </summary>
public sealed class PhysicalHitsoundSampleService : IHitsoundSampleService
{
    private readonly IApplicationDirectories directories;
    private readonly IAudioExporter exporter;
    private readonly IAudioGenerator generator;
    private readonly IAudioClipMixer mixer;
    private readonly IMapCleanerSampleService sampleAnalyzer;

    /// <summary>Creates the physical sample adapter and its audio export pipeline.</summary>
    /// <param name="sampleAnalyzer">The existing mapset audio file analyzer.</param>
    /// <param name="directories">The application directories containing the default export folder.</param>
    /// <param name="generator">Generates owned clips from source sample arguments.</param>
    /// <param name="exporter">Writes generated clips to audio files.</param>
    /// <param name="mixer">Combines multiple generated source clips.</param>
    public PhysicalHitsoundSampleService(
        IMapCleanerSampleService sampleAnalyzer,
        IApplicationDirectories directories,
        IAudioGenerator generator,
        IAudioExporter exporter,
        IAudioClipMixer mixer)
    {
        this.sampleAnalyzer = sampleAnalyzer ?? throw new ArgumentNullException(nameof(sampleAnalyzer));
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        this.mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
        string directory,
        CancellationToken cancellationToken = default)
    {
        return sampleAnalyzer.AnalyzeAsync(directory, true, cancellationToken);
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
    public async Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(directories.Exports);

        foreach (string path in Directory.EnumerateFiles(directories.Exports))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Delete(path);
        }

        foreach ((string name, List<SampleGeneratingArgs> source) in schema)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExportSourceGroupAsync(name, source, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExportSourceGroupAsync(
        string name,
        IReadOnlyList<SampleGeneratingArgs> source,
        CancellationToken cancellationToken)
    {
        List<SampleGeneratingArgs> valid = source
            .Where(sample => File.Exists(sample.Path))
            .Distinct(new SampleGeneratingArgsComparer())
            .ToList();
        if (valid.Count == 0) return;

        if (valid.Count == 1 && valid[0].CanCopyPaste)
        {
            string destination = Path.Combine(directories.Exports, name + valid[0].GetExtension());
            File.Copy(valid[0].Path, destination, true);
            return;
        }

        List<AudioClip> clips = [];
        foreach (SampleGeneratingArgs sample in valid)
        {
            cancellationToken.ThrowIfCancellationRequested();
            clips.Add(await generator.GenerateAsync(
                    new AudioGenerationRequest(sample),
                    cancellationToken)
                .ConfigureAwait(false));
        }

        AudioClip clip = clips.Count == 1
            ? clips[0]
            : await mixer.MixAsync(clips, cancellationToken).ConfigureAwait(false);
        await exporter.ExportAsync(
                clip,
                new AudioExportRequest(
                    Path.Combine(directories.Exports, name + ".wav"),
                    AudioExportFormat.WaveIeeeFloat),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
