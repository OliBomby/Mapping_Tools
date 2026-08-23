using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Edits the process-lifetime settings document and applies live-only side
///     effects without exposing Avalonia controls or storage-provider objects.
/// </summary>
public sealed partial class PreferencesViewModel : ObservableValidator, IShellFeatureActivation
{
    private const string CurrentTool = "<Current Tool>";
    private readonly IBetterSaveOverrideService _betterSaveOverride;
    private readonly IFilePicker _filePicker;
    private readonly IHotkeyBindingCoordinator _hotkeyBindings;
    private readonly IUserNotificationService _notifications;
    private readonly IQuickRunCommandRegistry _quickRunRegistry;

    private readonly ApplicationSettings _settings;
    private readonly IApplicationThemeService _themeService;

    /// <summary>Gets or edits the directory that receives beatmap backups.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string _backupsPath;

    /// <summary>Gets or edits the retained-backup limit as a typed count.</summary>
    [ObservableProperty] private int _maxBackupFiles;

    private IReadOnlyList<string> _multipleQuickRunTools = [CurrentTool];
    private IReadOnlyList<string> _noneQuickRunTools = [CurrentTool];

    /// <summary>Gets or edits the current user's osu! configuration file.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string _osuConfigPath;

    /// <summary>Gets or edits the directory containing the osu! executable.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string _osuPath;

    /// <summary>Gets or edits the periodic-backup interval as a typed duration.</summary>
    [ObservableProperty] private TimeSpan _periodicBackupInterval;

    private IReadOnlyList<string> _singleQuickRunTools = [CurrentTool];

    /// <summary>Gets or edits osu!'s beatmap-library directory.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string _songsPath;

    /// <summary>
    ///     Creates an editor over the process-lifetime settings document.
    /// </summary>
    /// <param name="settings">The mutable document shared by desktop services.</param>
    /// <param name="filePicker">Presents native folder and configuration-file pickers.</param>
    /// <param name="themeService">Applies palette changes to the live application.</param>
    /// <param name="notifications">Reports picker failures through the shell.</param>
    /// <param name="quickRunRegistry">Supplies explicit Smart QuickRun target choices.</param>
    /// <param name="hotkeyBindings">Applies shortcut changes to the running global listener.</param>
    /// <param name="betterSaveOverride">Reconfigures automatic save observation immediately.</param>
    public PreferencesViewModel(
        ApplicationSettings settings,
        IFilePicker filePicker,
        IApplicationThemeService themeService,
        IUserNotificationService notifications,
        IQuickRunCommandRegistry quickRunRegistry,
        IHotkeyBindingCoordinator hotkeyBindings,
        IBetterSaveOverrideService betterSaveOverride)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        _hotkeyBindings = hotkeyBindings ?? throw new ArgumentNullException(nameof(hotkeyBindings));
        _betterSaveOverride = betterSaveOverride ?? throw new ArgumentNullException(nameof(betterSaveOverride));

        _osuPath = settings.OsuPath;
        _songsPath = settings.SongsPath;
        _osuConfigPath = settings.OsuConfigPath;
        _backupsPath = settings.BackupsPath;
        _maxBackupFiles = settings.MaxBackupFiles;
        _periodicBackupInterval = settings.PeriodicBackupInterval;
        RefreshQuickRunTools();
    }

    /// <summary>Gets QuickRun targets that accept no selected hit objects.</summary>
    public IReadOnlyList<string> NoneQuickRunTools
    {
        get => _noneQuickRunTools;
        private set => SetProperty(ref _noneQuickRunTools, value);
    }

    /// <summary>Gets QuickRun targets that accept exactly one selected hit object.</summary>
    public IReadOnlyList<string> SingleQuickRunTools
    {
        get => _singleQuickRunTools;
        private set => SetProperty(ref _singleQuickRunTools, value);
    }

    /// <summary>Gets QuickRun targets that accept multiple selected hit objects.</summary>
    public IReadOnlyList<string> MultipleQuickRunTools
    {
        get => _multipleQuickRunTools;
        private set => SetProperty(ref _multipleQuickRunTools, value);
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
            false);
    }

    /// <summary>Gets or sets whether the background backup timer is enabled.</summary>
    public bool MakePeriodicBackups
    {
        get => _settings.MakePeriodicBackups;
        set => SetProperty(
            _settings.MakePeriodicBackups,
            value,
            _settings,
            static (settings, enabled) => settings.MakePeriodicBackups = enabled,
            false);
    }

    /// <summary>
    ///     Gets or sets whether general file pickers begin beside the current beatmap.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder
    {
        get => _settings.CurrentBeatmapDefaultFolder;
        set => SetProperty(
            _settings.CurrentBeatmapDefaultFolder,
            value,
            _settings,
            static (settings, enabled) => settings.CurrentBeatmapDefaultFolder = enabled,
            false);
    }

    /// <summary>Gets or sets whether live editor memory may be read.</summary>
    public bool UseEditorReader
    {
        get => _settings.UseEditorReader;
        set => SetProperty(
            _settings.UseEditorReader,
            value,
            _settings,
            static (settings, enabled) => settings.UseEditorReader = enabled,
            false);
    }

    /// <summary>Gets or sets whether Mapping Tools overwrites osu!'s own save with BetterSave.</summary>
    public bool OverrideOsuSave
    {
        get => _settings.OverrideOsuSave;
        set
        {
            if (SetProperty(
                    _settings.OverrideOsuSave,
                    value,
                    _settings,
                    static (settings, enabled) => settings.OverrideOsuSave = enabled,
                    false))
                _betterSaveOverride.Configure(_settings.SongsPath, value);
        }
    }

    /// <summary>Gets or sets whether tool execution reloads osu! after saving.</summary>
    public bool AutoReload
    {
        get => _settings.AutoReload;
        set => SetProperty(
            _settings.AutoReload,
            value,
            _settings,
            static (settings, enabled) => settings.AutoReload = enabled,
            false);
    }

    /// <summary>Gets or sets whether ordinary Run actions use each feature's QuickRun path.</summary>
    public bool AlwaysQuickRun
    {
        get => _settings.AlwaysQuickRun;
        set => SetProperty(
            _settings.AlwaysQuickRun,
            value,
            _settings,
            static (settings, enabled) => settings.AlwaysQuickRun = enabled,
            false);
    }

    /// <summary>Gets or sets whether QuickRun routes by the live selected-object count.</summary>
    public bool SmartQuickRunEnabled
    {
        get => _settings.SmartQuickRunEnabled;
        set => SetProperty(
            _settings.SmartQuickRunEnabled,
            value,
            _settings,
            static (settings, enabled) => settings.SmartQuickRunEnabled = enabled,
            false);
    }

    /// <summary>Gets or sets the target used when no hit objects are selected.</summary>
    public string NoneQuickRunTool
    {
        get => _settings.NoneQuickRunTool;
        set => SetQuickRunTarget(
            _settings.NoneQuickRunTool,
            value,
            static (settings, target) => settings.NoneQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when exactly one hit object is selected.</summary>
    public string SingleQuickRunTool
    {
        get => _settings.SingleQuickRunTool;
        set => SetQuickRunTarget(
            _settings.SingleQuickRunTool,
            value,
            static (settings, target) => settings.SingleQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when multiple hit objects are selected.</summary>
    public string MultipleQuickRunTool
    {
        get => _settings.MultipleQuickRunTool;
        set => SetQuickRunTarget(
            _settings.MultipleQuickRunTool,
            value,
            static (settings, target) => settings.MultipleQuickRunTool = target);
    }

    /// <summary>Gets or sets the live global QuickRun shortcut.</summary>
    public HotkeySettings? QuickRunHotkey
    {
        get => _settings.QuickRunHotkey;
        set => SetHotkey(
            _settings.QuickRunHotkey,
            value,
            static (settings, hotkey) => settings.QuickRunHotkey = hotkey,
            _hotkeyBindings.ApplyQuickRun);
    }

    /// <summary>Gets or sets the live global QuickUndo shortcut.</summary>
    public HotkeySettings? QuickUndoHotkey
    {
        get => _settings.QuickUndoHotkey;
        set => SetHotkey(
            _settings.QuickUndoHotkey,
            value,
            static (settings, hotkey) => settings.QuickUndoHotkey = hotkey,
            _hotkeyBindings.ApplyQuickUndo);
    }

    /// <summary>Gets or sets the live global BetterSave shortcut.</summary>
    public HotkeySettings? BetterSaveHotkey
    {
        get => _settings.BetterSaveHotkey;
        set => SetHotkey(
            _settings.BetterSaveHotkey,
            value,
            static (settings, hotkey) => settings.BetterSaveHotkey = hotkey,
            _hotkeyBindings.ApplyBetterSave);
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
                    false))
                _themeService.Apply(value);
        }
    }

    /// <inheritdoc />
    public void Activate()
    {
        RefreshQuickRunTools();
    }

    /// <inheritdoc />
    public void Deactivate()
    {
    }

    partial void OnOsuPathChanged(string value)
    {
        ApplyValidatedValue(value, static (settings, path) => settings.OsuPath = path, nameof(OsuPath));
    }

    partial void OnSongsPathChanged(string value)
    {
        ApplyValidatedValue(value, static (settings, path) => settings.SongsPath = path, nameof(SongsPath));
        if (!GetErrors(nameof(SongsPath)).Cast<object>().Any()) _betterSaveOverride.Configure(value, _settings.OverrideOsuSave);
    }

    partial void OnOsuConfigPathChanged(string value)
    {
        ApplyValidatedValue(value, static (settings, path) => settings.OsuConfigPath = path, nameof(OsuConfigPath));
    }

    partial void OnBackupsPathChanged(string value)
    {
        ApplyValidatedValue(value, static (settings, path) => settings.BackupsPath = path, nameof(BackupsPath));
    }

    partial void OnMaxBackupFilesChanged(int value)
    {
        ApplyValidatedValue(value, static (settings, count) => settings.MaxBackupFiles = count, nameof(MaxBackupFiles));
    }

    partial void OnPeriodicBackupIntervalChanged(TimeSpan value)
    {
        ApplyValidatedValue(value, static (settings, interval) => settings.PeriodicBackupInterval = interval, nameof(PeriodicBackupInterval));
    }

    [RelayCommand]
    private Task BrowseOsuPathAsync()
    {
        return PickFolderAsync("Select the osu! folder", OsuPath, path => OsuPath = path);
    }

    [RelayCommand]
    private Task BrowseSongsPathAsync()
    {
        return PickFolderAsync("Select the osu! Songs folder", SongsPath, path => SongsPath = path);
    }

    [RelayCommand]
    private Task BrowseBackupsPathAsync()
    {
        return PickFolderAsync("Select the Mapping Tools backups folder", BackupsPath, path => BackupsPath = path);
    }

    private void ApplyValidatedValue<T>(
        T value,
        Action<ApplicationSettings, T> apply,
        string propertyName)
    {
        ValidationContext context = new(this) { MemberName = propertyName };
        if (Validator.TryValidateProperty(value, context, null)) apply(_settings, value);
    }

    private async Task PickFolderAsync(
        string title,
        string startLocation,
        Action<string> apply)
    {
        try
        {
            var paths = await _filePicker.PickFoldersAsync(
                new OpenFolderPickerRequest
                {
                    Title = title,
                    SuggestedStartLocation = startLocation,
                    AllowMultiple = false,
                });
            if (paths.Count > 0) apply(paths[0]);
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

    [RelayCommand]
    private async Task BrowseOsuConfigPathAsync()
    {
        try
        {
            var paths = await _filePicker.PickOpenFilesAsync(
                new OpenFilePickerRequest
                {
                    Title = "Select the osu! user configuration file",
                    SuggestedStartLocation = OsuPath,
                    AllowMultiple = false,
                    Filters = [CommonFilePickerFilters.OsuConfiguration],
                });
            if (paths.Count > 0) OsuConfigPath = paths[0];
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

    private Task PublishFailureAsync(
        string title,
        string message,
        Exception exception)
    {
        return _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            message,
            exception));
    }

    private void RefreshQuickRunTools()
    {
        NoneQuickRunTools = GetQuickRunTools(QuickRunTargets.NoSelection);
        SingleQuickRunTools = GetQuickRunTools(QuickRunTargets.SingleSelection);
        MultipleQuickRunTools = GetQuickRunTools(QuickRunTargets.MultipleSelection);
    }

    private IReadOnlyList<string> GetQuickRunTools(QuickRunTargets target)
    {
        return
        [
            CurrentTool,
            .. _quickRunRegistry.GetCommandsFor(target)
                .Select(command => command.DisplayName),
        ];
    }

    private void SetQuickRunTarget(
        string current,
        string value,
        Action<ApplicationSettings, string> apply)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        SetProperty(current, value, _settings, apply, false);
    }

    private void SetHotkey(
        HotkeySettings? current,
        HotkeySettings? value,
        Action<ApplicationSettings, HotkeySettings?> apply,
        Action<HotkeySettings?> applyBinding)
    {
        if (SetProperty(current, value, _settings, apply, false)) applyBinding(value);
    }
}
