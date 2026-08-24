using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.RhythmGuide;
using Mapping_Tools.Core.Tools.RhythmGuide.Models;

namespace Mapping_Tools.Application.Tools.RhythmGuide;

/// <summary>Coordinates live-aware loading and backup-before-overwrite persistence for Rhythm Guide.</summary>
public sealed class RhythmGuideService : IRhythmGuideService
{
    private readonly IBeatmapBackupService backupService;
    private readonly IBeatmapEditingGateway editingGateway;
    private readonly IBeatmapFileSystem fileSystem;
    private readonly ITextFileStore textFileStore;

    /// <summary>Creates a service that loads source maps and safely persists guide output.</summary>
    /// <param name="editingGateway">The live-aware, backup-before-write beatmap gateway.</param>
    /// <param name="backupService">Creates preference-respecting copies of every source before it is read.</param>
    /// <param name="fileSystem">Checks whether a destination already exists.</param>
    /// <param name="textFileStore">Writes newly created beatmap documents.</param>
    public RhythmGuideService(
        IBeatmapEditingGateway editingGateway,
        IBeatmapBackupService backupService,
        IBeatmapFileSystem fileSystem,
        ITextFileStore textFileStore)
    {
        this.editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
    }

    /// <inheritdoc />
    public async Task<RhythmGuideResult> GenerateAsync(
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        List<Beatmap> sources = [];
        foreach (string path in options.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = await editingGateway.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            await backupService.CreateAsync(
                source,
                BeatmapBackupReason.Automatic,
                false,
                cancellationToken).ConfigureAwait(false);
            sources.Add(source.Editor.Beatmap);
        }

        if (options.ExportMode == RhythmGuideExportMode.AddToMap)
        {
            var target = await editingGateway.OpenBeatmapAsync(
                options.ExportPath,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            int originalCount = target.Editor.Beatmap.HitObjects.Count;
            RhythmGuideGenerator.Append(
                target.Editor.Beatmap,
                sources,
                options,
                cancellationToken);
            await editingGateway.SaveAsync(
                target,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new RhythmGuideResult(
                options.ExportPath,
                target.Editor.Beatmap.HitObjects.Count - originalCount,
                options.ExportMode);
        }

        var generated = RhythmGuideGenerator.CreateNewMap(
            sources,
            options,
            cancellationToken);
        BeatmapEditor output = new(generated.GetLines(), textFileStore)
        {
            Path = options.ExportPath,
        };
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
        }

        return new RhythmGuideResult(
            options.ExportPath,
            generated.HitObjects.Count,
            options.ExportMode);
    }

    private static void Validate(RhythmGuideOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Paths is null || options.Paths.Length == 0) throw new ArgumentException("Select at least one source beatmap.", nameof(options));
        if (options.Paths.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Source beatmap paths cannot be blank.", nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputName);
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0) throw new ArgumentException("Select at least one beat divisor.", nameof(options));
    }
}
