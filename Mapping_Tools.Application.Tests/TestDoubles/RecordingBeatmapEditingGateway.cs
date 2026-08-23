using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Application.Tests.TestDoubles;

internal sealed class RecordingBeatmapEditingGateway : IBeatmapEditingGateway
{
    public RecordingBeatmapEditingGateway(BeatmapEditingSession? session = null)
    {
        Session = session;
    }

    public BeatmapEditingSession? Session { get; set; }

    public Func<string, LiveBeatmapPreference, BeatmapEditingSession>? OpenBeatmapFactory { get; set; }

    public Func<string, StoryboardEditor>? OpenStoryboardFactory { get; set; }

    public Action<Editor, bool>? SaveEditorAction { get; set; }

    public Action<BeatmapEditingSession, bool>? SaveSessionAction { get; set; }

    public Exception? OpenBeatmapFailure { get; set; }

    public Exception? OpenStoryboardFailure { get; set; }

    public Exception? SaveFailure { get; set; }

    public List<(string Path, LiveBeatmapPreference Preference)> OpenRequests { get; } = [];

    public List<(string Path, StoryboardEditor Storyboard)> OpenStoryboardRequests { get; } = [];

    public List<(Editor Editor, bool ReloadEditor)> EditorSaveRequests { get; } = [];

    public List<(BeatmapEditingSession Session, bool ReloadEditor)> SessionSaveRequests { get; } = [];

    public List<(Editor Editor, bool ReloadEditor)> CompletedEditorSaveRequests { get; } = [];

    public List<(BeatmapEditingSession Session, bool ReloadEditor)> CompletedSessionSaveRequests { get; } = [];

    public BeatmapEditingSession? LastOpenedSession { get; private set; }

    public StoryboardEditor? LastOpenedStoryboard { get; private set; }

    public Task<BeatmapEditingSession> OpenBeatmapAsync(
        string path,
        LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OpenRequests.Add((path, livePreference));

        if (OpenBeatmapFailure is not null)
        {
            return Task.FromException<BeatmapEditingSession>(OpenBeatmapFailure);
        }

        BeatmapEditingSession result = OpenBeatmapFactory?.Invoke(path, livePreference)
            ?? Session
            ?? throw new NotSupportedException("No beatmap-open behavior was configured.");
        LastOpenedSession = result;
        return Task.FromResult(result);
    }

    public Task<StoryboardEditor> OpenStoryboardAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (OpenStoryboardFailure is not null)
        {
            return Task.FromException<StoryboardEditor>(OpenStoryboardFailure);
        }

        StoryboardEditor result = OpenStoryboardFactory?.Invoke(path)
            ?? throw new NotSupportedException("No storyboard-open behavior was configured.");
        OpenStoryboardRequests.Add((path, result));
        LastOpenedStoryboard = result;
        return Task.FromResult(result);
    }

    public Task SaveAsync(
        Editor editor,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EditorSaveRequests.Add((editor, reloadEditor));

        if (SaveFailure is not null)
        {
            return Task.FromException(SaveFailure);
        }

        SaveEditorAction?.Invoke(editor, reloadEditor);
        CompletedEditorSaveRequests.Add((editor, reloadEditor));
        return Task.CompletedTask;
    }

    public Task SaveAsync(
        BeatmapEditingSession session,
        bool reloadEditor = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SessionSaveRequests.Add((session, reloadEditor));

        if (SaveFailure is not null)
        {
            return Task.FromException(SaveFailure);
        }

        SaveSessionAction?.Invoke(session, reloadEditor);
        CompletedSessionSaveRequests.Add((session, reloadEditor));
        return Task.CompletedTask;
    }
}
