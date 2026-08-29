using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.ViewModels;

[TestClass]
public sealed class PreferencesViewModelTests
{
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
        settings.AutoReload = true;
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
        viewModel.AutoReload.Should().BeTrue();
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
    public void Activate_WithRegisteredCommands_RefreshesTargetsBySelectionSize()
    {
        // Arrange
        var settings = CreateSettings();
        QuickRunCommandRegistry registry = new();
        var viewModel = CreateViewModel(
            settings,
            quickRunRegistry: registry);
        registry.Register(new QuickRunCommand(
            "always",
            "Always",
            QuickRunTargets.Always,
            _ => Task.CompletedTask));
        registry.Register(new QuickRunCommand(
            "selected",
            "Selected",
            QuickRunTargets.AnySelection,
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

    private static ApplicationSettings CreateSettings()
    {
        return new ApplicationSettings
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
        ApplicationSettings settings,
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
            betterSaveOverride ?? new TestBetterSaveOverrideService());
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
