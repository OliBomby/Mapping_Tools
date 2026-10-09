using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Avalonia.Data;
using Avalonia.Input;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Desktop.Localization;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
[DoNotParallelize]
public sealed class PreferencesViewModelTests
{
    [TestMethod]
    public async Task QuickRunHotkey_EditorCapturesGestureAfterPortalRefresh_UpdatesSettingsAndCurrentValue()
    {
        // Arrange
        var settings = CreateSettings();
        settings.QuickRunHotkey = new HotkeySettings(44, 2);
        TestHotkeyBindingCoordinator bindings = new()
        {
            ReadShortcuts = _ => Task.FromResult(
                new Dictionary<string, HotkeySettings> { ["quick-run"] = new(97, 8) }),
        };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings);
        HotkeyEditor editor = new() { DataContext = viewModel };
        editor.Bind(HotkeyEditor.HotkeyProperty, new Binding(nameof(PreferencesViewModel.QuickRunHotkey)) { Mode = BindingMode.TwoWay });
        viewModel.Activate();
        await viewModel.HotkeyRefresh;

        // Act
        editor.ApplyKey(Key.B, KeyModifiers.Control);

        // Assert
        settings.QuickRunHotkey.Should().Be(new HotkeySettings(45, 2));
        bindings.QuickRun.Should().Be(settings.QuickRunHotkey);
        editor.Hotkey.Should().BeSameAs(viewModel.QuickRunHotkey);
        editor.Hotkey.Should().Be(settings.QuickRunHotkey);
        editor.Text.Should().Be("Ctrl + B");
    }

    [TestMethod]
    public async Task Undo_EditorClearsPortalAssignment_RestoresDesktopAssignment()
    {
        // Arrange
        var settings = CreateSettings();
        settings.QuickRunHotkey = new HotkeySettings(44, 2);
        TestHotkeyBindingCoordinator bindings = new()
        {
            ReadShortcuts = _ => Task.FromResult(
                new Dictionary<string, HotkeySettings> { ["quick-run"] = new(97, 8) }),
        };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings);
        HotkeyEditor editor = new() { DataContext = viewModel };
        editor.Bind(HotkeyEditor.HotkeyProperty, new Binding(nameof(PreferencesViewModel.QuickRunHotkey)) { Mode = BindingMode.TwoWay });
        viewModel.Activate();
        await viewModel.HotkeyRefresh;
        editor.ApplyKey(Key.Escape, KeyModifiers.None);

        // Act
        viewModel.UndoHistory.Undo();

        // Assert
        settings.QuickRunHotkey.Should().Be(new HotkeySettings(97, 8));
        editor.Hotkey.Should().Be(settings.QuickRunHotkey);
        editor.Text.Should().Be("Win + F8");
        bindings.QuickRun.Should().Be(settings.QuickRunHotkey);
    }

    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(44, 1)]
    public async Task Activate_DesktopRefreshFails_WarnsOnlyWhenShortcutIsConfigured(int key, int expectedWarnings)
    {
        // Arrange
        var settings = CreateSettings();
        settings.QuickRunHotkey = new HotkeySettings(key, 2);
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        TestHotkeyBindingCoordinator bindings = new() { ReadShortcuts = _ => throw new IOException("Desktop is unavailable") };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings, notifications: notifications);

        // Act
        viewModel.Activate();
        await viewModel.HotkeyRefresh;

        // Assert
        published.Should().HaveCount(expectedWarnings);
        published.Should().OnlyContain(notification => notification.Severity == UserNotificationSeverity.Warning
            && notification.Message == DesktopStrings.Shell_GlobalShortcutsUnavailable);
    }

    [TestMethod]
    public async Task Activate_SameDesktopGestureReentered_ReloadsAssignmentWithoutRegisteringAgain()
    {
        // Arrange
        var settings = CreateSettings();
        settings.QuickRunHotkey = new HotkeySettings(44, 2);
        TestHotkeyBindingCoordinator bindings = new()
        {
            ReadShortcuts = _ => Task.FromResult(
                new Dictionary<string, HotkeySettings> { ["quick-run"] = new(97, 8) }),
        };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings);
        viewModel.Activate();
        await viewModel.HotkeyRefresh;
        viewModel.QuickRunHotkey = new HotkeySettings(97, 8);
        viewModel.Deactivate();

        // Act
        viewModel.Activate();
        await viewModel.HotkeyRefresh;

        // Assert
        viewModel.QuickRunHotkey.Should().Be(new HotkeySettings(97, 8));
        bindings.QuickRun.Should().BeNull();
        viewModel.UndoHistory.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public async Task Activate_DesktopShortcutsChanged_RefreshesSettingsWithoutRebindingOrRecordingUndo()
    {
        // Arrange
        var settings = CreateSettings();
        settings.QuickRunHotkey = new HotkeySettings(44, 2);
        Dictionary<string, HotkeySettings> assignments = new() { ["quick-run"] = new(97, 8), ["quick-undo"] = new(69, 2) };
        int reads = 0;
        TestHotkeyBindingCoordinator bindings = new()
        {
            ReadShortcuts = _ =>
            {
                reads++;
                return Task.FromResult(assignments);
            },
        };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings);
        viewModel.Activate();
        await viewModel.HotkeyRefresh;
        viewModel.Deactivate();
        assignments["quick-run"] = new(98, 1);

        // Act
        viewModel.Activate();
        await viewModel.HotkeyRefresh;

        // Assert
        viewModel.QuickRunHotkey.Should().Be(new HotkeySettings(98, 1));
        viewModel.QuickUndoHotkey.Should().Be(new HotkeySettings(69, 2));
        viewModel.BetterSaveHotkey.Should().BeNull();
        settings.QuickRunHotkey.Should().Be(new HotkeySettings(98, 1));
        viewModel.UndoHistory.CanUndo.Should().BeFalse();
        bindings.QuickRun.Should().BeNull();
        reads.Should().Be(2);
    }

    [TestMethod]
    public async Task Activate_HotkeyEditedDuringRefresh_DiscardsLateDesktopResponse()
    {
        // Arrange
        var settings = CreateSettings();
        TaskCompletionSource<Dictionary<string, HotkeySettings>> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TestHotkeyBindingCoordinator bindings = new() { ReadShortcuts = _ => response.Task };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings);
        viewModel.Activate();

        // Act
        viewModel.QuickRunHotkey = new HotkeySettings(45, 2);
        response.SetResult(new Dictionary<string, HotkeySettings> { ["quick-run"] = new(44, 2) });
        await viewModel.HotkeyRefresh;

        // Assert
        viewModel.QuickRunHotkey.Should().Be(new HotkeySettings(45, 2));
        settings.QuickRunHotkey.Should().Be(new HotkeySettings(45, 2));
        bindings.QuickRun.Should().Be(new HotkeySettings(45, 2));
    }

    [TestMethod]
    public async Task Deactivate_PendingDesktopRefresh_CancelsRefreshWithoutWarning()
    {
        // Arrange
        var settings = CreateSettings();
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        CancellationToken receivedToken = default;
        TestHotkeyBindingCoordinator bindings = new()
        {
            ReadShortcuts = async token =>
            {
                receivedToken = token;
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return [];
            },
        };
        using var viewModel = CreateViewModel(settings, hotkeyBindings: bindings, notifications: notifications);
        viewModel.Activate();

        // Act
        viewModel.Deactivate();
        await viewModel.HotkeyRefresh;

        // Assert
        receivedToken.IsCancellationRequested.Should().BeTrue();
        published.Should().BeEmpty();
        viewModel.QuickRunHotkey.Should().BeNull();
    }

    [TestMethod]
    public void SelectedLanguage_Dutch_UpdatesLiveTextAndSerializesTheLanguageChoice()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        var settings = CreateSettings();
        using var viewModel = CreateViewModel(settings);

        try
        {
            // Act
            viewModel.SelectedLanguage = viewModel.Languages.Single(option => option.Code == "nl");
            var restored = JsonSerializer.Deserialize<DesktopApplicationSettings>(JsonSerializer.Serialize(settings))!;

            // Assert
            DesktopStrings.Shell_Preferences.Should().Be("Voorkeuren");
            restored.Language.Should().Be("nl");
            viewModel.SelectedLanguage.Name.Should().Be("Nederlands");
            viewModel.UndoHistory.CanUndo.Should().BeFalse();
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [TestMethod]
    public void SelectedLanguage_ExistingValidationError_TranslatesWithoutChangingTheDraftOrAcceptedSetting()
    {
        // Arrange
        string? previous = TranslationManager.Language;
        TranslationManager.SetLanguage("en");
        var settings = CreateSettings();
        string original = settings.SongsPath;
        using var viewModel = CreateViewModel(settings);
        viewModel.SongsPath = string.Empty;

        try
        {
            // Act
            viewModel.SelectedLanguage = viewModel.Languages.Single(option => option.Code == "nl");

            // Assert
            viewModel.GetErrors(nameof(PreferencesViewModel.SongsPath)).Should().ContainSingle()
                .Which.ErrorMessage.Should().Be(DesktopStrings.Shell_SelectAPath);
            DesktopStrings.Shell_SelectAPath.Should().NotBe("A path is required.");
            viewModel.SongsPath.Should().BeEmpty();
            settings.SongsPath.Should().Be(original);
        }
        finally
        {
            TranslationManager.SetLanguage(previous);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Constructor_WithPersistedSongsPath_DoesNotReconfigureWatcherOrRecordAnEdit(bool overrideOsuSave)
    {
        // Arrange
        var settings = CreateSettings();
        settings.OverrideOsuSave = overrideOsuSave;
        TestBetterSaveOverrideService watcher = new();

        // Act
        using var viewModel = CreateViewModel(settings, betterSaveOverride: watcher);

        // Assert
        viewModel.SongsPath.Should().Be(@"C:\osu!\Songs");
        settings.SongsPath.Should().Be(@"C:\osu!\Songs");
        watcher.Configurations.Should().BeEmpty();
        viewModel.UndoHistory.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void SongsPath_AfterInvalidDraftReturnsToAcceptedValue_DoesNotReconfigureWatcher()
    {
        // Arrange
        var settings = CreateSettings();
        TestBetterSaveOverrideService watcher = new();
        using var viewModel = CreateViewModel(settings, betterSaveOverride: watcher);
        viewModel.SongsPath = string.Empty;

        // Act
        viewModel.SongsPath = @"C:\osu!\Songs";

        // Assert
        viewModel.HasErrors.Should().BeFalse();
        settings.SongsPath.Should().Be(@"C:\osu!\Songs");
        watcher.Configurations.Should().BeEmpty();
    }

    [TestMethod]
    public void Undo_AfterGroupedPreferenceChanges_RestoresSettingsAndLiveServices()
    {
        // Arrange
        var settings = CreateSettings();
        RecordingThemeService themes = new();
        TestHotkeyBindingCoordinator bindings = new();
        TestBetterSaveOverrideService watcher = new();
        using var viewModel = CreateViewModel(settings, themeService: themes,
            hotkeyBindings: bindings, betterSaveOverride: watcher);
        using (viewModel.UndoHistory.BeginEdit())
        {
            viewModel.Theme = ApplicationTheme.Light;
            viewModel.QuickRunHotkey = new HotkeySettings(90, 6);
            viewModel.QuickUndoHotkey = new HotkeySettings(89, 6);
            viewModel.BetterSaveHotkey = new HotkeySettings(88, 6);
            viewModel.SongsPath = @"D:\Songs";
            viewModel.OverrideOsuSave = true;
            viewModel.MaxBackupFiles = 42;
            viewModel.MakePeriodicBackups = false;
        }

        // Act
        viewModel.UndoHistory.Undo();

        // Assert
        settings.Theme.Should().Be(ApplicationTheme.Dark);
        themes.AppliedThemes.Should().Equal(ApplicationTheme.Light, ApplicationTheme.Dark);
        settings.QuickRunHotkey.Should().BeNull();
        settings.QuickUndoHotkey.Should().BeNull();
        settings.BetterSaveHotkey.Should().BeNull();
        bindings.QuickRun.Should().BeNull();
        bindings.QuickUndo.Should().BeNull();
        bindings.BetterSave.Should().BeNull();
        settings.SongsPath.Should().Be(@"C:\osu!\Songs");
        settings.OverrideOsuSave.Should().BeFalse();
        watcher.Configurations.Last().Should().Be((@"C:\osu!\Songs", false));
        settings.MaxBackupFiles.Should().Be(25);
        settings.MakePeriodicBackups.Should().BeTrue();
        viewModel.UndoHistory.CanUndo.Should().BeFalse();
        viewModel.UndoHistory.CanRedo.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(nameof(PreferencesViewModel.OsuPath))]
    [DataRow(nameof(PreferencesViewModel.SongsPath))]
    [DataRow(nameof(PreferencesViewModel.OsuConfigPath))]
    [DataRow(nameof(PreferencesViewModel.BackupsPath))]
    public void Undo_IntoInvalidPathDraft_RestoresAcceptedPathAndValidation(string propertyName)
    {
        // Arrange
        var settings = CreateSettings();
        TestBetterSaveOverrideService watcher = new();
        using var viewModel = CreateViewModel(settings, betterSaveOverride: watcher);
        var property = typeof(PreferencesViewModel).GetProperty(propertyName)!;
        var setting = typeof(DesktopApplicationSettings).GetProperty(propertyName)!;
        string original = (string)setting.GetValue(settings)!;
        property.SetValue(viewModel, string.Empty);
        viewModel.UndoHistory.Capture();
        property.SetValue(viewModel, @"D:\NewPath");
        viewModel.UndoHistory.Capture();

        // Act
        viewModel.UndoHistory.Undo();

        // Assert
        property.GetValue(viewModel).Should().Be(string.Empty);
        setting.GetValue(settings).Should().Be(original);
        viewModel.HasErrors.Should().BeTrue();
        if (propertyName == nameof(PreferencesViewModel.SongsPath))
            watcher.Configurations.Last().Should().Be((original, false));
    }

    [TestMethod]
    public void Redo_AfterUndo_ReappliesLivePreferencesWithoutRecordingReplay()
    {
        // Arrange
        var settings = CreateSettings();
        RecordingThemeService themes = new();
        TestHotkeyBindingCoordinator bindings = new();
        using var viewModel = CreateViewModel(settings, themeService: themes, hotkeyBindings: bindings);
        HotkeySettings hotkey = new(90, 6);
        using (viewModel.UndoHistory.BeginEdit())
        {
            viewModel.Theme = ApplicationTheme.Light;
            viewModel.QuickRunHotkey = hotkey;
        }
        viewModel.UndoHistory.Undo();

        // Act
        viewModel.UndoHistory.Redo();

        // Assert
        settings.Theme.Should().Be(ApplicationTheme.Light);
        themes.AppliedThemes.Should().Equal(ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.Light);
        settings.QuickRunHotkey.Should().Be(hotkey);
        bindings.QuickRun.Should().Be(hotkey);
        viewModel.UndoHistory.CanRedo.Should().BeFalse();
        viewModel.UndoHistory.CanUndo.Should().BeTrue();
    }

    [TestMethod]
    public void Activate_AfterLeavingPreferences_PreservesHistoryAndDiscardsRedoAfterNewEdit()
    {
        // Arrange
        using var viewModel = CreateViewModel(CreateSettings());
        viewModel.MaxBackupFiles = 30;
        viewModel.UndoHistory.Capture();
        viewModel.Deactivate();

        // Act
        viewModel.Activate();
        viewModel.UndoHistory.Undo();
        int undone = viewModel.MaxBackupFiles;
        viewModel.MaxBackupFiles = 40;
        viewModel.UndoHistory.Capture();

        // Assert
        undone.Should().Be(25);
        viewModel.MaxBackupFiles.Should().Be(40);
        viewModel.UndoHistory.CanRedo.Should().BeFalse();
        viewModel.UndoHistory.CanUndo.Should().BeTrue();
    }

    [TestMethod]
    public void Constructor_WithPersistedSettings_ExposesValuesWithoutSaving()
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var viewModel = CreateViewModel(settings);

        // Assert
        viewModel.OsuPath.Should().Be(@"C:\osu!");
        viewModel.MaxBackupFiles.Should().Be(25);
        viewModel.PeriodicBackupInterval.Should().Be(TimeSpan.FromMinutes(5));
        viewModel.Theme.Should().Be(ApplicationTheme.Dark);
    }

    [TestMethod]
    public void OsuPath_WithBlankText_ShowsValidationAndPreservesPersistedValue()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.OsuPath = "   ";

        // Assert
        INotifyDataErrorInfo validation = viewModel;
        validation.GetErrors(nameof(PreferencesViewModel.OsuPath))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .Equal("Select a path.");
        settings.OsuPath.Should().Be(@"C:\osu!");
    }

    [TestMethod]
    public void OsuPath_WithInvalidThenValidText_UpdatesBindingValidationErrors()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);
        INotifyDataErrorInfo validation = viewModel;
        List<string?> changedProperties = [];
        validation.ErrorsChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        // Act
        viewModel.OsuPath = string.Empty;

        // Assert
        validation.HasErrors.Should().BeTrue();
        validation.GetErrors(nameof(PreferencesViewModel.OsuPath))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .Equal("Select a path.");
        changedProperties.Should().Equal(nameof(PreferencesViewModel.OsuPath));

        // Act
        viewModel.OsuPath = @"D:\Games\osu!";

        // Assert
        validation.HasErrors.Should().BeFalse();
        validation.GetErrors(nameof(PreferencesViewModel.OsuPath))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .BeEmpty();
        changedProperties.Should().Equal(
            nameof(PreferencesViewModel.OsuPath),
            nameof(PreferencesViewModel.OsuPath));
    }

    [TestMethod]
    public void OsuPath_WithNonBlankText_UpdatesSharedSettingsInMemory()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.OsuPath = @"D:\Games\osu!";

        // Assert
        settings.OsuPath.Should().Be(@"D:\Games\osu!");
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
    }

    [TestMethod]
    public void MaxBackupFiles_WithZeroAndLargeValue_AppliesWithoutInventedRange()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.MaxBackupFiles = 0;
        viewModel.MaxBackupFiles = int.MaxValue;

        // Assert
        settings.MaxBackupFiles.Should().Be(int.MaxValue);
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
    }

    [TestMethod]
    public void PeriodicBackupInterval_WithZero_AppliesWithoutInventedMinimum()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.PeriodicBackupInterval = TimeSpan.Zero;

        // Assert
        settings.PeriodicBackupInterval.Should().Be(TimeSpan.Zero);
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
    }

    [TestMethod]
    public void Theme_WhenChanged_AppliesLightThemeInMemory()
    {
        // Arrange
        var settings = CreateSettings();
        RecordingThemeService themes = new();
        var viewModel = CreateViewModel(
            settings,
            themeService: themes);

        // Act
        viewModel.Theme = ApplicationTheme.Light;

        // Assert
        settings.Theme.Should().Be(ApplicationTheme.Light);
        themes.AppliedThemes.Should().Equal(ApplicationTheme.Light);
    }

    [TestMethod]
    public void Theme_WhenUnchanged_DoesNotReapplyTheme()
    {
        // Arrange
        var settings = CreateSettings();
        RecordingThemeService themes = new();
        var viewModel = CreateViewModel(
            settings,
            themeService: themes);

        // Act
        viewModel.Theme = ApplicationTheme.Dark;

        // Assert
        themes.AppliedThemes.Should().BeEmpty();
    }

    [TestMethod]
    public void MakePeriodicBackups_WhenChanged_UpdatesLivePolicyInMemory()
    {
        // Arrange
        var settings = CreateSettings();
        var viewModel = CreateViewModel(settings);

        // Act
        viewModel.MakePeriodicBackups = false;

        // Assert
        settings.MakePeriodicBackups.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithQuickRunSettings_ExposesPersistedValues()
    {
        // Arrange
        var settings = CreateSettings();
        settings.OverrideOsuSave = true;
        settings.EditorReload = EditorReloadMode.SimulatedKeypress;
        settings.AlwaysQuickRun = true;
        settings.SmartQuickRunEnabled = true;
        settings.NoneQuickRunTool = "Cleaner";
        settings.SingleQuickRunTool = "Slider";
        settings.MultipleQuickRunTool = "Transformer";
        settings.QuickRunHotkey = new HotkeySettings(56, 2);
        settings.QuickUndoHotkey = new HotkeySettings(69, 6);
        settings.BetterSaveHotkey = new HotkeySettings(31, 2);

        // Act
        var viewModel = CreateViewModel(settings);

        // Assert
        viewModel.OverrideOsuSave.Should().BeTrue();
        viewModel.EditorReload.Should().Be(EditorReloadMode.SimulatedKeypress);
        viewModel.AlwaysQuickRun.Should().BeTrue();
        viewModel.SmartQuickRunEnabled.Should().BeTrue();
        viewModel.NoneQuickRunTool.Should().Be("Cleaner");
        viewModel.SingleQuickRunTool.Should().Be("Slider");
        viewModel.MultipleQuickRunTool.Should().Be("Transformer");
        viewModel.QuickRunHotkey.Should().Be(settings.QuickRunHotkey);
        viewModel.QuickUndoHotkey.Should().Be(settings.QuickUndoHotkey);
        viewModel.BetterSaveHotkey.Should().Be(settings.BetterSaveHotkey);
    }

    [TestMethod]
    public void Activate_WithUnsortedRegisteredCommands_RefreshesTargetsAlphabeticallyBySelectionSize()
    {
        // Arrange
        var settings = CreateSettings();
        QuickRunCommandRegistry registry = new();
        var viewModel = CreateViewModel(
            settings,
            quickRunRegistry: registry);
        registry.Register(new QuickRunCommand(
            "selected",
            "Selected",
            QuickRunTargets.AnySelection,
            _ => Task.CompletedTask));
        registry.Register(new QuickRunCommand(
            "always",
            "Always",
            QuickRunTargets.Always,
            _ => Task.CompletedTask));

        // Act
        viewModel.Activate();

        // Assert
        viewModel.NoneQuickRunTools.Should().Equal("<Current Tool>", "Always");
        viewModel.SingleQuickRunTools.Should().Equal("<Current Tool>", "Always", "Selected");
        viewModel.MultipleQuickRunTools.Should().Equal("<Current Tool>", "Always", "Selected");
    }

    [TestMethod]
    public void QuickRunHotkey_WhenChanged_UpdatesSettingsAndLiveBinding()
    {
        // Arrange
        var settings = CreateSettings();
        TestHotkeyBindingCoordinator bindings = new();
        var viewModel = CreateViewModel(
            settings,
            hotkeyBindings: bindings);
        HotkeySettings hotkey = new(90, 6);

        // Act
        viewModel.QuickRunHotkey = hotkey;

        // Assert
        settings.QuickRunHotkey.Should().Be(hotkey);
        bindings.QuickRun.Should().Be(hotkey);
    }

    [TestMethod]
    public void OverrideOsuSave_WhenChanged_ReconfiguresWatcherImmediately()
    {
        // Arrange
        var settings = CreateSettings();
        TestBetterSaveOverrideService betterSaveOverride = new();
        var viewModel = CreateViewModel(
            settings,
            betterSaveOverride: betterSaveOverride);

        // Act
        viewModel.OverrideOsuSave = true;

        // Assert
        settings.OverrideOsuSave.Should().BeTrue();
        betterSaveOverride.Configurations.Should().Equal((settings.SongsPath, true));
    }

    [TestMethod]
    public void SongsPath_WithValidValue_ReconfiguresEnabledWatcher()
    {
        // Arrange
        var settings = CreateSettings();
        settings.OverrideOsuSave = true;
        TestBetterSaveOverrideService betterSaveOverride = new();
        var viewModel = CreateViewModel(
            settings,
            betterSaveOverride: betterSaveOverride);

        // Act
        viewModel.SongsPath = @"D:\osu!\Songs";

        // Assert
        settings.SongsPath.Should().Be(@"D:\osu!\Songs");
        betterSaveOverride.Configurations.Should().Equal((@"D:\osu!\Songs", true));
    }

    [TestMethod]
    public async Task BrowseBackupsPathCommand_WithSelectedFolder_UpdatesPathInMemory()
    {
        // Arrange
        var settings = CreateSettings();
        TestFilePicker picker = new()
        {
            Folders = [@"D:\Mapping Tools Backups"],
        };
        var viewModel = CreateViewModel(
            settings,
            picker);

        // Act
        await ExecuteAsync(viewModel.BrowseBackupsPathCommand);

        // Assert
        picker.LastFolderRequest.Should().NotBeNull();
        picker.LastFolderRequest!.AllowMultiple.Should().BeFalse();
        settings.BackupsPath.Should().Be(@"D:\Mapping Tools Backups");
    }

    [TestMethod]
    public async Task BrowseOsuConfigPathCommand_WhenCancelled_PreservesPathWithoutSaving()
    {
        // Arrange
        var settings = CreateSettings();
        TestFilePicker picker = new();
        var viewModel = CreateViewModel(
            settings,
            picker);

        // Act
        await ExecuteAsync(viewModel.BrowseOsuConfigPathCommand);

        // Assert
        picker.LastOpenRequest.Should().NotBeNull();
        picker.LastOpenRequest!.Filters.Single().Patterns.Should().Equal("osu!.*.cfg");
        settings.OsuConfigPath.Should().Be(@"C:\osu!\osu!.Fixture.cfg");
    }

    [TestMethod]
    public async Task BrowseBackupsPathCommand_WhenPickerFails_PublishesErrorWithoutChangingPath()
    {
        // Arrange
        var settings = CreateSettings();
        TestFilePicker picker = new()
        {
            ExceptionToThrow = new IOException("Picker unavailable."),
        };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) =>
            published.Add(eventArgs.Notification);
        var viewModel = CreateViewModel(
            settings,
            picker,
            notifications: notifications);

        // Act
        await ExecuteAsync(viewModel.BrowseBackupsPathCommand);

        // Assert
        settings.BackupsPath.Should().Be(@"C:\Mapping Tools\Backups");
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Error);
        published[0].Title.Should().Be("Could not select folder");
    }

    private static DesktopApplicationSettings CreateSettings()
    {
        return new DesktopApplicationSettings
        {
            OsuPath = @"C:\osu!",
            SongsPath = @"C:\osu!\Songs",
            OsuConfigPath = @"C:\osu!\osu!.Fixture.cfg",
            BackupsPath = @"C:\Mapping Tools\Backups",
            MaxBackupFiles = 25,
            MakePeriodicBackups = true,
            PeriodicBackupInterval = TimeSpan.FromMinutes(5),
            Theme = ApplicationTheme.Dark,
        };
    }

    private static PreferencesViewModel CreateViewModel(
        DesktopApplicationSettings settings,
        TestFilePicker? filePicker = null,
        RecordingThemeService? themeService = null,
        IUserNotificationService? notifications = null,
        IQuickRunCommandRegistry? quickRunRegistry = null,
        IHotkeyBindingCoordinator? hotkeyBindings = null,
        IBetterSaveOverrideService? betterSaveOverride = null)
    {
        return new PreferencesViewModel(
            settings,
            filePicker ?? new TestFilePicker(),
            themeService ?? new RecordingThemeService(),
            notifications ?? new UserNotificationService(),
            quickRunRegistry ?? new QuickRunCommandRegistry(),
            hotkeyBindings ?? new TestHotkeyBindingCoordinator(),
            betterSaveOverride ?? new TestBetterSaveOverrideService(),
            hotkeyBindings as IGlobalHotkeyRegistration);
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

    private sealed class RecordingThemeService : IApplicationThemeService
    {
        public List<ApplicationTheme> AppliedThemes { get; } = [];

        public void Apply(ApplicationTheme theme)
        {
            AppliedThemes.Add(theme);
        }
    }
}
