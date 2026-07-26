using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Platform;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class PreferencesViewModelTests
{
    [TestMethod]
    public void Constructor_WithPersistedSettings_ExposesValuesWithoutSaving()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();

        // Act
        PreferencesViewModel viewModel = CreateViewModel(settings);

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
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);

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
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);
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
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);

        // Act
        viewModel.OsuPath = @"D:\Games\osu!";

        // Assert
        settings.OsuPath.Should().Be(@"D:\Games\osu!");
        ((INotifyDataErrorInfo)viewModel).HasErrors.Should().BeFalse();
    }

    [TestMethod]
    public void MaxBackupFiles_WithOutOfRangeThenValidValue_OnlyAppliesValidValue()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);

        // Act
        viewModel.MaxBackupFiles = 0;

        // Assert
        INotifyDataErrorInfo validation = viewModel;
        validation.GetErrors(nameof(PreferencesViewModel.MaxBackupFiles))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .Equal("Use a whole number from 1 through 100000.");
        settings.MaxBackupFiles.Should().Be(25);

        // Act
        viewModel.MaxBackupFiles = 500;

        // Assert
        validation.GetErrors(nameof(PreferencesViewModel.MaxBackupFiles))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .BeEmpty();
        settings.MaxBackupFiles.Should().Be(500);
    }

    [TestMethod]
    public void PeriodicBackupInterval_WithTooShortThenValidValue_OnlyAppliesValidInterval()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);

        // Act
        viewModel.PeriodicBackupInterval = TimeSpan.Zero;

        // Assert
        INotifyDataErrorInfo validation = viewModel;
        validation.GetErrors(nameof(PreferencesViewModel.PeriodicBackupInterval))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .Equal("Use an interval of at least one second.");
        settings.PeriodicBackupInterval.Should().Be(TimeSpan.FromMinutes(5));

        // Act
        viewModel.PeriodicBackupInterval = TimeSpan.FromMinutes(15);

        // Assert
        validation.GetErrors(nameof(PreferencesViewModel.PeriodicBackupInterval))
            .Cast<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .Should()
            .BeEmpty();
        settings.PeriodicBackupInterval.Should().Be(TimeSpan.FromMinutes(15));
    }

    [TestMethod]
    public void Theme_WhenChanged_AppliesLightThemeInMemory()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();
        RecordingThemeService themes = new();
        PreferencesViewModel viewModel = CreateViewModel(
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
        ApplicationSettings settings = CreateSettings();
        RecordingThemeService themes = new();
        PreferencesViewModel viewModel = CreateViewModel(
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
        ApplicationSettings settings = CreateSettings();
        PreferencesViewModel viewModel = CreateViewModel(settings);

        // Act
        viewModel.MakePeriodicBackups = false;

        // Assert
        settings.MakePeriodicBackups.Should().BeFalse();
    }

    [TestMethod]
    public async Task BrowseBackupsPathCommand_WithSelectedFolder_UpdatesPathInMemory()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();
        StubFilePicker picker = new()
        {
            FolderResults = [@"D:\Mapping Tools Backups"]
        };
        PreferencesViewModel viewModel = CreateViewModel(
            settings,
            filePicker: picker);

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
        ApplicationSettings settings = CreateSettings();
        StubFilePicker picker = new();
        PreferencesViewModel viewModel = CreateViewModel(
            settings,
            filePicker: picker);

        // Act
        await ExecuteAsync(viewModel.BrowseOsuConfigPathCommand);

        // Assert
        picker.LastOpenFileRequest.Should().NotBeNull();
        picker.LastOpenFileRequest!.Filters.Single().Patterns.Should().Equal("osu!.*.cfg");
        settings.OsuConfigPath.Should().Be(@"C:\osu!\osu!.Fixture.cfg");
    }

    [TestMethod]
    public async Task BrowseBackupsPathCommand_WhenPickerFails_PublishesErrorWithoutChangingPath()
    {
        // Arrange
        ApplicationSettings settings = CreateSettings();
        StubFilePicker picker = new()
        {
            ExceptionToThrow = new IOException("Picker unavailable.")
        };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) =>
            published.Add(eventArgs.Notification);
        PreferencesViewModel viewModel = CreateViewModel(
            settings,
            filePicker: picker,
            notifications: notifications);

        // Act
        await ExecuteAsync(viewModel.BrowseBackupsPathCommand);

        // Assert
        settings.BackupsPath.Should().Be(@"C:\Mapping Tools\Backups");
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Error);
        published[0].Title.Should().Be("Could not select folder");
    }

    private static ApplicationSettings CreateSettings() =>
        new()
        {
            OsuPath = @"C:\osu!",
            SongsPath = @"C:\osu!\Songs",
            OsuConfigPath = @"C:\osu!\osu!.Fixture.cfg",
            BackupsPath = @"C:\Mapping Tools\Backups",
            MaxBackupFiles = 25,
            MakePeriodicBackups = true,
            PeriodicBackupInterval = TimeSpan.FromMinutes(5),
            Theme = ApplicationTheme.Dark
        };

    private static PreferencesViewModel CreateViewModel(
        ApplicationSettings settings,
        StubFilePicker? filePicker = null,
        RecordingThemeService? themeService = null,
        IUserNotificationService? notifications = null) =>
        new(
            settings,
            filePicker ?? new StubFilePicker(),
            themeService ?? new RecordingThemeService(),
            notifications ?? new UserNotificationService());

    private static Task ExecuteAsync(IAsyncRelayCommand command) =>
        command.ExecuteAsync(null);

    private sealed class RecordingThemeService : IApplicationThemeService
    {
        public List<ApplicationTheme> AppliedThemes { get; } = [];

        public void Apply(ApplicationTheme theme) => AppliedThemes.Add(theme);
    }

    private sealed class StubFilePicker : IFilePicker
    {
        public bool CanOpenFiles => true;

        public bool CanSaveFiles => true;

        public bool CanPickFolders => true;

        public IReadOnlyList<string> OpenFileResults { get; init; } = [];

        public IReadOnlyList<string> FolderResults { get; init; } = [];

        public Exception? ExceptionToThrow { get; init; }

        public OpenFilePickerRequest? LastOpenFileRequest { get; private set; }

        public OpenFolderPickerRequest? LastFolderRequest { get; private set; }

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            LastOpenFileRequest = request;
            return Task.FromResult(OpenFileResults);
        }

        public Task<string?> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            LastFolderRequest = request;
            return Task.FromResult(FolderResults);
        }
    }
}
