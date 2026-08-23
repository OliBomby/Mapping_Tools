using System.Globalization;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Workspace;

/// <summary>
///     Coordinates selected paths, persisted recent history, native file picking,
///     and live osu! lookup without relying on a window or view model.
/// </summary>
public sealed class BeatmapWorkspace : IBeatmapWorkspace
{
    private const int RecentMapLimit = 20;
    private readonly ICurrentBeatmapLocator _currentBeatmapLocator;
    private readonly IFilePicker _filePicker;
    private readonly IBeatmapFileSystem _fileSystem;

    private readonly ApplicationSettings _settings;
    private readonly TimeProvider _timeProvider;
    private string[] _selectedPaths = [];

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
        IBeatmapFileSystem fileSystem,
        ICurrentBeatmapLocator currentBeatmapLocator,
        TimeProvider timeProvider)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _currentBeatmapLocator = currentBeatmapLocator
                                 ?? throw new ArgumentNullException(nameof(currentBeatmapLocator));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _settings.RecentMaps ??= [];
        RemoveInvalidRecentEntries();
    }

    /// <inheritdoc />
    public event EventHandler<BeatmapSelectionChangedEventArgs>? SelectionChanged;

    /// <inheritdoc />
    public IReadOnlyList<string> SelectedPaths => _selectedPaths.ToArray();

    /// <inheritdoc />
    public IReadOnlyList<RecentBeatmap> RecentMaps => _settings.RecentMaps.ToArray();

    /// <inheritdoc />
    public bool RestoreMostRecent()
    {
        var recent = _settings.RecentMaps.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Path));
        if (recent is null) return false;

        SetSelection(
            recent.Path.Split('|', StringSplitOptions.RemoveEmptyEntries),
            BeatmapSelectionSource.Startup);
        return _selectedPaths.Length > 0;
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
        _selectedPaths = selection;

        string displayDate = _timeProvider
            .GetLocalNow()
            .DateTime
            .ToString(CultureInfo.CurrentCulture);
        foreach (string path in selection)
        {
            _settings.RecentMaps.RemoveAll(recent => string.Equals(recent.Path, path, StringComparison.Ordinal));
            if (_settings.RecentMaps.Count >= RecentMapLimit) _settings.RecentMaps.RemoveAt(_settings.RecentMaps.Count - 1);

            _settings.RecentMaps.Insert(0, new RecentBeatmap(path, displayDate));
        }

        PublishSelection(source);
    }

    /// <inheritdoc />
    public void ClearSelection(
        BeatmapSelectionSource source = BeatmapSelectionSource.Programmatic)
    {
        _selectedPaths = [];
        PublishSelection(source);
    }

    /// <inheritdoc />
    public bool RemoveRecent(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _settings.RecentMaps.RemoveAll(recent => string.Equals(recent.Path, path, StringComparison.Ordinal)) > 0;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetMissingSelectedPaths()
    {
        return _selectedPaths
            .Where(path => !_fileSystem.FileExists(path))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> PickBeatmapsAsync(
        bool allowMultiple,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paths = await _filePicker.PickOpenFilesAsync(
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
        string? path = await _currentBeatmapLocator.FindCurrentBeatmapAsync(
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path))
            return new CurrentBeatmapSelectionResult(
                CurrentBeatmapSelectionStatus.Unavailable,
                null);

        if (!_fileSystem.FileExists(path))
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
        if (!_settings.CurrentBeatmapDefaultFolder) return null;

        string? selectedParent = _selectedPaths.Length == 0
            ? null
            : _fileSystem.GetParentDirectory(_selectedPaths[0]);
        return string.IsNullOrWhiteSpace(selectedParent)
            ? _settings.SongsPath
            : selectedParent;
    }

    private void RemoveInvalidRecentEntries()
    {
        _settings.RecentMaps.RemoveAll(recent => recent is null || string.IsNullOrWhiteSpace(recent.Path) || recent.DisplayDate is null);
        while (_settings.RecentMaps.Count > RecentMapLimit) _settings.RecentMaps.RemoveAt(_settings.RecentMaps.Count - 1);
    }

    private void PublishSelection(BeatmapSelectionSource source)
    {
        SelectionChanged?.Invoke(
            this,
            new BeatmapSelectionChangedEventArgs(
                _selectedPaths.ToArray(),
                source));
    }
}
