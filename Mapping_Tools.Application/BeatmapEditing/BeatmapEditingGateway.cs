using System.Globalization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Contracts;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.BeatmapEditing;

/// <summary>
///     Builds editable documents from disk and deliberately overlays live state
///     only when Editor Reader identifies the exact same beatmap.
/// </summary>
public sealed class BeatmapEditingGateway : IBeatmapEditingGateway
{
    private readonly IBeatmapBackupService backupService;
    private readonly ITextFileStore fileStore;
    private readonly ILiveBeatmapReader liveReader;
    private readonly IEditorReloadService reloadService;
    private readonly ApplicationSettings settings;

    /// <summary>
    ///     Creates the application service that arbitrates between durable files
    ///     and the newer, potentially unsaved state held by osu!.
    /// </summary>
    /// <param name="fileStore">Persistence used by every returned document editor.</param>
    /// <param name="backupService">
    ///     Creates the durable pre-save snapshot that must succeed before an existing document is overwritten.
    /// </param>
    /// <param name="liveReader">The platform adapter that reads osu!'s editor memory.</param>
    /// <param name="reloadService">The platform adapter that refreshes osu! after a save.</param>
    /// <param name="settings">The current preference controlling Editor Reader use.</param>
    public BeatmapEditingGateway(
        ITextFileStore fileStore,
        IBeatmapBackupService backupService,
        ILiveBeatmapReader liveReader,
        IEditorReloadService reloadService,
        ApplicationSettings settings)
    {
        this.fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        this.liveReader = liveReader ?? throw new ArgumentNullException(nameof(liveReader));
        this.reloadService = reloadService ?? throw new ArgumentNullException(nameof(reloadService));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public async Task<BeatmapEditingSession> OpenBeatmapAsync(
        string path,
        LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        BeatmapEditor diskEditor = new(path, fileStore);
        if (livePreference == LiveBeatmapPreference.DiskOnly) return DiskSession(diskEditor);

        if (!settings.UseEditorReader)
            return livePreference == LiveBeatmapPreference.RequireLive
                ? throw new LiveBeatmapUnavailableException(
                    "Live editor state is disabled in Mapping Tools settings.")
                : DiskSession(diskEditor);

        try
        {
            var snapshot = await liveReader
                .ReadAsync(cancellationToken)
                .ConfigureAwait(false);

            if (snapshot is null)
                return livePreference == LiveBeatmapPreference.RequireLive
                    ? throw new LiveBeatmapUnavailableException(
                        "No active osu! beatmap editor could be read.")
                    : DiskSession(diskEditor);

            if (!string.Equals(snapshot.Path, path, StringComparison.Ordinal))
                return livePreference == LiveBeatmapPreference.RequireLive
                    ? throw new LiveBeatmapUnavailableException(
                        $"osu! is editing '{snapshot.Path}', not the requested beatmap '{path}'.")
                    : DiskSession(diskEditor);

            var selected = ApplyLiveState(diskEditor.Beatmap, snapshot);
            return new BeatmapEditingSession(
                diskEditor,
                BeatmapEditingSource.LiveEditor,
                selected,
                liveEditorTime: snapshot.EditorTime);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (LiveBeatmapUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (livePreference == LiveBeatmapPreference.PreferLive)
        {
            return DiskSession(diskEditor, exception);
        }
        catch (Exception exception)
        {
            throw new LiveBeatmapUnavailableException(
                "osu! editor state could not be read safely.",
                exception);
        }
    }

    /// <inheritdoc />
    public Task<StoryboardEditor> OpenStoryboardAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new StoryboardEditor(path, fileStore));
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        Editor editor,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(editor);
        await SaveCoreAsync(editor, null, reloadEditor, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        BeatmapEditingSession session,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        await SaveCoreAsync(session.Editor, session, reloadEditor, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SaveCoreAsync(
        Editor editor,
        BeatmapEditingSession? session,
        bool reloadEditor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (session is null)
            await backupService.CreateAsync(
                    [editor.Path],
                    BeatmapBackupReason.Automatic,
                    true,
                    cancellationToken)
                .ConfigureAwait(false);
        else
            await backupService.CreateAsync(
                    session,
                    BeatmapBackupReason.Automatic,
                    true,
                    cancellationToken)
                .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        editor.SaveFile();

        if (reloadEditor)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await reloadService.ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static BeatmapEditingSession DiskSession(
        BeatmapEditor editor,
        Exception? liveReadFailure = null)
    {
        return new BeatmapEditingSession(
            editor,
            BeatmapEditingSource.Disk,
            [],
            liveReadFailure);
    }

    private static IReadOnlyList<HitObject> ApplyLiveState(
        Beatmap beatmap,
        LiveBeatmapSnapshot snapshot)
    {
        beatmap.SetBookmarks(snapshot.Bookmarks.ToList());
        beatmap.BeatmapTiming.SetTimingPoints(snapshot.TimingPoints.ToList());
        beatmap.HitObjects = snapshot.HitObjects.ToList();

        beatmap.General["PreviewTime"] =
            new StringValue(snapshot.PreviewTime.ToString(CultureInfo.InvariantCulture));
        beatmap.Difficulty["SliderMultiplier"] =
            new StringValue(snapshot.SliderMultiplier.ToString(CultureInfo.InvariantCulture));
        beatmap.Difficulty["SliderTickRate"] =
            new StringValue(snapshot.SliderTickRate.ToString(CultureInfo.InvariantCulture));
        beatmap.BeatmapTiming.SliderMultiplier = snapshot.SliderMultiplier;

        beatmap.HitObjects = beatmap.HitObjects.OrderBy(hitObject => hitObject.Time).ToList();
        beatmap.CalculateHitObjectComboStuff();
        beatmap.CalculateSliderEndTimes();
        beatmap.GiveObjectsGreenlines();

        var selected = snapshot.SelectedHitObjects.ToHashSet();
        return beatmap.HitObjects.Where(selected.Contains).ToList();
    }
}
