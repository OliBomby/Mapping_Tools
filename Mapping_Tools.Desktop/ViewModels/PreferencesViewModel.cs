using Mapping_Tools.Desktop.Localization;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
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
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Application.Localization;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Edits the process-lifetime settings document and applies live-only side
///     effects without exposing Avalonia controls or storage-provider objects.
/// </summary>
public sealed partial class PreferencesViewModel : LocalizedObservableValidator, IShellFeatureActivation, IShellUndoFeature, IDisposable
{
    private const string current_tool = "<Current Tool>";
    private readonly IBetterSaveOverrideService betterSaveOverride;
    private readonly IFilePicker filePicker;
    private readonly IHotkeyBindingCoordinator hotkeyBindings;
    private readonly IUserNotificationService notifications;
    private readonly IQuickRunCommandRegistry quickRunRegistry;

    private readonly DesktopApplicationSettings settings;
    private readonly IApplicationThemeService themeService;

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
        OsuPath = settings.OsuPath;
        SongsPath = settings.SongsPath;
        OsuConfigPath = settings.OsuConfigPath;
        BackupsPath = settings.BackupsPath;
        MaxBackupFiles = settings.MaxBackupFiles;
        PeriodicBackupInterval = settings.PeriodicBackupInterval;
        RefreshQuickRunTools();
        UndoHistory = new DialogUndoHistory(this, () => { });
        TranslationManager.LanguageChanged += OnLanguageChanged;
    }

    /// <summary>Gets the Preferences edit history retained for this app session.</summary>
    public DialogUndoHistory UndoHistory { get; }

    IProjectUndoHistory IShellUndoFeature.UndoHistory => UndoHistory;

    /// <inheritdoc />
    public void Dispose()
    {
        TranslationManager.LanguageChanged -= OnLanguageChanged;
        UndoHistory.Dispose();
    }

    /// <summary>Gets supported text languages in their native spelling, preceded by the system default.</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new(null, string.Empty),
        new("en", "English"),
        new("nl", "Nederlands"),
    ];

    /// <summary>Gets or sets the live interface language without changing date, number or expression syntax.</summary>
    public LanguageOption SelectedLanguage
    {
        get => settings.Language is null
            ? Languages[0]
            : Languages.FirstOrDefault(option => string.Equals(option.Code, settings.Language.Split('-')[0], StringComparison.OrdinalIgnoreCase)) ?? Languages[1];
        set
        {
            if (SetProperty(settings.Language, value.Code, settings, static (document, code) => document.Language = code, false))
                TranslationManager.SetLanguage(value.Code);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs eventArgs)
    {
        foreach (var language in Languages) language.Refresh();
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    /// <summary>Gets or edits the directory that receives beatmap backups.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.Shell_SelectAPath))]
    [Undoable]
    public partial string BackupsPath { get; set; }

    /// <summary>Gets or edits the retained-backup limit as a typed count.</summary>
    [ObservableProperty]
    [Undoable]
    public partial int MaxBackupFiles { get; set; }

    /// <summary>Gets or edits the current user's osu! configuration file.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.Shell_SelectAPath))]
    [Undoable]
    public partial string OsuConfigPath { get; set; }

    /// <summary>Gets or edits the directory containing the osu! executable.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.Shell_SelectAPath))]
    [Undoable]
    public partial string OsuPath { get; set; }

    /// <summary>Gets or edits the periodic-backup interval as a typed duration.</summary>
    [ObservableProperty]
    [Undoable]
    public partial TimeSpan PeriodicBackupInterval { get; set; }

    /// <summary>Gets or edits osu!'s beatmap-library directory.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessageResourceType = typeof(DesktopStrings), ErrorMessageResourceName = nameof(DesktopStrings.Shell_SelectAPath))]
    [Undoable]
    public partial string SongsPath { get; set; }

    /// <summary>Gets QuickRun targets that accept no selected hit objects.</summary>
    public IReadOnlyList<string> NoneQuickRunTools
    {
        get;
        private set => SetProperty(ref field, value);
    } = [current_tool];

    /// <summary>Gets QuickRun targets that accept exactly one selected hit object.</summary>
    public IReadOnlyList<string> SingleQuickRunTools
    {
        get;
        private set => SetProperty(ref field, value);
    } = [current_tool];

    /// <summary>Gets QuickRun targets that accept multiple selected hit objects.</summary>
    public IReadOnlyList<string> MultipleQuickRunTools
    {
        get;
        private set => SetProperty(ref field, value);
    } = [current_tool];

    /// <summary>Gets or sets whether tool runs create automatic safety backups.</summary>
    [Undoable]
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
    [Undoable]
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
    [Undoable]
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

    /// <summary>Gets the available current-beatmap fetching modes.</summary>
    public IReadOnlyList<CurrentBeatmapFetchingMode> CurrentBeatmapFetchingModes { get; } =
        Enum.GetValues<CurrentBeatmapFetchingMode>();

    /// <summary>Gets or sets how the current beatmap path is fetched.</summary>
    [Undoable]
    public CurrentBeatmapFetchingMode CurrentBeatmapFetching
    {
        get => settings.CurrentBeatmapFetching;
        set => SetProperty(
            settings.CurrentBeatmapFetching,
            value,
            settings,
            static (settings, selected) => settings.CurrentBeatmapFetching = selected,
            false);
    }

    /// <summary>Gets or sets whether active osu!lazer external edits are detected automatically.</summary>
    [Undoable]
    public bool AutoDetectLazerExternalEdit
    {
        get => settings.AutoDetectLazerExternalEdit;
        set => SetProperty(
            settings.AutoDetectLazerExternalEdit,
            value,
            settings,
            static (settings, enabled) => settings.AutoDetectLazerExternalEdit = enabled,
            false);
    }

    /// <summary>Gets the available beatmap live-state reading modes.</summary>
    public IReadOnlyList<BeatmapLiveStateReadingMode> BeatmapLiveStateReadingModes { get; } =
        Enum.GetValues<BeatmapLiveStateReadingMode>();

    /// <summary>Gets or sets how unsaved beatmap editor state is read.</summary>
    [Undoable]
    public BeatmapLiveStateReadingMode BeatmapLiveStateReading
    {
        get => settings.BeatmapLiveStateReading;
        set => SetProperty(
            settings.BeatmapLiveStateReading,
            value,
            settings,
            static (settings, selected) => settings.BeatmapLiveStateReading = selected,
            false);
    }

    /// <summary>Gets the available editor reload modes.</summary>
    public IReadOnlyList<EditorReloadMode> EditorReloadModes { get; } =
        Enum.GetValues<EditorReloadMode>();

    /// <summary>Gets or sets how the osu! editor is reloaded after a save.</summary>
    [Undoable]
    public EditorReloadMode EditorReload
    {
        get => settings.EditorReload;
        set => SetProperty(
            settings.EditorReload,
            value,
            settings,
            static (settings, selected) => settings.EditorReload = selected,
            false);
    }

    /// <summary>Gets or sets whether Mapping Tools overwrites osu!'s own save with BetterSave.</summary>
    [Undoable]
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

    /// <summary>Gets or sets whether ordinary Run actions use each feature's QuickRun path.</summary>
    [Undoable]
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
    [Undoable]
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
    [Undoable]
    public string NoneQuickRunTool
    {
        get => settings.NoneQuickRunTool;
        set => SetQuickRunTarget(
            settings.NoneQuickRunTool,
            value,
            static (settings, target) => settings.NoneQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when exactly one hit object is selected.</summary>
    [Undoable]
    public string SingleQuickRunTool
    {
        get => settings.SingleQuickRunTool;
        set => SetQuickRunTarget(
            settings.SingleQuickRunTool,
            value,
            static (settings, target) => settings.SingleQuickRunTool = target);
    }

    /// <summary>Gets or sets the target used when multiple hit objects are selected.</summary>
    [Undoable]
    public string MultipleQuickRunTool
    {
        get => settings.MultipleQuickRunTool;
        set => SetQuickRunTarget(
            settings.MultipleQuickRunTool,
            value,
            static (settings, target) => settings.MultipleQuickRunTool = target);
    }

    /// <summary>Gets or sets the live global QuickRun shortcut.</summary>
    [Undoable]
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
    [Undoable]
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
    [Undoable]
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
    [Undoable]
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
        string previousPath = settings.SongsPath;
        ApplyValidatedValue(value, static (settings, path) => settings.SongsPath = path, nameof(SongsPath));
        if (settings.SongsPath != previousPath)
            betterSaveOverride.Configure(settings.SongsPath, settings.OverrideOsuSave);
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
        return PickFolderAsync(DesktopStrings.Shell_SelectTheOsuFolder, OsuPath, path => OsuPath = path);
    }

    [RelayCommand]
    private Task BrowseSongsPathAsync()
    {
        return PickFolderAsync(DesktopStrings.Shell_SelectTheOsuSongsFolder, SongsPath, path => SongsPath = path);
    }

    [RelayCommand]
    private Task BrowseBackupsPathAsync()
    {
        return PickFolderAsync(DesktopStrings.Shell_SelectTheMappingToolsBackupsFolder, BackupsPath, path => BackupsPath = path);
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
                DesktopStrings.Shell_FolderPickerFailed,
                DesktopStrings.Shell_FolderPickerFailedMessage,
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
                    Title = DesktopStrings.Shell_SelectTheOsuUserConfigurationFile,
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
                DesktopStrings.Shell_ConfigPickerFailed,
                DesktopStrings.Shell_ConfigPickerFailedMessage,
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
                .OrderBy(command => command.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(command => command.DisplayName),
        ];
    }

    private void SetQuickRunTarget(
        string current,
        string value,
        Action<DesktopApplicationSettings, string> apply,
        [CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        SetProperty(current, value, settings, apply, false, propertyName);
    }

    private void SetHotkey(
        HotkeySettings? current,
        HotkeySettings? value,
        Action<DesktopApplicationSettings, HotkeySettings?> apply,
        Action<HotkeySettings?> applyBinding,
        [CallerMemberName] string propertyName = "")
    {
        if (SetProperty(current, value, settings, apply, false, propertyName)) applyBinding(value);
    }

    // Invalid path text is editable state, but services must retain the last accepted path.
    // Record that separately so undoing into an invalid draft also restores its accepted value.
    [Undoable]
    private string AcceptedOsuPath
    {
        get => settings.OsuPath;
        set => settings.OsuPath = value;
    }

    [Undoable]
    private string AcceptedSongsPath
    {
        get => settings.SongsPath;
        set
        {
            if (settings.SongsPath == value) return;
            settings.SongsPath = value;
            betterSaveOverride.Configure(value, settings.OverrideOsuSave);
        }
    }

    [Undoable]
    private string AcceptedOsuConfigPath
    {
        get => settings.OsuConfigPath;
        set => settings.OsuConfigPath = value;
    }

    [Undoable]
    private string AcceptedBackupsPath
    {
        get => settings.BackupsPath;
        set => settings.BackupsPath = value;
    }
}
