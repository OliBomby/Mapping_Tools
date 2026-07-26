using System.ComponentModel.DataAnnotations;
using Mapping_Tools.ApplicationServices.Execution;
using Mapping_Tools.ApplicationServices.Interactions;
using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Settings;
using Mapping_Tools.Desktop.Platform;
using ReactiveUI;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Edits the first Preferences migration slice and persists each valid change
/// without exposing Avalonia controls or storage-provider objects.
/// </summary>
public sealed class PreferencesViewModel : ViewModelBase
{
    private static readonly FilePickerFilter OsuConfigurationFilter = new(
        "osu! user configuration",
        ["osu!.*.cfg"]);

    private readonly ApplicationSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IFilePicker _filePicker;
    private readonly IApplicationThemeService _themeService;
    private readonly IUserNotificationService _notifications;
    private string _osuPath;
    private string _songsPath;
    private string _osuConfigPath;
    private string _backupsPath;
    private int _maxBackupFiles;
    private TimeSpan _periodicBackupInterval;

    /// <summary>
    /// Creates an editor over the process-lifetime settings document.
    /// </summary>
    /// <param name="settings">The mutable document shared by desktop services.</param>
    /// <param name="settingsService">Persists valid changes.</param>
    /// <param name="filePicker">Presents native folder and configuration-file pickers.</param>
    /// <param name="themeService">Applies palette changes to the live application.</param>
    /// <param name="notifications">Reports picker and persistence failures through the shell.</param>
    public PreferencesViewModel(
        ApplicationSettings settings,
        ISettingsService settingsService,
        IFilePicker filePicker,
        IApplicationThemeService themeService,
        IUserNotificationService notifications)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        _osuPath = settings.OsuPath;
        _songsPath = settings.SongsPath;
        _osuConfigPath = settings.OsuConfigPath;
        _backupsPath = settings.BackupsPath;
        _maxBackupFiles = settings.MaxBackupFiles;
        _periodicBackupInterval = settings.PeriodicBackupInterval;

        BrowseOsuPathCommand = ReactiveCommand.CreateFromTask(
            () => PickFolderAsync(
                "Select the osu! folder",
                OsuPath,
                path => OsuPath = path));
        BrowseSongsPathCommand = ReactiveCommand.CreateFromTask(
            () => PickFolderAsync(
                "Select the osu! Songs folder",
                SongsPath,
                path => SongsPath = path));
        BrowseBackupsPathCommand = ReactiveCommand.CreateFromTask(
            () => PickFolderAsync(
                "Select the Mapping Tools backups folder",
                BackupsPath,
                path => BackupsPath = path));
        BrowseOsuConfigPathCommand = ReactiveCommand.CreateFromTask(PickOsuConfigAsync);
    }

    /// <summary>Gets or edits the directory containing the osu! executable.</summary>
    [RequiredText(ErrorMessage = "Select a path.")]
    public string OsuPath
    {
        get => _osuPath;
        set => SetPath(
            ref _osuPath,
            value,
            path => _settings.OsuPath = path,
            nameof(OsuPath));
    }

    /// <summary>Gets or edits osu!'s beatmap-library directory.</summary>
    [RequiredText(ErrorMessage = "Select a path.")]
    public string SongsPath
    {
        get => _songsPath;
        set => SetPath(
            ref _songsPath,
            value,
            path => _settings.SongsPath = path,
            nameof(SongsPath));
    }

    /// <summary>Gets or edits the current user's osu! configuration file.</summary>
    [RequiredText(ErrorMessage = "Select a path.")]
    public string OsuConfigPath
    {
        get => _osuConfigPath;
        set => SetPath(
            ref _osuConfigPath,
            value,
            path => _settings.OsuConfigPath = path,
            nameof(OsuConfigPath));
    }

    /// <summary>Gets or edits the directory that receives beatmap backups.</summary>
    [RequiredText(ErrorMessage = "Select a path.")]
    public string BackupsPath
    {
        get => _backupsPath;
        set => SetPath(
            ref _backupsPath,
            value,
            path => _settings.BackupsPath = path,
            nameof(BackupsPath));
    }

    /// <summary>Gets or edits the retained-backup limit as a typed count.</summary>
    [Range(
        1,
        100_000,
        ErrorMessage = "Use a whole number from 1 through 100000.")]
    public int MaxBackupFiles
    {
        get => _maxBackupFiles;
        set
        {
            if (_maxBackupFiles == value)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _maxBackupFiles, value);
            if (ValidateProperty(value))
            {
                _settings.MaxBackupFiles = value;
                Persist();
            }
        }
    }

    /// <summary>Gets or edits the periodic-backup interval as a typed duration.</summary>
    [MinimumTimeSpan(
        "00:00:01",
        ErrorMessage = "Use an interval of at least one second.")]
    public TimeSpan PeriodicBackupInterval
    {
        get => _periodicBackupInterval;
        set
        {
            if (_periodicBackupInterval == value)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _periodicBackupInterval, value);
            if (ValidateProperty(value))
            {
                _settings.PeriodicBackupInterval = value;
                Persist();
            }
        }
    }

    /// <summary>Gets or sets whether destructive tools create safety backups.</summary>
    public bool MakeBackups
    {
        get => _settings.MakeBackups;
        set => SetBoolean(
            _settings.MakeBackups,
            value,
            updated => _settings.MakeBackups = updated,
            nameof(MakeBackups));
    }

    /// <summary>Gets or sets whether the background backup timer is enabled.</summary>
    public bool MakePeriodicBackups
    {
        get => _settings.MakePeriodicBackups;
        set => SetBoolean(
            _settings.MakePeriodicBackups,
            value,
            updated => _settings.MakePeriodicBackups = updated,
            nameof(MakePeriodicBackups));
    }

    /// <summary>
    /// Gets or sets whether general file pickers begin beside the current beatmap.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder
    {
        get => _settings.CurrentBeatmapDefaultFolder;
        set => SetBoolean(
            _settings.CurrentBeatmapDefaultFolder,
            value,
            updated => _settings.CurrentBeatmapDefaultFolder = updated,
            nameof(CurrentBeatmapDefaultFolder));
    }

    /// <summary>Gets or sets whether live editor memory may be read.</summary>
    public bool UseEditorReader
    {
        get => _settings.UseEditorReader;
        set => SetBoolean(
            _settings.UseEditorReader,
            value,
            updated => _settings.UseEditorReader = updated,
            nameof(UseEditorReader));
    }

    /// <summary>Gets or sets whether the dark application palette is active.</summary>
    public bool IsDarkTheme
    {
        get => _settings.Theme == ApplicationTheme.Dark;
        set
        {
            ApplicationTheme theme = value
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
            if (_settings.Theme == theme)
            {
                return;
            }

            _settings.Theme = theme;
            this.RaisePropertyChanged();
            _themeService.Apply(theme);
            Persist();
        }
    }

    /// <summary>Gets the native folder-picker command for the osu! directory.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> BrowseOsuPathCommand { get; }

    /// <summary>Gets the native folder-picker command for the Songs directory.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> BrowseSongsPathCommand { get; }

    /// <summary>Gets the native file-picker command for the osu! configuration.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> BrowseOsuConfigPathCommand { get; }

    /// <summary>Gets the native folder-picker command for the backups directory.</summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> BrowseBackupsPathCommand { get; }

    private void SetPath(
        ref string field,
        string? value,
        Action<string> apply,
        string propertyName)
    {
        string normalized = value ?? string.Empty;
        if (field == normalized)
        {
            return;
        }

        this.RaiseAndSetIfChanged(ref field, normalized, propertyName);
        if (ValidateProperty(normalized, propertyName))
        {
            apply(normalized);
            Persist();
        }
    }

    private void SetBoolean(
        bool current,
        bool value,
        Action<bool> apply,
        string propertyName)
    {
        if (current == value)
        {
            return;
        }

        apply(value);
        this.RaisePropertyChanged(propertyName);
        Persist();
    }

    private async Task PickFolderAsync(
        string title,
        string startLocation,
        Action<string> apply)
    {
        try
        {
            IReadOnlyList<string> paths = await _filePicker.PickFoldersAsync(
                new OpenFolderPickerRequest
                {
                    Title = title,
                    SuggestedStartLocation = startLocation,
                    AllowMultiple = false
                });
            if (paths.Count > 0)
            {
                apply(paths[0]);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                "Could not select folder",
                "The folder picker could not return a local path.",
                exception).ConfigureAwait(false);
        }
    }

    private async Task PickOsuConfigAsync()
    {
        try
        {
            IReadOnlyList<string> paths = await _filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = "Select the osu! user configuration file",
                    SuggestedStartLocation = OsuPath,
                    AllowMultiple = false,
                    Filters = [OsuConfigurationFilter]
                });
            if (paths.Count > 0)
            {
                OsuConfigPath = paths[0];
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(
                "Could not select configuration",
                "The file picker could not return a local osu! configuration path.",
                exception).ConfigureAwait(false);
        }
    }

    private void Persist()
    {
        try
        {
            _settingsService.Save(_settings);
        }
        catch (Exception exception)
        {
            _ = PublishFailureAsync(
                "Could not save preferences",
                "The change remains active for this session but could not be written to config.json.",
                exception);
        }
    }

    private Task PublishFailureAsync(
        string title,
        string message,
        Exception exception) =>
        _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));
}
