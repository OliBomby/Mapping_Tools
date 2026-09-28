using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Progress;
using Mapping_Tools.Core.ToolHelpers.Sliders;
using Mapping_Tools.Core.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Core.Tools.TumourGenerator;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mapping_Tools.Application.Tools.TumourGenerator;

/// <summary>
///     Runs Tumour Generator 2 through the shared beatmap editing, backup, and
///     editor-reload boundaries.
/// </summary>
public sealed class TumourGeneratorService : ITumourGeneratorService
{
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly ApplicationSettings settings;
    private readonly ILogger<TumourGeneratorService> logger;

    /// <summary>Creates the service over the shared editing gateway.</summary>
    /// <param name="editingGateway">Loads live or disk maps and saves backup-first.</param>
    /// <param name="settings">Supplies the automatic editor reload preference.</param>
    /// <param name="logger">Records import and per-beatmap generation milestones.</param>
    public TumourGeneratorService(
        IBeatmapEditingGateway editingGateway,
        ApplicationSettings settings,
        ILogger<TumourGeneratorService>? logger = null)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? NullLogger<TumourGeneratorService>.Instance;
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
                mode == HitObjectSelectionMode.Selected ? LiveBeatmapPreference.RequireLive : LiveBeatmapPreference.PreferLive,
                cancellationToken)
            .ConfigureAwait(false);
        var markedObjects = BeatmapObjectSelection.Select(session, mode, timeCode);
        logger.LogInformation("Imported {SliderCount} sliders from {Path} using {Mode}",
            markedObjects.Count(hitObject => hitObject.IsSlider), path, mode);
        double circleSize = session.Beatmap.Difficulty["CircleSize"].DoubleValue;
        return new TumourImportResult(
            markedObjects.Where(hitObject => hitObject.IsSlider).ToArray(),
            circleSize,
            session.Source == BeatmapEditingSource.LiveEditor);
    }

    /// <inheritdoc />
    public async Task<TumourRunResult> RunAsync(
        IReadOnlyList<string> paths,
        TumourGeneratorServiceOptions project,
        bool quickRun,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(project);
        if (paths.Count == 0) throw new ArgumentException("At least one beatmap path is required.", nameof(paths));
        Validate(project);
        logger.LogInformation("Started for {Count} beatmaps; selection {Mode}", paths.Count, project.ImportModeSetting);

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
            logger.LogInformation("Processing beatmap {Index}/{Count}: {Path}", pathIndex + 1, paths.Count, path);
            var session = await editingGateway.OpenBeatmapAsync(
                    path,
                    project.ImportModeSetting == HitObjectSelectionMode.Selected
                        ? LiveBeatmapPreference.RequireLive
                        : LiveBeatmapPreference.PreferLive,
                    cancellationToken)
                .ConfigureAwait(false);
            // Load sliders from the selector
            var markedObjects = BeatmapObjectSelection.Select(
                session,
                project.ImportModeSetting,
                project.TimeCode);
            logger.LogInformation("Selected {SelectedCount} objects in {Path}", markedObjects.Count, path);
            int generatedBeforeMap = generatedCount;
            for (int objectIndex = 0; objectIndex < markedObjects.Count; objectIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Generate copious amounts of tumours on each slider
                if (generator.TumourGenerate(markedObjects[objectIndex], cancellationToken)) generatedCount++;
                progress?.Report((pathIndex + (objectIndex + 1d) / Math.Max(markedObjects.Count, 1)) / paths.Count);
            }

            if (project.FixSv)
            {
                logger.LogInformation("Fixing slider velocity in {Path}", path);
                SliderVelocityFixer.Fix(
                    session.Beatmap,
                    markedObjects,
                    project.DelegateToBpm,
                    project.RemoveSliderTicks,
                    cancellationToken);
            }

            logger.LogInformation("Generated {GeneratedCount} sliders in {Path}; saving",
                generatedCount - generatedBeforeMap, path);

            bool shouldReload = AutomaticEditorReloadPolicy.ShouldReloadEditor(
                session,
                quickRun,
                settings);
            // Save the file
            await editingGateway.SaveAsync(session, shouldReload, cancellationToken).ConfigureAwait(false);
            editorReloaded |= shouldReload;
            completedPaths.Add(path);
            progress?.Report(pathIndex + 1, paths.Count);
        }

        progress?.Report(1);
        logger.LogInformation("Completed {Count} beatmaps with {GeneratedCount} sliders generated",
            completedPaths.Count, generatedCount);
        return new TumourRunResult(completedPaths, generatedCount, editorReloaded);
    }

    private static TumourGeneratorEngine CreateGenerator(TumourGeneratorEngineOptions options)
    {
        return new TumourGeneratorEngine
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
        TumourGeneratorEngine.Validate(project);
    }
}
