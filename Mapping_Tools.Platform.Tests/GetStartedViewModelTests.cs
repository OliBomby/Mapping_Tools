using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class GetStartedViewModelTests
{
    [TestMethod]
    public void RecentMaps_WhenItemAdded_UpdatesEmptyStateAndRaisesPropertyChanged()
    {
        // Arrange
        GetStartedViewModel viewModel = CreateViewModel();
        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, eventArgs) =>
            changedProperties.Add(eventArgs.PropertyName);

        // Act
        viewModel.RecentMaps.Add(new RecentMapViewModel(
            "map.osu",
            @"C:\Songs\map.osu",
            "today"));

        // Assert
        viewModel.HasNoRecentMaps.Should().BeFalse();
        changedProperties.Should().ContainSingle()
            .Which.Should().Be(nameof(GetStartedViewModel.HasNoRecentMaps));
    }

    [TestMethod]
    public async Task OpenWebsiteCommand_WhenExecuted_OpensWebsite()
    {
        // Arrange
        RecordingLauncher launcher = new();
        GetStartedViewModel viewModel = CreateViewModel(launcher);

        // Act
        await ExecuteAsync(viewModel.OpenWebsiteCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://mappingtools.github.io"));
    }

    [TestMethod]
    public async Task OpenSourceCommand_WhenPlatformRejects_PublishesWarning()
    {
        // Arrange
        RecordingLauncher launcher = new() { AcceptUris = false };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) =>
            published.Add(eventArgs.Notification);
        GetStartedViewModel viewModel = CreateViewModel(launcher, notifications);

        // Act
        await ExecuteAsync(viewModel.OpenSourceCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://github.com/OliBomby/Mapping_Tools"));
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Warning);
        published[0].Title.Should().Be("Could not open link");
    }

    private static GetStartedViewModel CreateViewModel(
        RecordingLauncher? launcher = null,
        IUserNotificationService? notifications = null) =>
        new(
            new ApplicationSettings(),
            launcher ?? new RecordingLauncher(),
            notifications ?? new UserNotificationService());

    private static Task ExecuteAsync(IAsyncRelayCommand command) =>
        command.ExecuteAsync(null);

    private sealed class RecordingLauncher : IPlatformLauncher
    {
        public bool AcceptUris { get; init; } = true;

        public List<Uri> OpenedUris { get; } = [];

        public Task<bool> OpenUriAsync(
            Uri uri,
            CancellationToken cancellationToken = default)
        {
            OpenedUris.Add(uri);
            return Task.FromResult(AcceptUris);
        }

        public Task<bool> OpenFileAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> OpenFolderAsync(
            string path,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
