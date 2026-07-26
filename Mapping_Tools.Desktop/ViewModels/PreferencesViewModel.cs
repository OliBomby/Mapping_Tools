using System.Collections;
using System.ComponentModel;
using System.Globalization;
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
public sealed class PreferencesViewModel : ViewModelBase, INotifyDataErrorInfo
{
    private static readonly IValueValidator<string> RequiredPath =
        ValueValidators.RequiredText("Select a path.");
    private static readonly IValueValidator<int> BackupCount =
        ValueValidators.InclusiveRange(
            1,
            100_000,
            "Use a whole number from 1 through 100000.");
    private static readonly FilePickerFilter OsuConfigurationFilter = new(
        "osu! user configuration",
        ["osu!.*.cfg"]);

    private readonly ApplicationSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly IFilePicker _filePicker;
    private readonly IApplicationThemeService _themeService;
    private readonly IUserNotificationService _notifications;
    private readonly Dictionary<string, string> _validationErrors =
        new(StringComparer.Ordinal);
    private string _osuPath;
    private string _songsPath;
    private string _osuConfigPath;
    private string _backupsPath;
    private string _maxBackupFilesText;
    private string _periodicBackupIntervalText;
    /// <summary>
    /// Notifies Avalonia when a property-level correction is added, replaced,
    /// or cleared so the owning input control can update its validation state.
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

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
        _maxBackupFilesText = settings.MaxBackupFiles.ToString(CultureInfo.InvariantCulture);
        _periodicBackupIntervalText = settings.PeriodicBackupInterval.ToString("c", CultureInfo.InvariantCulture);

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
    public string BackupsPath
    {
        get => _backupsPath;
        set => SetPath(
            ref _backupsPath,
            value,
            path => _settings.BackupsPath = path,
            nameof(BackupsPath));
    }

    /// <summary>Gets editable invariant text for the retained-backup limit.</summary>
    public string MaxBackupFilesText
    {
        get => _maxBackupFilesText;
        set
        {
            string normalized = value ?? string.Empty;
            if (_maxBackupFilesText == normalized)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _maxBackupFilesText, normalized);
            if (!TextValueConverters.InvariantInt32.TryConvert(
                    normalized,
                    out int count,
                    out string? conversionError))
            {
                SetValidationError(
                    nameof(MaxBackupFilesText),
                    conversionError);
                return;
            }

            ValidationOutcome outcome = BackupCount.Validate(count);
            SetValidationError(
                nameof(MaxBackupFilesText),
                outcome.ErrorMessage);
            if (outcome.IsValid)
            {
                _settings.MaxBackupFiles = count;
                Persist();
            }
        }
    }

    /// <summary>Gets editable constant-format text for the periodic-backup interval.</summary>
    public string PeriodicBackupIntervalText
    {
        get => _periodicBackupIntervalText;
        set
        {
            string normalized = value ?? string.Empty;
            if (_periodicBackupIntervalText == normalized)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _periodicBackupIntervalText, normalized);
            bool parsed = TimeSpan.TryParseExact(
                normalized,
                "c",
                CultureInfo.InvariantCulture,
                out TimeSpan interval);
            if (!parsed)
            {
                SetValidationError(
                    nameof(PeriodicBackupIntervalText),
                    "Use the format hh:mm:ss, for example 00:10:00.");
                return;
            }

            if (interval < TimeSpan.FromSeconds(1))
            {
                SetValidationError(
                    nameof(PeriodicBackupIntervalText),
                    "Use an interval of at least one second.");
                return;
            }

            SetValidationError(nameof(PeriodicBackupIntervalText), null);
            _settings.PeriodicBackupInterval = interval;
            Persist();
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

    /// <summary>
    /// Gets whether any editable preference currently contains an invalid
    /// value that has not been written to the shared settings document.
    /// </summary>
    public bool HasErrors => _validationErrors.Count > 0;

    /// <summary>
    /// Returns the current corrections for one bindable property, or all
    /// corrections when <paramref name="propertyName"/> is empty.
    /// </summary>
    /// <param name="propertyName">
    /// The view-model property whose bound control is requesting errors.
    /// </param>
    /// <returns>
    /// A stable snapshot containing zero or one correction per editable
    /// preference.
    /// </returns>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _validationErrors.Values.ToArray();
        }

        return _validationErrors.TryGetValue(propertyName, out string? error)
            ? new[] { error }
            : Array.Empty<string>();
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
        ValidationOutcome outcome = RequiredPath.Validate(normalized);
        SetValidationError(propertyName, outcome.ErrorMessage);
        if (outcome.IsValid)
        {
            apply(normalized);
            Persist();
        }
    }

    private void SetValidationError(
        string propertyName,
        string? error)
    {
        bool changed;
        if (error is null)
        {
            changed = _validationErrors.Remove(propertyName);
        }
        else if (_validationErrors.TryGetValue(
                     propertyName,
                     out string? current)
                 && current == error)
        {
            changed = false;
        }
        else
        {
            _validationErrors[propertyName] = error;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        this.RaisePropertyChanged(nameof(HasErrors));
        ErrorsChanged?.Invoke(
            this,
            new DataErrorsChangedEventArgs(propertyName));
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
