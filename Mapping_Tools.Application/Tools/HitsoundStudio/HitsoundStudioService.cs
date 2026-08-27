using System.Globalization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.HitsoundStudio.Contracts;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Audio.Midi;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Core.Tools.HitsoundStudio.Models;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>
///     Coordinates Hitsound Studio imports, neutral audio services, schema rules,
///     package export, and cancellation without referencing a desktop framework.
/// </summary>
public sealed class HitsoundStudioService : IHitsoundStudioService
{
    private readonly IAudioExporter audioExporter;
    private readonly IBeatmapEditingGateway beatmaps;
    private readonly HitsoundStudioEngine engine;
    private readonly IBeatmapsetFileSystem files;
    private readonly IAudioGenerator generator;
    private readonly IMidiService midi;
    private readonly IAudioClipMixer mixer;
    private readonly AudioPreviewService preview;
    private readonly IFileRevealService reveal;
    private readonly IMapCleanerSampleService sampleAnalyzer;

    /// <summary>Creates the Hitsound Studio application service.</summary>
    /// <param name="beatmaps">Loads disk-only beatmaps and writes export copies.</param>
    /// <param name="sampleAnalyzer">Finds canonical sample paths in map folders.</param>
    /// <param name="generator">Generates audio through the step-41 audio port.</param>
    /// <param name="preview">Owns playback and session lifetime through the step-41 service.</param>
    /// <param name="audioExporter">Encodes owned neutral clips.</param>
    /// <param name="mixer">Mixes owned neutral clips.</param>
    /// <param name="midi">Imports and exports neutral MIDI events.</param>
    /// <param name="files">Performs export-directory mutations.</param>
    /// <param name="reveal">Opens the completed export directory.</param>
    /// <param name="engine">Applies framework-neutral layer and schema rules.</param>
    public HitsoundStudioService(
        IBeatmapEditingGateway beatmaps,
        IMapCleanerSampleService sampleAnalyzer,
        IAudioGenerator generator,
        AudioPreviewService preview,
        IAudioExporter audioExporter,
        IAudioClipMixer mixer,
        IMidiService midi,
        IBeatmapsetFileSystem files,
        IFileRevealService reveal,
        HitsoundStudioEngine engine)
    {
        this.beatmaps = beatmaps ?? throw new ArgumentNullException(nameof(beatmaps));
        this.sampleAnalyzer = sampleAnalyzer ?? throw new ArgumentNullException(nameof(sampleAnalyzer));
        this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
        this.preview = preview ?? throw new ArgumentNullException(nameof(preview));
        this.audioExporter = audioExporter ?? throw new ArgumentNullException(nameof(audioExporter));
        this.mixer = mixer ?? throw new ArgumentNullException(nameof(mixer));
        this.midi = midi ?? throw new ArgumentNullException(nameof(midi));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));
        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HitsoundLayer>> ImportAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return request.ImportType switch
        {
            ImportType.None =>
            [
                new HitsoundLayer(request.Name, SampleSetOrDefault(request.SampleSet), request.Hitsound,
                    new SampleGeneratingArgs(request.SamplePath), new LayerImportArgs()),
            ],
            ImportType.Stack => await ImportStackAsync(request, cancellationToken).ConfigureAwait(false),
            ImportType.Hitsounds => await ImportHitsoundsAsync(request, cancellationToken).ConfigureAwait(false),
            ImportType.Storyboard => await ImportStoryboardAsync(request, cancellationToken).ConfigureAwait(false),
            ImportType.MIDI => await ImportMidiAsync(request, cancellationToken).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(request.ImportType), request.ImportType, null),
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HitsoundLayer>> ReloadAsync(
        IReadOnlyList<HitsoundLayer> layers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(layers);
        List<HitsoundLayer> reloaded = [];
        foreach (var group in layers
                     .Where(layer => layer.ImportArgs.ImportType != ImportType.None)
                     .GroupBy(layer => layer.ImportArgs.GetImportReloadingArgs(), new ImportReloadingArgsComparer()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imported = await ImportAsync(
                ToImportRequest(group.Key), cancellationToken).ConfigureAwait(false);
            foreach (var layer in group) layer.Reload(imported.ToList());
        }

        return layers;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<SampleGeneratingArgs, Exception>> ValidateSamplesAsync(
        IReadOnlyList<SampleGeneratingArgs> samples,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        Dictionary<SampleGeneratingArgs, Exception> failures =
            new(new SampleGeneratingArgsComparer());
        foreach (var sample in samples.Distinct(new SampleGeneratingArgsComparer()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Load the samples so validation can be done
            if (string.IsNullOrWhiteSpace(sample.Path) || IsSkinDefaultSample(sample.Path)) continue;

            if (!files.FileExists(sample.Path))
            {
                failures[sample] = new FileNotFoundException(
                    $"The sample source does not exist: {sample.Path}", sample.Path);
                continue;
            }

            try
            {
                await generator.GenerateAsync(new AudioGenerationRequest(sample), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures[sample] = exception;
            }
        }

        return failures;
    }

    /// <inheritdoc />
    public Task<IAudioPlaybackSession> PreviewAsync(
        SampleGeneratingArgs sample,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sample);
        return preview.PreviewGeneratedAsync(new AudioGenerationRequest(sample), cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioExportResult> ExportAsync(
        HitsoundStudioServiceOptions project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        Validate(project);
        files.EnsureDirectoryExists(project.ExportFolder);
        bool writesFiles = project.HitsoundExportModeSetting == HitsoundStudioExportMode.Midi
            ? project.ExportMap
            : project.ExportMap || project.ExportSamples;

        Report(progress, 0.05);
        bool validateSampleFile = project.SingleSampleExportFormat != HitsoundStudioSampleExportFormat.MidiChords
                                  && project.MixedSampleExportFormat != HitsoundStudioSampleExportFormat.MidiChords;
        SampleGeneratingArgsComparer comparer = new(validateSampleFile);
        Func<SampleGeneratingArgs, bool> isValid = sample =>
            !validateSampleFile || IsValidSource(sample);
        var mode = project.HitsoundExportModeSetting;
        // Convert the multiple layers into packages that have the samples from all the layers at one specific time
        // Don't add default sample when exporting midi files because that's not a final export.
        var packages = engine.ZipLayers(
            project.HitsoundLayers,
            project.DefaultSample,
            mode == HitsoundStudioExportMode.Standard ? project.ZipLayersLeniency : 0,
            mode == HitsoundStudioExportMode.Standard && validateSampleFile).ToList();
        Report(progress, mode == HitsoundStudioExportMode.Midi ? 0.2 : 0.1);

        // Balance the volume between greenlines and samples
        engine.BalanceVolumes(
            packages,
            0,
            false,
            mode is HitsoundStudioExportMode.Coinciding or
                HitsoundStudioExportMode.Storyboard);
        Report(progress, 0.2);
        Report(progress, mode == HitsoundStudioExportMode.Standard
            ? 0.3
            : mode == HitsoundStudioExportMode.Midi
                ? 0.2
                : 0.5);

        HitsoundStudioStandardResult? standard = null;
        HitsoundStudioNamedResult? named = null;
        SampleSchema schema;
        IReadOnlyList<HitsoundEvent> events;
        if (project.UsePreviousSampleSchema && project.PreviousSampleSchema is null)
            throw new InvalidDataException("A previous sample schema is required when that option is enabled.");

        if (mode == HitsoundStudioExportMode.Standard)
        {
            // Convert the packages to hitsounds that fit on an osu standard map
            standard = engine.BuildStandard(
                packages,
                project.UsePreviousSampleSchema ? project.PreviousSampleSchema : null,
                project.AllowGrowthPreviousSampleSchema,
                project.FirstCustomIndex,
                isValid,
                comparer);
            schema = standard.Schema;
            events = standard.Events;
        }
        else if (mode == HitsoundStudioExportMode.Midi)
        {
            schema = project.PreviousSampleSchema ?? new SampleSchema();
            events = [];
        }
        else
        {
            named = engine.BuildNamed(
                packages,
                project.UsePreviousSampleSchema ? project.PreviousSampleSchema : null,
                mode == HitsoundStudioExportMode.Coinciding && project.HitsoundExportGameMode == GameMode.Mania,
                mode == HitsoundStudioExportMode.Coinciding && project.AddCoincidingRegularHitsounds,
                project.AllowGrowthPreviousSampleSchema,
                isValid,
                comparer);
            schema = named.Schema;
            events = named.Events;
        }

        Report(progress, mode == HitsoundStudioExportMode.Midi
            ? 0.4
            : mode == HitsoundStudioExportMode.Standard
                ? 0.6
                : 0.5);
        if (project.DeleteAllInExportFirst && writesFiles)
        {
            // Delete all files in the export folder before filling it again.
            foreach (string path in files.EnumerateFiles(
                         project.ExportFolder,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                files.Delete(path);
            }
        }

        Report(progress, mode == HitsoundStudioExportMode.Midi
            ? 0.4
            : mode == HitsoundStudioExportMode.Standard
                ? 0.7
                : 0.6);

        // Count the number of samples
        int sampleCount = 0;
        string? mapPath = null;
        Beatmap? midiBeatmap = null;
        if (project.ExportMap && mode != HitsoundStudioExportMode.Midi)
        {
            var session = await beatmaps.OpenBeatmapAsync(
                project.BaseBeatmap,
                LiveBeatmapPreference.DiskOnly,
                cancellationToken).ConfigureAwait(false);
            // Export the hitsound map and sound samples
            ApplyExport(session.Editor.Beatmap, events, project);
            mapPath = Path.Combine(project.ExportFolder, session.Editor.Beatmap.GetFileName());
            session.Editor.SaveFile(mapPath);
            Report(progress, mode == HitsoundStudioExportMode.Standard ? 0.8 : 0.7);
        }
        else if (mode == HitsoundStudioExportMode.Midi)
        {
            var session = await beatmaps.OpenBeatmapAsync(
                project.BaseBeatmap,
                LiveBeatmapPreference.DiskOnly,
                cancellationToken).ConfigureAwait(false);
            midiBeatmap = session.Editor.Beatmap;
            if (project.ExportMap)
            {
                string midiPath = Path.Combine(project.ExportFolder, project.HitsoundDiffName + ".mid");
                await ExportMidiAsync(packages, midiBeatmap, midiPath, project.AddGreenLineVolumeToMidi, cancellationToken)
                    .ConfigureAwait(false);
                mapPath = midiPath;
            }
        }

        if (project.ExportSamples && mode != HitsoundStudioExportMode.Midi)
            sampleCount = standard is not null
                ? await ExportStandardSamplesAsync(standard.Schema, project, comparer, cancellationToken)
                    .ConfigureAwait(false)
                : await ExportNamedSamplesAsync(named!, project, comparer, cancellationToken)
                    .ConfigureAwait(false);

        Report(progress, mode == HitsoundStudioExportMode.Midi ? 1 : 0.99);

        if (writesFiles) await reveal.RevealAsync(project.ExportFolder, cancellationToken).ConfigureAwait(false);

        Report(progress, 1);
        // Count the number of changes of custom index
        string detailedSummary = mode switch
        {
            HitsoundStudioExportMode.Standard =>
                $"Number of sample indices: {standard!.Schema.GetCustomIndices(comparer).Count}, "
                + $"Number of samples: {standard.Schema.Count(entry => entry.Value.Any(isValid))}, "
                + $"Number of greenlines: {CountIndexChanges(events)}",
            HitsoundStudioExportMode.Coinciding or HitsoundStudioExportMode.Storyboard =>
                $"Number of sample indices: 0, Number of samples: "
                + $"{packages.SelectMany(package => package.Samples).Select(sample => sample.SampleArgs).Distinct(comparer).Count()}, "
                + "Number of greenlines: 0",
            _ => $"Number of notes: {packages.Sum(package => package.Samples.Count)}, "
                 + $"Number of volume changes: {(project.AddGreenLineVolumeToMidi ? midiBeatmap?.BeatmapTiming.TimingPoints.Count ?? 0 : 0)}",
        };
        return new HitsoundStudioExportResult(
            mapPath,
            sampleCount,
            project.HitsoundLayers.Count,
            project.HitsoundExportModeSetting == HitsoundStudioExportMode.Midi
                ? packages.Sum(package => package.Samples.Count)
                : events.Count,
            schema,
            detailedSummary);
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportStackAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        string path = OnePath(request);
        var session = await beatmaps.OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
            .ConfigureAwait(false);
        var times = session.Editor.Beatmap.HitObjects
            .Where(hitObject => (request.X == -1 || Math.Abs(hitObject.Pos.X - request.X) < 3) && (request.Y == -1 || Math.Abs(hitObject.Pos.Y - request.Y) < 3))
            .Select(hitObject => hitObject.Time)
            .ToList();
        HitsoundLayer layer = new(request.Name, SampleSetOrDefault(request.SampleSet), request.Hitsound,
            new SampleGeneratingArgs(request.SamplePath), new LayerImportArgs(ImportType.Stack)
            {
                Path = path,
                X = request.X,
                Y = request.Y,
            })
        {
            Times = times,
        };
        return [layer];
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportHitsoundsAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        List<HitsoundLayer> all = [];
        foreach (string path in request.Paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var one = request with { Paths = [path] };
            all.AddRange(await ImportHitsoundsForPathAsync(one, cancellationToken).ConfigureAwait(false));
        }

        PrefixImportedNames(all, request.Name);
        FinishImportedLayers(all, request.RemoveDuplicates);
        return all.OrderBy(layer => layer.Name).ToArray();
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportHitsoundsForPathAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        string path = OnePath(request);
        var session = await beatmaps.OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
            .ConfigureAwait(false);
        var beatmap = session.Editor.Beatmap;
        var timeline = beatmap.GetTimeline();
        var mode = (GameMode)beatmap.General["Mode"].IntValue;
        string mapDirectory = session.Editor.GetParentFolder();
        var firstSamples = await sampleAnalyzer.AnalyzeAsync(
            mapDirectory,
            request.DetectDuplicateSamples,
            cancellationToken).ConfigureAwait(false);
        List<HitsoundLayer> layers = [];
        foreach (var item in timeline.TimelineObjects.Where(item => item.HasHitsound))
        {
            cancellationToken.ThrowIfCancellationRequested();
            double volume = request.DiscriminateVolumes ? item.FenoSampleVolume / 100 : 1;
            foreach (string filename in item.GetPlayingFilenames(mode))
            {
                bool explicitFilename = item.UsesFilename;
                var sampleSet = explicitFilename ? item.FenoSampleSet : HitsoundFilename.GetSampleSet(filename);
                var hitsound = explicitFilename ? item.GetHitsound() : HitsoundFilename.GetHitsound(filename);
                string source = Path.Combine(mapDirectory, filename);
                string extensionless = Path.Combine(
                    Path.GetDirectoryName(source) ?? mapDirectory,
                    Path.GetFileNameWithoutExtension(source));
                if (firstSamples.TryGetValue(extensionless, out string? canonical))
                    source = canonical;
                else if (!explicitFilename)
                    source = Path.Combine(mapDirectory,
                        $"{sampleSet.ToString().ToLowerInvariant()}-hit{hitsound.ToString().ToLowerInvariant()}-1.wav");

                LayerImportArgs import = new(ImportType.Hitsounds)
                {
                    Path = path,
                    SamplePath = source,
                    Volume = volume,
                    DetectDuplicateSamples = request.DetectDuplicateSamples,
                    DiscriminateVolumes = request.DiscriminateVolumes,
                    RemoveDuplicates = request.RemoveDuplicates,
                };
                var existing = layers.FirstOrDefault(layer => layer.ImportArgs.Equals(import));
                if (existing is null)
                {
                    existing = new HitsoundLayer(
                        Path.GetFileNameWithoutExtension(source),
                        sampleSet,
                        hitsound,
                        new SampleGeneratingArgs(source) { Volume = volume },
                        import);
                    layers.Add(existing);
                }

                existing.Times.Add(item.Time);
            }
        }

        if (request.IncludeStoryboard) layers.AddRange(ImportStoryboardFromBeatmap(path, beatmap, mapDirectory, request, "SB: "));

        return layers.OrderBy(layer => layer.Name).ToArray();
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportStoryboardAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        List<HitsoundLayer> all = [];
        foreach (string path in request.Paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var one = request with { Paths = [path] };
            all.AddRange(await ImportStoryboardForPathAsync(one, cancellationToken).ConfigureAwait(false));
        }

        PrefixImportedNames(all, request.Name);
        FinishImportedLayers(all, request.RemoveDuplicates);
        return all.OrderBy(layer => layer.Name).ToArray();
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportStoryboardForPathAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        string path = OnePath(request);
        var session = await beatmaps.OpenBeatmapAsync(path, LiveBeatmapPreference.DiskOnly, cancellationToken)
            .ConfigureAwait(false);
        var layers = ImportStoryboardFromBeatmap(
            path,
            session.Editor.Beatmap,
            session.Editor.GetParentFolder(),
            request,
            string.Empty);
        return layers.OrderBy(layer => layer.Name).ToArray();
    }

    private async Task<IReadOnlyList<HitsoundLayer>> ImportMidiAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken)
    {
        string path = OnePath(request);
        var sequence = await midi.ImportAsync(new MidiImportRequest(path), cancellationToken)
            .ConfigureAwait(false);
        List<HitsoundLayer> layers = [];
        foreach (var note in sequence.Notes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bank = request.DiscriminateInstruments ? note.Bank : -1;
            int patch = request.DiscriminateInstruments ? note.Patch : -1;
            bool percussion = bank == 128;
            int key = request.DiscriminateKeys || percussion ? note.Key : -1;
            double length = request.DiscriminateLengths
                ? RoundLength(note.DurationMilliseconds, request.LengthRoughness)
                : -1;
            int velocity = request.DiscriminateVelocities
                ? (int)RoundVelocity(note.Velocity, request.VelocityRoughness)
                : -1;
            LayerImportArgs import = new(ImportType.MIDI)
            {
                Path = path,
                Bank = bank,
                Patch = patch,
                Key = key,
                Length = length,
                LengthRoughness = request.LengthRoughness,
                Velocity = velocity,
                VelocityRoughness = request.VelocityRoughness,
                Offset = request.Offset,
            };
            var layer = layers.FirstOrDefault(candidate => candidate.ImportArgs.Equals(import));
            if (layer is null)
            {
                string instrumentName = note.InstrumentName ?? (bank == 128 ? "Percussion" : patch is >= 0 and < 128 ? patch.ToString() : "Undefined");
                string keyName = note.KeyName ?? key.ToString();
                string name = instrumentName;
                if (request.DiscriminateKeys || percussion) name += "," + keyName;
                if (request.DiscriminateLengths) name += "," + Math.Round(length).ToString("0.###", CultureInfo.InvariantCulture);
                if (request.DiscriminateVelocities) name += "," + velocity;
                layer = new HitsoundLayer(
                    name,
                    SampleSet.Normal,
                    Hitsound.Normal,
                    new SampleGeneratingArgs(string.Empty, bank, patch, -1, key, length, velocity),
                    import);
                layers.Add(layer);
            }

            layer.Times.Add(note.StartMilliseconds + request.Offset);
        }

        int maximumVelocity = layers.Count == 0 ? 0 : layers.Max(layer => layer.SampleArgs.Velocity);
        if (maximumVelocity > 0)
            foreach (var layer in layers)
                layer.SampleArgs.Velocity = (int)Math.Round(layer.SampleArgs.Velocity / (double)maximumVelocity * 127);

        foreach (var layer in layers) layer.Times.Sort();
        PrefixImportedNames(layers, request.Name);
        return layers.OrderBy(layer => layer.Name).ToArray();
    }

    private List<HitsoundLayer> ImportStoryboardFromBeatmap(
        string path,
        Beatmap beatmap,
        string mapDirectory,
        HitsoundStudioImportRequest request,
        string prefix)
    {
        List<HitsoundLayer> layers = [];
        foreach (var sound in beatmap.StoryboardSoundSamples)
        {
            double volume = request.DiscriminateVolumes ? sound.Volume / 100 : 1;
            string source = Path.Combine(mapDirectory, sound.FilePath);
            string filename = Path.GetFileNameWithoutExtension(sound.FilePath);
            var sampleSet = HitsoundFilename.GetSampleSet(filename);
            var hitsound = HitsoundFilename.GetHitsound(filename);
            LayerImportArgs import = new(ImportType.Storyboard)
            {
                Path = path,
                SamplePath = source,
                Volume = volume,
                DiscriminateVolumes = request.DiscriminateVolumes,
                RemoveDuplicates = request.RemoveDuplicates,
            };
            var layer = layers.FirstOrDefault(candidate => candidate.ImportArgs.Equals(import));
            if (layer is null)
            {
                layer = new HitsoundLayer(prefix + filename, sampleSet, hitsound,
                    new SampleGeneratingArgs(source) { Volume = volume }, import);
                layers.Add(layer);
            }

            layer.Times.Add(sound.StartTime);
        }

        return layers;
    }

    private async Task<int> ExportStandardSamplesAsync(
        SampleSchema schema,
        HitsoundStudioServiceOptions project,
        SampleGeneratingArgsComparer comparer,
        CancellationToken cancellationToken)
    {
        int count = 0;
        foreach ((string key, var source) in schema)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source.Count == 0) continue;
            string name = key;
            string output = await ExportSourceGroupAsync(
                source,
                name,
                project.ExportFolder,
                project.SingleSampleExportFormat,
                project.MixedSampleExportFormat,
                comparer,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(output)) count++;
        }

        return count;
    }

    private async Task<int> ExportNamedSamplesAsync(
        HitsoundStudioNamedResult named,
        HitsoundStudioServiceOptions project,
        SampleGeneratingArgsComparer comparer,
        CancellationToken cancellationToken)
    {
        int count = 0;
        foreach (var group in named.Names
                     .Where(pair => !string.IsNullOrEmpty(pair.Value))
                     .GroupBy(pair => pair.Value, StringComparer.Ordinal))
        {
            string output = await ExportSourceGroupAsync(
                group.Select(pair => pair.Key).ToArray(),
                group.Key,
                project.ExportFolder,
                project.SingleSampleExportFormat,
                project.MixedSampleExportFormat,
                comparer,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(output)) count++;
        }

        return count;
    }

    private async Task<string> ExportSourceGroupAsync(
        IReadOnlyList<SampleGeneratingArgs> source,
        string name,
        string exportFolder,
        HitsoundStudioSampleExportFormat singleFormat,
        HitsoundStudioSampleExportFormat mixedFormat,
        SampleGeneratingArgsComparer comparer,
        CancellationToken cancellationToken)
    {
        var valid = source.Where(IsValidSource).Distinct(comparer).ToList();
        if (valid.Count == 0) return string.Empty;

        var format = valid.Count == 1 ? singleFormat : mixedFormat;
        if (format == HitsoundStudioSampleExportFormat.MidiChords)
        {
            await ExportMidiChordsAsync(valid, Path.Combine(exportFolder, name + ".mid"), cancellationToken)
                .ConfigureAwait(false);
            return name + ".mid";
        }

        if (valid.Count == 1 && format == HitsoundStudioSampleExportFormat.Default && valid[0].CanCopyPaste)
        {
            string destination = Path.Combine(exportFolder, name + valid[0].GetExtension());
            files.CopyFile(valid[0].Path, destination, true);
            return Path.GetFileName(destination);
        }

        List<AudioClip> clips = [];
        foreach (var sample in valid.Distinct(comparer))
        {
            cancellationToken.ThrowIfCancellationRequested();
            clips.Add(await generator.GenerateAsync(new AudioGenerationRequest(sample), cancellationToken)
                .ConfigureAwait(false));
        }

        var clip = clips.Count == 1
            ? clips[0]
            : await mixer.MixAsync(clips, cancellationToken).ConfigureAwait(false);
        var audioFormat = format switch
        {
            HitsoundStudioSampleExportFormat.WavePcm => AudioExportFormat.WavePcm,
            HitsoundStudioSampleExportFormat.OggVorbis => AudioExportFormat.OggVorbis,
            _ => AudioExportFormat.WaveIeeeFloat,
        };
        string extension = audioFormat == AudioExportFormat.OggVorbis ? ".ogg" : ".wav";
        string path = Path.Combine(exportFolder, name + extension);
        await audioExporter.ExportAsync(clip, new AudioExportRequest(path, audioFormat), cancellationToken)
            .ConfigureAwait(false);
        return Path.GetFileName(path);
    }

    private async Task<int> ExportMidiChordsAsync(
        IReadOnlyList<SamplePackage> packages,
        HitsoundStudioServiceOptions project,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(project.ExportFolder, project.HitsoundDiffName + ".mid");
        var notes = packages.SelectMany(package => package.Samples.Select(sample =>
            ToMidiNote(package.Time, sample.SampleArgs))).ToList();
        await midi.ExportAsync(new MidiExportRequest(path, new MidiSequence(notes)), cancellationToken)
            .ConfigureAwait(false);
        return notes.Count == 0 ? 0 : 1;
    }

    private async Task ExportMidiChordsAsync(
        IReadOnlyList<SampleGeneratingArgs> samples,
        string path,
        CancellationToken cancellationToken)
    {
        var notes = samples.Where(sample => sample.Key >= 0)
            .Select(sample => ToMidiNote(0, sample)).ToList();
        if (notes.Count > 0)
            await midi.ExportAsync(new MidiExportRequest(path, new MidiSequence(notes)), cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task ExportMidiAsync(
        IReadOnlyList<SamplePackage> packages,
        Beatmap beatmap,
        string path,
        bool addGreenLineVolume,
        CancellationToken cancellationToken)
    {
        double bpm = beatmap.BeatmapTiming.Redlines.Count > 0
            ? beatmap.BeatmapTiming.Redlines[0].GetBpm()
            : 60;
        var notes = packages.SelectMany(package => package.Samples.Select(sample =>
            ToMidiNote(package.Time, sample.SampleArgs))).ToList();
        List<MidiVolumeChange> volumeChanges = [];
        if (addGreenLineVolume)
            foreach (var point in beatmap.BeatmapTiming.TimingPoints)
                for (int channel = 1; channel <= 16; channel++)
                    volumeChanges.Add(new MidiVolumeChange(
                        Math.Max(0, point.Offset),
                        channel,
                        Math.Clamp((int)(point.Volume * 127 / 100), 0, 127)));

        await midi.ExportAsync(
            new MidiExportRequest(path, new MidiSequence(notes, volumeChanges), bpm),
            cancellationToken).ConfigureAwait(false);
    }

    private static MidiNote ToMidiNote(double time, SampleGeneratingArgs sample)
    {
        return new MidiNote(
            Math.Max(0, time),
            Math.Max(0, sample.Length < 0 ? 0 : sample.Length),
            Math.Max(0, sample.Bank),
            Math.Clamp(sample.Patch, 0, 127),
            Math.Clamp(sample.Key, 0, 127),
            Math.Clamp(sample.Velocity, 0, 127));
    }

    private bool IsValidSource(SampleGeneratingArgs sample)
    {
        return !string.IsNullOrWhiteSpace(sample.Path) && files.FileExists(sample.Path);
    }

    private static bool IsSkinDefaultSample(string path)
    {
        string filename = Path.GetFileName(path);
        string[] sets = ["none", "normal", "soft", "drum"];
        string[] sounds = ["normal", "whistle", "finish", "clap"];
        return filename.EndsWith("-1.wav", StringComparison.OrdinalIgnoreCase)
               && sets.Any(set => sounds.Any(sound =>
                   filename.Equals($"{set}-hit{sound}-1.wav", StringComparison.OrdinalIgnoreCase)));
    }

    private static int CountIndexChanges(IReadOnlyList<HitsoundEvent> events)
    {
        int count = 0;
        int lastIndex = -1;
        foreach (var item in events)
        {
            if (item.CustomIndex == lastIndex) continue;
            lastIndex = item.CustomIndex;
            count++;
        }

        return count;
    }

    private void ApplyExport(
        Beatmap beatmap,
        IReadOnlyList<HitsoundEvent> events,
        HitsoundStudioServiceOptions project)
    {
        if (project.HitsoundExportModeSetting == HitsoundStudioExportMode.Storyboard)
        {
            beatmap.StoryboardSoundSamples.Clear();
            foreach (var item in events.Where(item => !string.IsNullOrEmpty(item.Filename)))
                beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample(
                    item.Time,
                    0,
                    item.Filename,
                    item.Volume * 100));
        }
        else
        {
            var timingPoints = project.HitsoundExportModeSetting == HitsoundStudioExportMode.Standard
                ? engine.BuildStandardTimingPoints(beatmap.BeatmapTiming, events).ToList()
                : beatmap.BeatmapTiming.Redlines.Select(point => point.Copy()).ToList();
            beatmap.HitObjects.Clear();
            foreach (var item in events)
            {
                int customIndex = item.CustomIndex;
                double volume = item.Volume * 100;
                if (project.HitsoundExportModeSetting == HitsoundStudioExportMode.Standard)
                {
                    customIndex = 0;
                    volume = 0;
                }

                beatmap.HitObjects.Add(new HitObject(
                    item.Pos,
                    item.Time,
                    5,
                    item.GetHitsounds(),
                    item.SampleSet,
                    item.Additions,
                    customIndex,
                    volume,
                    item.Filename));
            }

            beatmap.BeatmapTiming.SetTimingPoints(timingPoints);
        }

        beatmap.General["StackLeniency"] = new StringValue("0.0");
        beatmap.General["Mode"] = new StringValue(((int)project.HitsoundExportGameMode).ToString(CultureInfo.InvariantCulture));
        beatmap.Metadata["Version"] = new StringValue(project.HitsoundDiffName);
        int keys = project.HitsoundExportGameMode == GameMode.Mania
            ? Math.Clamp(events.Select(item => item.Pos.X).Distinct().Count(), 1, 18)
            : 4;
        beatmap.Difficulty["CircleSize"] = new StringValue(keys.ToString(CultureInfo.InvariantCulture));
    }

    private static void PrefixImportedNames(IEnumerable<HitsoundLayer> layers, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;
        foreach (var layer in layers) layer.Name = $"{prefix}: {layer.Name}";
    }

    private static void FinishImportedLayers(List<HitsoundLayer> layers, bool removeDuplicates)
    {
        foreach (var layer in layers)
        {
            if (!removeDuplicates) continue;
            layer.Times.Sort();
            layer.RemoveDuplicates();
        }
    }

    private static HitsoundStudioImportRequest ToImportRequest(ImportReloadingArgs args)
    {
        return new HitsoundStudioImportRequest
        {
            ImportType = args.ImportType,
            Paths = [args.Path],
            X = args.X,
            Y = args.Y,
            DiscriminateLengths = true,
            DiscriminateVelocities = true,
            LengthRoughness = args.LengthRoughness,
            VelocityRoughness = args.VelocityRoughness,
            DiscriminateVolumes = args.DiscriminateVolumes,
            DetectDuplicateSamples = args.DetectDuplicateSamples,
            RemoveDuplicates = args.RemoveDuplicates,
            Offset = args.Offset,
        };
    }

    private static string OnePath(HitsoundStudioImportRequest request)
    {
        return request.Paths.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path))
               ?? throw new InvalidDataException("An import source path is required.");
    }

    private static SampleSet SampleSetOrDefault(SampleSet sampleSet)
    {
        return sampleSet == SampleSet.None ? SampleSet.Normal : sampleSet;
    }

    private static double RoundVelocity(double velocity, double roughness)
    {
        if (velocity < 0 || roughness <= 0) return velocity;
        return Math.Round(velocity / roughness) * roughness;
    }

    private static double RoundLength(double length, double roughness)
    {
        if (length < 0 || roughness <= 0) return length;
        return Math.Pow(Math.Ceiling(Math.Pow(length, 1 / roughness)), roughness);
    }

    private static void Validate(HitsoundStudioServiceOptions project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.BaseBeatmap);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.ExportFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(project.HitsoundDiffName);
        ArgumentNullException.ThrowIfNull(project.DefaultSample);
        if (project.HitsoundLayers is null || project.HitsoundLayers.Count == 0)
            throw new ArgumentException("There are no hitsound layers.", nameof(project));
        if (!Enum.IsDefined(project.HitsoundExportModeSetting)
            || !Enum.IsDefined(project.HitsoundExportGameMode)
            || !Enum.IsDefined(project.SingleSampleExportFormat)
            || !Enum.IsDefined(project.MixedSampleExportFormat))
            throw new ArgumentException("Hitsound Studio contains an unknown export setting.", nameof(project));
    }

    private static void Report(IProgress<double>? progress, double value)
    {
        progress?.Report(value);
    }
}
