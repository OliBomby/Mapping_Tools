using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.Tools.HitsoundCopier;

namespace Mapping_Tools.Application.Tools.HitsoundCopier;

/// <summary>Coordinates source selection, multi-map transformation, sample ports, and safe saves.</summary>
public sealed class HitsoundCopierService : IHitsoundCopierService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IHitsoundSampleService samples;
    private readonly ApplicationSettings settings;

    /// <summary>Creates the Hitsound Copier application service.</summary>
    /// <param name="editingGateway">Loads live-aware maps and saves through the backup boundary.</param>
    /// <param name="samples">Supplies file/audio sample discovery and export.</param>
    /// <param name="settings">Supplies the Editor Reader preference.</param>
    public HitsoundCopierService(
        IBeatmapEditingGateway editingGateway,
        IHitsoundSampleService samples,
        ApplicationSettings settings)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.samples = samples ?? throw new ArgumentNullException(nameof(samples));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<HitsoundCopierResult> CopyAsync(
        HitsoundCopierServiceOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        string[] targetPaths = options.PathTo
            .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        BeatmapEditingSession? sourceSession = null;
        if (!string.IsNullOrWhiteSpace(options.PathFrom))
        {
            var preference = options.SourceSelectionMode == HitObjectSelectionMode.Selected
                ? LiveBeatmapPreference.RequireLive
                : LiveBeatmapPreference.PreferLive;
            sourceSession = await editingGateway.OpenBeatmapAsync(
                options.PathFrom,
                preference,
                cancellationToken).ConfigureAwait(false);
        }

        List<string> processed = [];
        SampleSchema schema = new();
        int matched = 0;
        int generated = 0;
        int muted = 0;
        for (int index = 0; index < targetPaths.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetSession = await editingGateway.OpenBeatmapAsync(
                targetPaths[index],
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            var source = sourceSession?.Editor.Beatmap ?? CreateEmptySource(targetSession.Editor.Beatmap);
            var sourceObjects = sourceSession is null
                ? []
                : SelectSourceObjects(sourceSession, options);
            string? targetDirectory = Path.GetDirectoryName(targetPaths[index]);
            string mapDirectory = string.IsNullOrWhiteSpace(targetDirectory)
                ? Directory.GetCurrentDirectory()
                : targetDirectory;

            bool inspectTargetSamples = options.CopyMode == 1 || options.CopyStoryboardedSamples && options.IgnoreHitsoundSatisfiedSamples;
            var firstSamples = inspectTargetSamples
                ? await samples.AnalyzeAsync(mapDirectory, cancellationToken).ConfigureAwait(false)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string sourceDirectory = sourceSession is null
                ? mapDirectory
                : GetDirectory(options.PathFrom);
            var sourceSamples = sourceSession is not null && options.CopyMode == 1 && (options.CopyToSliderTicks || options.CopyToSliderSlides)
                ? await samples.AnalyzeAsync(sourceDirectory, cancellationToken).ConfigureAwait(false)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var result = HitsoundCopierEngine.Apply(
                targetSession.Editor.Beatmap,
                source,
                sourceObjects,
                options,
                mapDirectory,
                firstSamples,
                sourceDirectory,
                sourceSamples,
                request => samples.TryCreateAssignment(
                    sourceDirectory,
                    request.SourceFilenames,
                    sourceSamples,
                    request.Role,
                    request.SampleSet,
                    request.StartIndex,
                    schema),
                schema,
                cancellationToken);
            await editingGateway.SaveAsync(
                targetSession,
                settings.AutoReload,
                cancellationToken).ConfigureAwait(false);
            processed.Add(targetPaths[index]);
            matched += result.MatchedHitsoundCount;
            generated += result.GeneratedSampleCount;
            muted += result.MutedEdgeCount;
            schema.MergeWith(result.SampleSchema);
            progress?.Report(index + 1, targetPaths.Length);
        }

        if (schema.Count > 0) await samples.ExportAsync(schema, cancellationToken).ConfigureAwait(false);
        return new HitsoundCopierResult(processed, matched, generated, muted, schema);
    }

    private static IReadOnlyList<HitObject> SelectSourceObjects(
        BeatmapEditingSession session,
        HitsoundCopierServiceOptions options)
    {
        return BeatmapObjectSelection.Select(
            session,
            options.SourceSelectionMode,
            options.TimeCode);
    }

    private static Beatmap CreateEmptySource(Beatmap target)
    {
        var empty = target.DeepCopy();
        empty.HitObjects.Clear();
        empty.BeatmapTiming.Clear();
        empty.StoryboardSoundSamples.Clear();
        return empty;
    }

    private static string GetDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        return string.IsNullOrWhiteSpace(directory)
            ? Directory.GetCurrentDirectory()
            : directory;
    }

    private static void Validate(HitsoundCopierServiceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PathTo);
        if (options.PathTo.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 0)
            throw new ArgumentException("Select at least one target beatmap.", nameof(options));
        if (options.CopyMode is not 0 and not 1) throw new ArgumentException("Hitsound Copier received an unknown copy mode.", nameof(options));
        if (!Enum.IsDefined(options.SourceSelectionMode)) throw new ArgumentException("Hitsound Copier received an unknown source selection mode.", nameof(options));
        if (options.SourceSelectionMode == HitObjectSelectionMode.Time && string.IsNullOrWhiteSpace(options.TimeCode))
            throw new ArgumentException("A time code is required for Time mode.", nameof(options));
        if (options.TemporalLeniency < 0
            || !double.IsFinite(options.TemporalLeniency)
            || !double.IsFinite(options.TimingOffset)
            || !double.IsFinite(options.MinLength)
            || options.MinLength < 0)
            throw new ArgumentException("Hitsound Copier timing and filter values are invalid.", nameof(options));
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0 || options.MutedDivisors is null || options.MutedDivisors.Length == 0)
            throw new ArgumentException("Hitsound Copier requires beat divisors for its filter.", nameof(options));
    }
}
