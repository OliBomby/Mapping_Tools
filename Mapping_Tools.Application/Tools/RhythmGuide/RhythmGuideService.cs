using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Coordinates live-aware loading and backup-before-overwrite persistence for Rhythm Guide.</summary>
public sealed class RhythmGuideService : IRhythmGuideService
{
    private readonly IBeatmapBackupService backupService;
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IBeatmapsetFileSystem fileSystem;
    private readonly ITextFileStore textFileStore;
    private readonly IBeatmapDecoder beatmapDecoder;
    private readonly IBeatmapEncoder beatmapEncoder;
    private readonly ILogger<RhythmGuideService> logger;

    /// <summary>Creates a service that loads source maps and safely persists guide output.</summary>
    /// <param name="editingGateway">The live-aware, backup-before-write beatmap gateway.</param>
    /// <param name="backupService">Creates preference-respecting copies of every source before it is read.</param>
    /// <param name="fileSystem">Checks whether a destination already exists.</param>
    /// <param name="textFileStore">Writes newly created beatmap documents.</param>
    /// <param name="beatmapDecoder">Decodes normalized source beatmaps for output generation.</param>
    /// <param name="beatmapEncoder">Encodes normalized source beatmaps and saves output.</param>
    /// <param name="logger">Records source loading, generation, and output milestones.</param>
    public RhythmGuideService(
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService,
        IBeatmapsetFileSystem fileSystem,
        ITextFileStore textFileStore,
        IBeatmapDecoder beatmapDecoder,
        IBeatmapEncoder beatmapEncoder,
        ILogger<RhythmGuideService>? logger = null)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
        this.beatmapDecoder = beatmapDecoder ?? throw new ArgumentNullException(nameof(beatmapDecoder));
        this.beatmapEncoder = beatmapEncoder ?? throw new ArgumentNullException(nameof(beatmapEncoder));
        this.logger = logger ?? NullLogger<RhythmGuideService>.Instance;
    }

    /// <inheritdoc />
    public async Task<RhythmGuideResult> GenerateAsync(
        RhythmGuideServiceOptions.RhythmGuideRunOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        logger.LogInformation("Started with {SourceCount} source beatmaps; mode {Mode}; output {OutputPath}",
            options.Paths.Length, options.ExportMode, options.ExportPath);
        List<Beatmap> sources = [];
        foreach (string path in options.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Loading source {Path}", path);
            var source = await editingGateway.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            await backupService.CreateAsync(
                source,
                BeatmapBackupReason.Automatic,
                false,
                cancellationToken).ConfigureAwait(false);
            sources.Add(source.Beatmap);
        }
        logger.LogInformation("Loaded and backed up {SourceCount} source beatmaps", sources.Count);

        if (options.ExportMode == RhythmGuideExportMode.AddToMap)
        {
            var target = await editingGateway.OpenBeatmapAsync(
                options.ExportPath,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            int originalCount = target.Beatmap.HitObjects.Count;
            RhythmGuideGenerator.Append(
                target.Beatmap,
                sources,
                options,
                cancellationToken);
            logger.LogInformation("Appended {ObjectCount} objects to {Path}; saving",
                target.Beatmap.HitObjects.Count - originalCount, options.ExportPath);
            await editingGateway.SaveAsync(
                target,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new RhythmGuideResult(
                options.ExportPath,
                target.Beatmap.HitObjects.Count - originalCount,
                options.ExportMode);
        }

        var generated = RhythmGuideGenerator.CreateNewMap(
            sources,
            options,
            beatmapDecoder,
            beatmapEncoder,
            cancellationToken);
        logger.LogInformation("Generated {ObjectCount} objects for new map {Path}", generated.HitObjects.Count, options.ExportPath);
        BeatmapEditingSession output = new(
            generated,
            options.ExportPath,
            textFileStore,
            beatmapEncoder,
            BeatmapEditingSource.Disk,
            []);
        if (fileSystem.FileExists(options.ExportPath))
        {
            await editingGateway.SaveAsync(
                output,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.SaveFile();
            logger.LogInformation("Created new beatmap {Path}", options.ExportPath);
        }

        return new RhythmGuideResult(
            options.ExportPath,
            generated.HitObjects.Count,
            options.ExportMode);
    }

    private static void Validate(RhythmGuideServiceOptions.RhythmGuideRunOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Paths is null || options.Paths.Length == 0) throw new ArgumentException(ApplicationStrings.RhythmGuide_SourceBeatmapRequired, nameof(options));
        if (options.Paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(ApplicationStrings.RhythmGuide_SourceBeatmapPathsCannotBeBlank, nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExportPath);
        if (!Enum.IsDefined(options.ExportMode)) throw new ArgumentException(ApplicationStrings.RhythmGuide_UnknownExportMode, nameof(options));
        RhythmGuideGenerator.Validate(options);
    }
}
