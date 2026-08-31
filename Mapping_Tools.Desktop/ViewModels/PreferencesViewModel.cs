using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Edits the process-lifetime settings document and applies live-only side
///     effects without exposing Avalonia controls or storage-provider objects.
/// </summary>
public sealed partial class PreferencesViewModel : ObservableValidator, IShellFeatureActivation
{
    private const string current_tool = "<Current Tool>";
    private readonly IBetterSaveOverrideService betterSaveOverride;
    private readonly IFilePicker filePicker;
    private readonly IHotkeyBindingCoordinator hotkeyBindings;
    private readonly IUserNotificationService notifications;
    private readonly IQuickRunCommandRegistry quickRunRegistry;

    private readonly DesktopApplicationSettings settings;
    private readonly IApplicationThemeService themeService;

    /// <summary>Gets or edits the directory that receives beatmap backups.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string backupsPath;

    /// <summary>Gets or edits the retained-backup limit as a typed count.</summary>
    [ObservableProperty] private int maxBackupFiles;

    private IReadOnlyList<string> multipleQuickRunTools = [current_tool];
    private IReadOnlyList<string> noneQuickRunTools = [current_tool];

    /// <summary>Gets or edits the current user's osu! configuration file.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string osuConfigPath;

    /// <summary>Gets or edits the directory containing the osu! executable.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string osuPath;

    /// <summary>Gets or edits the periodic-backup interval as a typed duration.</summary>
    [ObservableProperty] private TimeSpan periodicBackupInterval;

    private IReadOnlyList<string> singleQuickRunTools = [current_tool];

    /// <summary>Gets or edits osu!'s beatmap-library directory.</summary>
    [ObservableProperty] [NotifyDataErrorInfo] [Required(ErrorMessage = "Select a path.")]
    private string songsPath;

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
        DesktopApplicationSettings settings,
        IFilePicker filePicker,
        IApplicationThemeService themeService,
        IUserNotificationService notifications,
        IQuickRunCommandRegistry quickRunRegistry,
        IHotkeyBindingCoordinator hotkeyBindings,
        IBetterSaveOverrideService betterSaveOverride)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.quickRunRegistry = quickRunRegistry ?? throw new ArgumentNullException(nameof(quickRunRegistry));
        this.hotkeyBindings = hotkeyBindings ?? throw new ArgumentNullException(nameof(hotkeyBindings));
        this.betterSaveOverride = betterSaveOverride ?? throw new ArgumentNullException(nameof(betterSaveOverride));

        osuPath = settings.OsuPath;
        songsPath = settings.SongsPath;
        osuConfigPath = settings.OsuConfigPath;
        backupsPath = settings.BackupsPath;
        maxBackupFiles = settings.MaxBackupFiles;
        periodicBackupInterval = settings.PeriodicBackupInterval;
        RefreshQuickRunTools();
    }

    /// <summary>Gets QuickRun targets that accept no selected hit objects.</summary>
    public IReadOnlyList<string> NoneQuickRunTools
    {
        get => noneQuickRunTools;
        private set => SetProperty(ref noneQuickRunTools, value);
    }

    /// <summary>Gets QuickRun targets that accept exactly one selected hit object.</summary>
    public IReadOnlyList<string> SingleQuickRunTools
    {
        get => singleQuickRunTools;
        private set => SetProperty(ref singleQuickRunTools, value);
    }

    /// <summary>Gets QuickRun targets that accept multiple selected hit objects.</summary>
    public IReadOnlyList<string> MultipleQuickRunTools
    {
        get => multipleQuickRunTools;
        private set => SetProperty(ref multipleQuickRunTools, value);
    }

    /// <summary>Gets or sets whether destructive tools create safety backups.</summary>
    public bool MakeBackups
    {
        get => settings.MakeBackups;
        set => SetProperty(
            settings.MakeBackups,
            value,
            settings,
            static (settings, enabled) => settings.MakeBackups = enabled,
            false);
    }

    /// <summary>Gets or sets whether the background backup timer is enabled.</summary>
    public bool MakePeriodicBackups
    {
        get => settings.MakePeriodicBackups;
        set => SetProperty(
            settings.MakePeriodicBackups,
            value,
            settings,
            static (settings, enabled) => settings.MakePeriodicBackups = enabled,
            false);
    }

    /// <summary>
    ///     Gets or sets whether general file pickers begin beside the current beatmap.
    /// </summary>
    public bool CurrentBeatmapDefaultFolder
    {
        get => settings.CurrentBeatmapDefaultFolder;
        set => SetProperty(
            settings.CurrentBeatmapDefaultFolder,
            value,
            settings,
            static (settings, enabled) => settings.CurrentBeatmapDefaultFolder = enabled,
            false);
    }

    /// <summary>Gets or sets whether live editor memory may be read.</summary>
    public bool UseEditorReader
    {
        get => settings.UseEditorReader;
        set => SetProperty(
            settings.UseEditorReader,
            value,
            settings,
            static (settings, enabled) => settings.UseEditorReader = enabled,
            false);
    }

    /// <summary>Gets or sets whether Mapping Tools overwrites osu!'s own save with BetterSave.</summary>
    public bool OverrideOsuSave
    {
        get => settings.OverrideOsuSave;
        set
        {
            if (SetProperty(
                    settings.OverrideOsuSave,
                    value,
                    settings,
                    static (settings, enabled) => settings.OverrideOsuSave = enabled,
                    false))
                betterSaveOverride.Configure(settings.SongsPath, value);
        }
    }

    /// <summary>Gets or sets whether tool execution reloads osu! after saving.</summary>
    public bool AutoReload
    {
        get => settings.AutoReload;
        set => SetProperty(
            settings.AutoReload,
            value,
            settings,
            static (settings, enabled) => settings.AutoReload = enabled,
            false);
    }

    /// <summary>Gets or sets whether ordinary Run actions use each feature's QuickRun path.</summary>
    public bool AlwaysQuickRun
    {
        get => settings.AlwaysQuickRun;
        set => SetProperty(
            settings.AlwaysQuickRun,
            value,
            settings,
            static (settings, enabled) => settings.AlwaysQuickRun = enabled,
            false);
    }

    /// <summary>Gets or sets whether QuickRun routes by the live selected-object count.</summary>
    public bool SmartQuickRunEnabled
    {
        get => settings.SmartQuickRunEnabled;
        set => SetProperty(
            settings.SmartQuickRunEnabled,
            value,
            settings,
            static (settings, enabled) => settings.SmartQuickRunEnabled = enabled,
            false);
    }

    /// <summary>Gets or sets the target used when no hit objects are selected.</summary>
    public string NoneQuickRunTool
    {
        get => settings.NoneQuickRunTool;
        set => SetQuickRunTarget(
            settings.NoneQuickRunTool,
            value,
            static (settings, target) => settings.NoneQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when exactly one hit object is selected.</summary>
    public string SingleQuickRunTool
    {
        get => settings.SingleQuickRunTool;
        set => SetQuickRunTarget(
            settings.SingleQuickRunTool,
            value,
            static (settings, target) => settings.SingleQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when multiple hit objects are selected.</summary>
    public string MultipleQuickRunTool
    {
        get => settings.MultipleQuickRunTool;
        set => SetQuickRunTarget(
            settings.MultipleQuickRunTool,
            value,
            static (settings, target) => settings.MultipleQuickRunTool = target);
    }

    /// <summary>Gets or sets the live global QuickRun shortcut.</summary>
    public HotkeySettings? QuickRunHotkey
    {
        get => settings.QuickRunHotkey;
        set => SetHotkey(
            settings.QuickRunHotkey,
            value,
            static (settings, hotkey) => settings.QuickRunHotkey = hotkey,
            hotkeyBindings.ApplyQuickRun);
    }

    /// <summary>Gets or sets the live global QuickUndo shortcut.</summary>
    public HotkeySettings? QuickUndoHotkey
    {
        get => settings.QuickUndoHotkey;
        set => SetHotkey(
            settings.QuickUndoHotkey,
            value,
            static (settings, hotkey) => settings.QuickUndoHotkey = hotkey,
            hotkeyBindings.ApplyQuickUndo);
    }

    /// <summary>Gets or sets the live global BetterSave shortcut.</summary>
    public HotkeySettings? BetterSaveHotkey
    {
        get => settings.BetterSaveHotkey;
        set => SetHotkey(
            settings.BetterSaveHotkey,
            value,
            static (settings, hotkey) => settings.BetterSaveHotkey = hotkey,
            hotkeyBindings.ApplyBetterSave);
    }

    /// <summary>Gets or sets the palette applied immediately to the live application.</summary>
    public ApplicationTheme Theme
    {
        get => settings.Theme;
        set
        {
            if (SetProperty(
                    settings.Theme,
                    value,
                    settings,
                    static (settings, theme) => settings.Theme = theme,
                    false))
                themeService.Apply(value);
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
        if (!GetErrors(nameof(SongsPath)).Cast<object>().Any()) betterSaveOverride.Configure(value, settings.OverrideOsuSave);
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
        Action<DesktopApplicationSettings, T> apply,
        string propertyName)
    {
        ValidationContext context = new(this) { MemberName = propertyName };
        if (Validator.TryValidateProperty(value, context, null)) apply(settings, value);
    }

    private async Task PickFolderAsync(
        string title,
        string startLocation,
        Action<string> apply)
    {
        try
        {
            var paths = await filePicker.PickFoldersAsync(
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
            var paths = await filePicker.PickOpenFilesAsync(
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
        return notifications.PublishAsync(new UserNotification(
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
            current_tool,
            .. quickRunRegistry.GetCommandsFor(target)
                .Select(command => command.DisplayName),
        ];
    }

    private void SetQuickRunTarget(
        string current,
        string value,
        Action<DesktopApplicationSettings, string> apply)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        SetProperty(current, value, settings, apply, false);
    }

    private void SetHotkey(
        HotkeySettings? current,
        HotkeySettings? value,
        Action<DesktopApplicationSettings, HotkeySettings?> apply,
        Action<HotkeySettings?> applyBinding)
    {
        if (SetProperty(current, value, settings, apply, false)) applyBinding(value);
    }
}
