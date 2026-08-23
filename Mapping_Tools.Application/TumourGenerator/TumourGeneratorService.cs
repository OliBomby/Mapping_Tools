using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Core.Tools.TumourGenerating;
using CoreTumourGenerator = Mapping_Tools.Core.Tools.TumourGenerating.TumourGenerator;

namespace Mapping_Tools.Application.TumourGenerator;

/// <summary>
///     Runs Tumour Generator 2 through the shared beatmap editing, backup, and
///     editor-reload boundaries.
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

    /// <inheritdoc />
    public async Task<TumourImportResult> ImportAsync(
        string path,
        TumourImportMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(mode)) throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(mode));

        var session = await editingGateway.OpenBeatmapAsync(
                path,
                mode == TumourImportMode.Selected ? LiveBeatmapPreference.RequireLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        var markedObjects = SelectObjects(session, mode, timeCode);
        double circleSize = session.Editor.Beatmap.Difficulty["CircleSize"].DoubleValue;
        return new TumourImportResult(
            markedObjects.Where(hitObject => hitObject.IsSlider).ToArray(),
            circleSize,
            session.Source == BeatmapEditingSource.LiveEditor);
    }

    /// <inheritdoc />
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
            var result = previewHitObject.DeepCopy();
            var generator = CreateGenerator(options);
            generator.TumourGenerate(result, cancellationToken);
            return new TumourPreviewResult(result, generator.LayerLengths.ToArray());
        }, cancellationToken);
    }

    /// <inheritdoc />
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

        int generatedCount = 0;
        bool editorReloaded = false;
        var completedPaths = new List<string>(paths.Count);
        // Initialize the Tumour Generator
        var generator = CreateGenerator(project);
        for (int pathIndex = 0; pathIndex < paths.Count; pathIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string path = paths[pathIndex];
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            var session = await editingGateway.OpenBeatmapAsync(
                    path,
                    project.ImportModeSetting == TumourImportMode.Selected
                        ? LiveBeatmapPreference.RequireLive
                        : LiveBeatmapPreference.DiskOnly,
                    cancellationToken)
                .ConfigureAwait(false);
            // Load sliders from the selector
            var markedObjects = SelectObjects(session, project.ImportModeSetting, project.TimeCode);
            for (int objectIndex = 0; objectIndex < markedObjects.Count; objectIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Generate copious amounts of tumours on each slider
                if (generator.TumourGenerate(markedObjects[objectIndex], cancellationToken)) generatedCount++;
                progress?.Report(100d * (pathIndex + (objectIndex + 1d) / Math.Max(markedObjects.Count, 1)) / paths.Count);
            }

            if (project.FixSv)
                // Reconstruct SliderVelocity (stolen from completionator)
                TumourSliderVelocityFixer.Fix(
                    session.Editor.Beatmap,
                    markedObjects,
                    project.DelegateToBpm,
                    project.RemoveSliderTicks,
                    cancellationToken);

            bool shouldReload = reloadEditor && session.Source == BeatmapEditingSource.LiveEditor;
            // Save the file
            await editingGateway.SaveAsync(session, shouldReload, cancellationToken).ConfigureAwait(false);
            editorReloaded |= shouldReload;
            completedPaths.Add(path);
            progress?.Report(100d * (pathIndex + 1d) / paths.Count);
        }

        progress?.Report(100);
        return new TumourRunResult(completedPaths, generatedCount, editorReloaded);
    }

    private static CoreTumourGenerator CreateGenerator(TumourGeneratorOptions options)
    {
        return new CoreTumourGenerator
        {
            TumourLayers = options.TumourLayers,
            JustMiddleAnchors = options.JustMiddleAnchors,
            Scalar = options.Scale,
            Reconstructor = new Reconstructor { DebugConstruction = options.DebugConstruction },
        };
    }

    private static IReadOnlyList<HitObject> SelectObjects(
        BeatmapEditingSession session,
        TumourImportMode mode,
        string? timeCode)
    {
        return mode switch
        {
            TumourImportMode.Selected => session.SelectedHitObjects,
            TumourImportMode.Bookmarked => session.Editor.Beatmap.GetBookmarkedObjects(),
            TumourImportMode.Time => session.Editor.Beatmap.QueryTimeCode(timeCode ?? string.Empty).ToList(),
            TumourImportMode.Everything => session.Editor.Beatmap.HitObjects,
            _ => throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(mode)),
        };
    }
}
