using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Core.Tools.TumourGenerator;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using CoreTumourGenerator = Mapping_Tools.Core.Tools.TumourGenerator.TumourGenerator;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

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
        HitObjectSelectionMode mode,
        string? timeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!Enum.IsDefined(mode)) throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(mode));

        var session = await editingGateway.OpenBeatmapAsync(
                path,
                mode == HitObjectSelectionMode.Selected ? LiveBeatmapPreference.RequireLive : LiveBeatmapPreference.DiskOnly,
                cancellationToken)
            .ConfigureAwait(false);
        var markedObjects = BeatmapObjectSelection.Select(session, mode, timeCode);
        double circleSize = session.Editor.Beatmap.Difficulty["CircleSize"].DoubleValue;
        return new TumourImportResult(
            markedObjects.Where(hitObject => hitObject.IsSlider).ToArray(),
            circleSize,
            session.Source == BeatmapEditingSource.LiveEditor);
    }

    /// <inheritdoc />
    public Task<TumourPreviewResult> PreviewAsync(
        HitObject previewHitObject,
        TumourGeneratorEngineOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previewHitObject);
        ArgumentNullException.ThrowIfNull(options);
        CoreTumourGenerator.Validate(options);
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
        TumourGeneratorServiceOptions project,
        bool reloadEditor,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(project);
        if (paths.Count == 0) throw new ArgumentException("At least one beatmap path is required.", nameof(paths));
        Validate(project);

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
                    project.ImportModeSetting == HitObjectSelectionMode.Selected
                        ? LiveBeatmapPreference.RequireLive
                        : LiveBeatmapPreference.DiskOnly,
                    cancellationToken)
                .ConfigureAwait(false);
            // Load sliders from the selector
            var markedObjects = BeatmapObjectSelection.Select(
                session,
                project.ImportModeSetting,
                project.TimeCode);
            for (int objectIndex = 0; objectIndex < markedObjects.Count; objectIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Generate copious amounts of tumours on each slider
                if (generator.TumourGenerate(markedObjects[objectIndex], cancellationToken)) generatedCount++;
                progress?.Report((pathIndex + (objectIndex + 1d) / Math.Max(markedObjects.Count, 1)) / paths.Count);
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
            progress?.Report(pathIndex + 1, paths.Count);
        }

        progress?.Report(1);
        return new TumourRunResult(completedPaths, generatedCount, editorReloaded);
    }

    private static CoreTumourGenerator CreateGenerator(TumourGeneratorEngineOptions options)
    {
        return new CoreTumourGenerator
        {
            TumourLayers = options.TumourLayers,
            JustMiddleAnchors = options.JustMiddleAnchors,
            Scalar = options.Scale,
            Reconstructor = new Reconstructor { DebugConstruction = options.DebugConstruction },
        };
    }

    private static void Validate(TumourGeneratorServiceOptions project)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!Enum.IsDefined(project.ImportModeSetting))
            throw new ArgumentException("Tumour Generator contains an unknown import mode.", nameof(project));
        CoreTumourGenerator.Validate(project);
    }

}
