using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Infrastructure.Editor.Mtipc2;

/// <summary>Proof-of-concept editing gateway that uses complete MTIPC2 documents.</summary>
public sealed class Mtipc2BeatmapEditingGateway : IBeatmapEditingGateway
{
    private readonly Mtipc2TextFileStore store;
    private readonly Mtipc2Client client;

    /// <summary>Creates a gateway over the MTIPC2 file store.</summary>
    public Mtipc2BeatmapEditingGateway(Mtipc2TextFileStore store, Mtipc2Client client)
    {
        this.store = store;
        this.client = client;
    }

    /// <inheritdoc />
    public Task<BeatmapEditingSession> OpenBeatmapAsync(string path,
        LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var beatmap = new Beatmap(store.ReadAllLines(path).ToList());
        Mtipc2EditorState? state = store.TryFileId(path, out string id) ? client.EditorState() : null;
        bool isLive = state is not null && string.Equals(state.FileId, id, StringComparison.OrdinalIgnoreCase);
        if (livePreference == LiveBeatmapPreference.RequireLive && !isLive)
            throw new LiveBeatmapUnavailableException("The requested beatmap is not open in osu!'s editor.");
        var selected = isLive
            ? state!.SelectedIndices.Where(index => index >= 0 && index < beatmap.HitObjects.Count)
                .Select(index => beatmap.HitObjects[index]).ToArray()
            : [];
        return Task.FromResult(new BeatmapEditingSession(beatmap, path, store,
            isLive ? BeatmapEditingSource.LiveEditor : BeatmapEditingSource.Disk,
            selected, liveEditorTime: isLive ? state!.TimeMs : null));
    }

    /// <inheritdoc />
    public Task<StoryboardEditingSession> OpenStoryboardAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new StoryboardEditingSession(path, store));
    }

    /// <inheritdoc />
    public Task SaveAsync(EditingSession session, bool reloadEditor = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        session.SaveFile();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveAsync(BeatmapEditingSession session, bool reloadEditor = false, CancellationToken cancellationToken = default) =>
        SaveAsync((EditingSession)session, reloadEditor, cancellationToken);
}
