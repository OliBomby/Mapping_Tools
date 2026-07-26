using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Platform;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Edits the process-lifetime settings document and applies live-only side
/// effects without exposing Avalonia controls or storage-provider objects.
/// </summary>
public sealed class PreferencesViewModel : ObservableValidator
{
    private static readonly FilePickerFilter OsuConfigurationFilter = new(
        "osu! user configuration",
        ["osu!.*.cfg"]);

    private readonly ApplicationSettings _settings;
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
    /// <param name="filePicker">Presents native folder and configuration-file pickers.</param>
    /// <param name="themeService">Applies palette changes to the live application.</param>
    /// <param name="notifications">Reports picker failures through the shell.</param>
    public PreferencesViewModel(
        ApplicationSettings settings,
        IFilePicker filePicker,
        IApplicationThemeService themeService,
        IUserNotificationService notifications)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        _osuPath = settings.OsuPath;
        _songsPath = settings.SongsPath;
        _osuConfigPath = settings.OsuConfigPath;
        _backupsPath = settings.BackupsPath;
        _maxBackupFiles = settings.MaxBackupFiles;
        _periodicBackupInterval = settings.PeriodicBackupInterval;

        BrowseOsuPathCommand = new AsyncRelayCommand(
            () => PickFolderAsync(
                "Select the osu! folder",
                OsuPath,
                path => OsuPath = path));
        BrowseSongsPathCommand = new AsyncRelayCommand(
            () => PickFolderAsync(
                "Select the osu! Songs folder",
                SongsPath,
                path => SongsPath = path));
        BrowseBackupsPathCommand = new AsyncRelayCommand(
            () => PickFolderAsync(
                "Select the Mapping Tools backups folder",
                BackupsPath,
                path => BackupsPath = path));
        BrowseOsuConfigPathCommand = new AsyncRelayCommand(PickOsuConfigAsync);
    }

    /// <summary>Gets or edits the directory containing the osu! executable.</summary>
    [Required(ErrorMessage = "Select a path.")]
    public string OsuPath
    {
        get => _osuPath;
        set => SetValidatedProperty(
            ref _osuPath,
            value ?? string.Empty,
            static (settings, path) => settings.OsuPath = path);
    }

    /// <summary>Gets or edits osu!'s beatmap-library directory.</summary>
    [Required(ErrorMessage = "Select a path.")]
    public string SongsPath
    {
        get => _songsPath;
        set => SetValidatedProperty(
            ref _songsPath,
            value ?? string.Empty,
            static (settings, path) => settings.SongsPath = path);
    }

    /// <summary>Gets or edits the current user's osu! configuration file.</summary>
    [Required(ErrorMessage = "Select a path.")]
    public string OsuConfigPath
    {
        get => _osuConfigPath;
        set => SetValidatedProperty(
            ref _osuConfigPath,
            value ?? string.Empty,
            static (settings, path) => settings.OsuConfigPath = path);
    }

    /// <summary>Gets or edits the directory that receives beatmap backups.</summary>
    [Required(ErrorMessage = "Select a path.")]
    public string BackupsPath
    {
        get => _backupsPath;
        set => SetValidatedProperty(
            ref _backupsPath,
            value ?? string.Empty,
            static (settings, path) => settings.BackupsPath = path);
    }

    /// <summary>Gets or edits the retained-backup limit as a typed count.</summary>
    [Range(
        1,
        100_000,
        ErrorMessage = "Use a whole number from 1 through 100000.")]
    public int MaxBackupFiles
    {
        get => _maxBackupFiles;
        set => SetValidatedProperty(
            ref _maxBackupFiles,
            value,
            static (settings, count) => settings.MaxBackupFiles = count);
    }

    /// <summary>Gets or edits the periodic-backup interval as a typed duration.</summary>
    [MinimumTimeSpan(
        "00:00:01",
        ErrorMessage = "Use an interval of at least one second.")]
    public TimeSpan PeriodicBackupInterval
    {
        get => _periodicBackupInterval;
        set => SetValidatedProperty(
            ref _periodicBackupInterval,
            value,
            static (settings, interval) =>
                settings.PeriodicBackupInterval = interval);
    }

    /// <summary>Gets or sets whether destructive tools create safety backups.</summary>
    public bool MakeBackups
    {
        get => _settings.MakeBackups;
        set => SetProperty(
            _settings.MakeBackups,
            value,
            _settings,
            static (settings, enabled) => settings.MakeBackups = enabled,
            validate: false);
    }

    /// <summary>Gets or sets whether the background backup timer is enabled.</summary>
    public bool MakePeriodicBackups
    {
        get => _settings.MakePeriodicBackups;
        set => SetProperty(
            _settings.MakePeriodicBackups,
            value,
            _settings,
            static (settings, enabled) =>
                settings.MakePeriodicBackups = enabled,
            validate: false);
    }

    /// <summary>
    /// Gets or sets whether general file pickers begin beside the current beatmap.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder
    {
        get => _settings.CurrentBeatmapDefaultFolder;
        set => SetProperty(
            _settings.CurrentBeatmapDefaultFolder,
            value,
            _settings,
            static (settings, enabled) =>
                settings.CurrentBeatmapDefaultFolder = enabled,
            validate: false);
    }

    /// <summary>Gets or sets whether live editor memory may be read.</summary>
    public bool UseEditorReader
    {
        get => _settings.UseEditorReader;
        set => SetProperty(
            _settings.UseEditorReader,
            value,
            _settings,
            static (settings, enabled) =>
                settings.UseEditorReader = enabled,
            validate: false);
    }

    /// <summary>Gets or sets the palette applied immediately to the live application.</summary>
    public ApplicationTheme Theme
    {
        get => _settings.Theme;
        set
        {
            if (SetProperty(
                    _settings.Theme,
                    value,
                    _settings,
                    static (settings, theme) => settings.Theme = theme,
                    validate: false))
            {
                _themeService.Apply(value);
            }
        }
    }

    /// <summary>Gets the native folder-picker command for the osu! directory.</summary>
    public IAsyncRelayCommand BrowseOsuPathCommand { get; }

    /// <summary>Gets the native folder-picker command for the Songs directory.</summary>
    public IAsyncRelayCommand BrowseSongsPathCommand { get; }

    /// <summary>Gets the native file-picker command for the osu! configuration.</summary>
    public IAsyncRelayCommand BrowseOsuConfigPathCommand { get; }

    /// <summary>Gets the native folder-picker command for the backups directory.</summary>
    public IAsyncRelayCommand BrowseBackupsPathCommand { get; }

    private void SetValidatedProperty<T>(
        ref T field,
        T value,
        Action<ApplicationSettings, T> apply,
        [CallerMemberName] string propertyName = "")
    {
        if (SetProperty(
                ref field,
                value,
                validate: true,
                propertyName: propertyName)
            && PropertyIsValid(propertyName))
        {
            apply(_settings, value);
        }
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

    private bool PropertyIsValid(string propertyName) =>
        !GetErrors(propertyName).Cast<object>().Any();

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
