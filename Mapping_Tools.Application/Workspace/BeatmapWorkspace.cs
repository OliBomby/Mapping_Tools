using System.Globalization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Workspace.Models;

namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Coordinates selected paths, persisted recent history, native file picking,
///     and live osu! lookup without relying on a window or view model.
/// </summary>
public sealed class BeatmapWorkspace : IBeatmapWorkspace
{
    private const int recent_map_limit = 20;
    private readonly ICurrentBeatmapLocator currentBeatmapLocator;
    private readonly IFilePicker filePicker;
    private readonly IBeatmapsetFileSystem fileSystem;

    private readonly ApplicationSettings settings;
    private readonly TimeProvider timeProvider;
    private string[] selectedPaths = [];

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
    public BeatmapWorkspace(
        ApplicationSettings settings,
        IFilePicker filePicker,
        IBeatmapsetFileSystem fileSystem,
        ICurrentBeatmapLocator currentBeatmapLocator,
        TimeProvider timeProvider)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        this.currentBeatmapLocator = currentBeatmapLocator
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.settings.RecentMaps ??= [];
        RemoveInvalidRecentEntries();
    }

    /// <inheritdoc />
    public event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

    /// <inheritdoc />
    public IReadOnlyList<string> SelectedPaths => selectedPaths.ToArray();

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

        string displayDate = timeProvider
            .GetLocalNow()
            .DateTime
            .ToString(CultureInfo.CurrentCulture);
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
                SuggestedStartLocation = GetPickerStartLocation(),
                AllowMultiple = allowMultiple,
                Filters = [CommonFilePickerFilters.BeatmapsAndStoryboards],
            },
            cancellationToken);

        if (paths.Count == 0) return false;

        SetSelection(paths, BeatmapSelectionSource.FilePicker);
        return true;
    }

    /// <inheritdoc />
    public async Task<CurrentBeatmapSelectionResult> SelectCurrentBeatmapAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path;
        try
        {
            path = await currentBeatmapLocator.FindCurrentBeatmapAsync(
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new CurrentBeatmapSelectionResult(
                CurrentBeatmapSelectionStatus.Unavailable,
                null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!fileSystem.FileExists(path))
            return new CurrentBeatmapSelectionResult(
                CurrentBeatmapSelectionStatus.FileMissing,
                path);

        SetSelection([path], BeatmapSelectionSource.CurrentEditor);
        return new CurrentBeatmapSelectionResult(
            CurrentBeatmapSelectionStatus.Selected,
            path);
    }

    private string? GetPickerStartLocation()
    {
        if (!settings.CurrentBeatmapDefaultFolder) return null;

        string? selectedParent = selectedPaths.Length == 0
            ? null
            : fileSystem.GetParentDirectory(selectedPaths[0]);
        return string.IsNullOrWhiteSpace(selectedParent)
            ? settings.SongsPath
            : selectedParent;
    }

    private void RemoveInvalidRecentEntries()
    {
        settings.RecentMaps.RemoveAll(recent => recent is null || string.IsNullOrWhiteSpace(recent.Path) || recent.DisplayDate is null);
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
