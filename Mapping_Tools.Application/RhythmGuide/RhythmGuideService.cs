using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.SafetyCopies;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Tools.RhythmGuide;

namespace Mapping_Tools.Application.RhythmGuide;

/// <summary>Coordinates live-aware loading and backup-before-overwrite persistence for Rhythm Guide.</summary>
public sealed class RhythmGuideService : IRhythmGuideService
{
    private readonly IBeatmapEditingGateway _editingGateway;
    private readonly IBeatmapBackupService _backupService;
    private readonly IBeatmapFileSystem _fileSystem;
    private readonly ITextFileStore _textFileStore;

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
        _editingGateway = editingGateway ?? throw new ArgumentNullException(nameof(editingGateway));
        _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _textFileStore = textFileStore ?? throw new ArgumentNullException(nameof(textFileStore));
    }

    /// <inheritdoc/>
    public async Task<RhythmGuideResult> GenerateAsync(
        RhythmGuideOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        List<Beatmap> sources = [];
        foreach (string path in options.Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditingSession source = await _editingGateway.OpenBeatmapAsync(
                path,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            await _backupService.CreateAsync(
                source,
                BeatmapBackupReason.Automatic,
                force: false,
                cancellationToken).ConfigureAwait(false);
            sources.Add(source.Editor.Beatmap);
        }

        if (options.ExportMode == RhythmGuideExportMode.AddToMap)
        {
            BeatmapEditingSession target = await _editingGateway.OpenBeatmapAsync(
                options.ExportPath,
                LiveBeatmapPreference.PreferLive,
                cancellationToken).ConfigureAwait(false);
            int originalCount = target.Editor.Beatmap.HitObjects.Count;
            RhythmGuideGenerator.Append(
                target.Editor.Beatmap,
                sources,
                options,
                cancellationToken);
            await _editingGateway.SaveAsync(
                target,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return new RhythmGuideResult(
                options.ExportPath,
                target.Editor.Beatmap.HitObjects.Count - originalCount,
                options.ExportMode);
        }

        Beatmap generated = RhythmGuideGenerator.CreateNewMap(
            sources,
            options,
            cancellationToken);
        BeatmapEditor2 output = new(generated.GetLines(), _textFileStore)
        {
            Path = options.ExportPath
        };
        if (_fileSystem.FileExists(options.ExportPath))
        {
            await _editingGateway.SaveAsync(
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
        if (options.Paths is null || options.Paths.Length == 0)
        {
            throw new ArgumentException("Select at least one source beatmap.", nameof(options));
        }
        if (options.Paths.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Source beatmap paths cannot be blank.", nameof(options));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ExportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.OutputName);
        if (options.BeatDivisors is null || options.BeatDivisors.Length == 0)
        {
            throw new ArgumentException("Select at least one beat divisor.", nameof(options));
        }
    }
}
