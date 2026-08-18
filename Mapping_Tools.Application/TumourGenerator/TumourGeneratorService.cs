using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders.Newgen;
using CoreTumourGenerator = Mapping_Tools.Core.Tools.TumourGenerating.TumourGenerator;
using Mapping_Tools.Core.Tools.TumourGenerating;

namespace Mapping_Tools.Application.TumourGenerator;

/// <summary>
/// Runs Tumour Generator 2 through the shared beatmap editing, backup, and
/// editor-reload boundaries.
/// </summary>
public sealed class TumourGeneratorService : ITumourGeneratorService
{
    private readonly IBeatmapEditingGateway editingGateway;

    /// <summary>Creates the service over the shared editing gateway.</summary>
    /// <param name="editingGateway">Loads live or disk maps and saves backup-first.</param>
    public TumourGeneratorService(IBeatmapEditingGateway editingGateway)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
    }

    /// <inheritdoc/>
    public async Task<TumourImportResult> ImportAsync(
        string path,
        TumourImportMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(mode)) throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(mode));

        BeatmapEditingSession session = await editingGateway.OpenBeatmapAsync(
                path,
                mode == TumourImportMode.Selected ? LiveBeatmapPreference.RequireLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyList<HitObject> markedObjects = SelectObjects(session, mode, timeCode);
        double circleSize = session.Editor.Beatmap.Difficulty["CircleSize"].DoubleValue;
        return new TumourImportResult(
            markedObjects.Where(hitObject => hitObject.IsSlider).ToArray(),
            circleSize,
            session.Source == BeatmapEditingSource.LiveEditor);
    }

    /// <inheritdoc/>
    public Task<TumourPreviewResult> PreviewAsync(
        HitObject previewHitObject,
        TumourGeneratorOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previewHitObject);
        ArgumentNullException.ThrowIfNull(options);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            HitObject result = previewHitObject.DeepCopy();
            CoreTumourGenerator generator = CreateGenerator(options);
            generator.TumourGenerate(result, cancellationToken);
            return new TumourPreviewResult(result, generator.LayerLengths.ToArray());
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TumourRunResult> RunAsync(
        IReadOnlyList<string> paths,
        TumourGeneratorProject project,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(project);
        if (paths.Count == 0) throw new ArgumentException("At least one beatmap path is required.", nameof(paths));

        var generatedCount = 0;
        var editorReloaded = false;
        var completedPaths = new List<string>(paths.Count);
        CoreTumourGenerator generator = CreateGenerator(project);
        for (var pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[pathIndex];
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            BeatmapEditingSession session = await editingGateway.OpenBeatmapAsync(
                    path,
                    project.ImportModeSetting == TumourImportMode.Selected
                        ? LiveBeatmapPreference.RequireLive
                        : LiveBeatmapPreference.DiskOnly,
                    cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<HitObject> markedObjects = SelectObjects(session, project.ImportModeSetting, project.TimeCode);
            for (var objectIndex = 0; objectIndex < markedObjects.Count; objectIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (generator.TumourGenerate(markedObjects[objectIndex], cancellationToken)) generatedCount++;
                progress?.Report(100d * (pathIndex + (objectIndex + 1d) / Math.Max(markedObjects.Count, 1)) / paths.Count);
            }

            if (project.FixSv)
            {
                TumourSliderVelocityFixer.Fix(
                    session.Editor.Beatmap,
                    markedObjects,
                    project.DelegateToBpm,
                    project.RemoveSliderTicks,
                    cancellationToken);
            }

            bool shouldReload = reloadEditor && session.Source == BeatmapEditingSource.LiveEditor;
            await editingGateway.SaveAsync(session, shouldReload, cancellationToken).ConfigureAwait(false);
            editorReloaded |= shouldReload;
            completedPaths.Add(path);
            progress?.Report(100d * (pathIndex + 1d) / paths.Count);
        }

        progress?.Report(100);
        return new TumourRunResult(completedPaths, generatedCount, editorReloaded);
    }

    private static CoreTumourGenerator CreateGenerator(TumourGeneratorOptions options) => new()
    {
        TumourLayers = options.TumourLayers,
        JustMiddleAnchors = options.JustMiddleAnchors,
        Scalar = options.Scale,
        Reconstructor = new Reconstructor { DebugConstruction = options.DebugConstruction }
    };

    private static IReadOnlyList<HitObject> SelectObjects(
        BeatmapEditingSession session,
        TumourImportMode mode,
        string? timeCode) => mode switch
    {
        TumourImportMode.Selected => session.SelectedHitObjects,
        TumourImportMode.Bookmarked => session.Editor.Beatmap.GetBookmarkedObjects(),
        TumourImportMode.Time => session.Editor.Beatmap.QueryTimeCode(timeCode ?? string.Empty).ToList(),
        TumourImportMode.Everything => session.Editor.Beatmap.HitObjects,
        _ => throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(mode))
    };
}
