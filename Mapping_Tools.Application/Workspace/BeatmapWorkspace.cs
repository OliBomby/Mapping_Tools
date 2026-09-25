using System.Globalization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;

namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Coordinates selected paths, persisted recent history, and native file
///     picking without relying on a window or view model.
/// </summary>
public sealed class BeatmapWorkspace : IBeatmapWorkspace
{
    private const string missing_selected_path_message =
        "It seems like one of the selected beatmaps does not exist. Please re-select the file with 'File > Open beatmap'.";
    private const int recent_map_limit = 20;
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IFilePicker filePicker;
    private readonly IBeatmapsetFileSystem fileSystem;
    private readonly IUserNotificationService notifications;

    private readonly ApplicationSettings settings;
    private readonly TimeProvider timeProvider;
    private string[] selectedPaths = [];
    private bool selectedFromExternalEdit;

    /// <summary>
    ///     Creates workspace state backed by the same settings instance that will
    ///     be persisted when the application exits.
    /// </summary>
    /// <param name="settings">Owns typed recent history and picker path preferences.</param>
    /// <param name="filePicker">Presents the shared beatmap file dialog.</param>
    /// <param name="fileSystem">Checks selections and derives picker start folders.</param>
    /// <param name="currentBeatmapLocator">
    ///     Supplies the current path through the same live-reader boundary used
    ///     by editing sessions, without leaking process-memory types here.
    /// </param>
    /// <param name="timeProvider">Supplies deterministic timestamps for recent history.</param>
    /// <param name="notifications">Publishes warnings when a selected path is read after its file disappears.</param>
    public BeatmapWorkspace(
        ApplicationSettings settings,
        IFilePicker filePicker,
        IBeatmapsetFileSystem fileSystem,
        ICurrentBeatmapLocator currentBeatmapLocator,
        TimeProvider timeProvider,
        IUserNotificationService notifications)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        RemoveInvalidRecentEntries();
    }

    /// <inheritdoc />
    public event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

    /// <inheritdoc />
    public IReadOnlyList<string> SelectedPaths
    {
        get
        {
            IReadOnlyList<string> paths = selectedPaths.ToArray();
            IReadOnlyList<string> missing = GetMissingSelectedPaths();
            if (missing.Count > 0 && !selectedFromExternalEdit)
            {
                _ = notifications.PublishAsync(
                    new UserNotification(
                        UserNotificationSeverity.Warning,
                        "Selected beatmap is missing",
                        missing_selected_path_message));
            }

            return paths;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RecentBeatmap> RecentMaps => settings.RecentMaps.ToArray();

    /// <inheritdoc />
    public bool RestoreMostRecent()
    {
        var recent = settings.RecentMaps.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Path));
        if (recent is null) return false;

        SetSelection(
            recent.Path.Split('|', StringSplitOptions.RemoveEmptyEntries),
            BeatmapSelectionSource.Startup);
        return selectedPaths.Length > 0;
    }

    /// <inheritdoc />
    public void SetSelection(
        IEnumerable<string> paths,
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
    {
        ArgumentNullException.ThrowIfNull(paths);
        string[] selection = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
        selectedPaths = selection;
        selectedFromExternalEdit = source == BeatmapSelectionSource.LazerExternalEdit;

        string displayDate = timeProvider
            .GetLocalNow()
            .DateTime
            .ToString(CultureInfo.CurrentCulture);
        if (source != BeatmapSelectionSource.LazerExternalEdit)
            foreach (string path in selection)
            {
                settings.RecentMaps.RemoveAll(recent => string.Equals(recent.Path, path, StringComparison.Ordinal));
                if (settings.RecentMaps.Count >= recent_map_limit) settings.RecentMaps.RemoveAt(settings.RecentMaps.Count - 1);

                settings.RecentMaps.Insert(0, new RecentBeatmap(path, displayDate));
            }

        PublishSelection(source);
    }

    /// <inheritdoc />
    public void ClearSelection(
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
    {
        selectedPaths = [];
        selectedFromExternalEdit = false;
        PublishSelection(source);
    }

    /// <inheritdoc />
    public bool RemoveRecent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return settings.RecentMaps.RemoveAll(recent => string.Equals(recent.Path, path, StringComparison.Ordinal)) > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMissingSelectedPaths()
    {
        return selectedPaths
            .Where(path => !fileSystem.FileExists(path))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> PickBeatmapsAsync(
        bool allowMultiple,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Open beatmap",
                SuggestedStartLocation = GetBeatmapPickerStartLocation(),
                AllowMultiple = allowMultiple,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
            },
            cancellationToken);

        if (paths.Count == 0) return false;

        SetSelection(paths, BeatmapSelectionSource.FilePicker);
        return true;
    }

    /// <inheritdoc />
    public string? GetBeatmapPickerStartLocation(string? currentDirectory = null)
    {
        if (!settings.CurrentBeatmapDefaultFolder) return currentDirectory;

        IReadOnlyList<string> paths = SelectedPaths;
        string? selectedParent = paths.Count == 0
            ? null
            : fileSystem.GetParentDirectory(paths[0]);
        return string.IsNullOrWhiteSpace(selectedParent)
            ? settings.SongsPath
            : selectedParent;
    }

    /// <inheritdoc />
    public async Task<string> ResolveQuickRunBeatmapAsync(
        bool updateSelection = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string? currentPath = null;
        bool hasCurrentBeatmap = false;
        try
        {
            currentPath = await currentBeatmapLocator
                .FindCurrentBeatmapAsync(cancellationToken)
                .ConfigureAwait(false);
            hasCurrentBeatmap = !string.IsNullOrWhiteSpace(currentPath)
                                && fileSystem.FileExists(currentPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // QuickRun intentionally falls back to the shell selection when osu!
            // is closed or the live reader cannot resolve a current map.
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (hasCurrentBeatmap)
        {
            if (updateSelection) SetSelection([currentPath!], BeatmapSelectionSource.CurrentEditor);

            return currentPath!;
        }

        return SelectedPaths.FirstOrDefault() ?? string.Empty;
    }

    private void RemoveInvalidRecentEntries()
    {
        settings.RecentMaps.RemoveAll(recent => string.IsNullOrWhiteSpace(recent.Path));
        while (settings.RecentMaps.Count > recent_map_limit) settings.RecentMaps.RemoveAt(settings.RecentMaps.Count - 1);
    }

    private void PublishSelection(BeatmapSelectionSource source)
    {
        SelectionChanged?.Invoke(
            this,
            new BeatmapSelectionChangedEventArgs(
                selectedPaths.ToArray(),
                source));
    }
}
