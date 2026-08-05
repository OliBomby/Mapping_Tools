using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class DesktopShellTests
{
    [TestMethod]
    public void ShellFeatureRegistry_DuplicateIdentifier_Throws()
    {
        // Arrange
        ShellFeatureRegistration first = Registration("same", "First", () => new StubFeatureViewModel());
        ShellFeatureRegistration second = Registration("SAME", "Second", () => new StubFeatureViewModel());

        // Act
        Action act = () => _ = new ShellFeatureRegistry([first, second]);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*same*registered more than once*");
    }

    [TestMethod]
    public void MainViewModel_SearchPartialExactAndClear_FiltersRegisteredFeatures()
    {
        // Arrange
        using MainViewModel viewModel = CreateMainViewModel(
            [Registration("get-started", "Get started"), Registration("timing", "Timing copier")]);

        // Act
        viewModel.SearchText = "cop";

        // Assert
        viewModel.VisibleFeatures.Select(item => item.Id).Should().Equal("timing");

        // Act
        viewModel.SearchText = "Get started";

        // Assert
        viewModel.VisibleFeatures.Select(item => item.Id).Should().Equal("get-started");

        // Act
        viewModel.SearchText = string.Empty;

        // Assert
        viewModel.VisibleFeatures.Select(item => item.Id).Should().Equal("get-started", "timing");
    }

    [TestMethod]
    public void MainViewModel_ToggleFavorite_UpdatesSettingsAndSortsFavoriteFirst()
    {
        // Arrange
        ApplicationSettings settings = new();
        using MainViewModel viewModel = CreateMainViewModel(
            [Registration("alpha", "Alpha"), Registration("zulu", "Zulu")],
            settings);
        ShellFeatureItemViewModel zulu = viewModel.FeatureItems.Single(item => item.Id == "zulu");

        // Act
        zulu.ToggleFavoriteCommand.Execute(null);

        // Assert
        settings.FavoriteTools.Should().Equal("zulu");
        viewModel.VisibleFeatures.Select(item => item.Id).Should().Equal("zulu", "alpha");
        zulu.IsFavorite.Should().BeTrue();
    }

    [TestMethod]
    public void MainViewModel_ActivateDifferentFeature_DeactivatesPreviousAndCachesInstances()
    {
        // Arrange
        StubFeatureViewModel first = new();
        StubFeatureViewModel second = new();
        using MainViewModel viewModel = CreateMainViewModel(
        [
            Registration("first", "First", () => first),
            Registration("second", "Second", () => second)
        ]);
        ShellFeatureItemViewModel secondItem = viewModel.FeatureItems.Single(item => item.Id == "second");
        ShellFeatureItemViewModel firstItem = viewModel.FeatureItems.Single(item => item.Id == "first");

        // Act
        secondItem.ActivateCommand.Execute(null);
        firstItem.ActivateCommand.Execute(null);

        // Assert
        first.ActivationCount.Should().Be(2);
        first.DeactivationCount.Should().Be(1);
        second.ActivationCount.Should().Be(1);
        second.DeactivationCount.Should().Be(1);
        viewModel.CurrentFeature.Should().BeSameAs(first);
    }

    [TestMethod]
    public async Task MainViewModel_RepeatedNotifications_QueuesInOrderAndDismissesIndependently()
    {
        // Arrange
        UserNotificationService notifications = new();
        using MainViewModel viewModel = CreateMainViewModel(notifications: notifications);
        UserNotification repeated = new(
            UserNotificationSeverity.Warning,
            "Check map",
            "The same warning can occur more than once.");

        // Act
        await notifications.PublishAsync(repeated);
        await notifications.PublishAsync(repeated);

        // Assert
        viewModel.NotificationQueue.Should().HaveCount(2);
        viewModel.NotificationQueue.Select(item => item.Title).Should().Equal("Check map", "Check map");

        // Act
        viewModel.NotificationQueue[0].DismissCommand.Execute(null);

        // Assert
        viewModel.NotificationQueue.Should().ContainSingle();
    }

    [TestMethod]
    public async Task MainViewModel_OpenWebsiteCommand_WhenExecuted_OpensWebsite()
    {
        // Arrange
        RecordingLauncher launcher = new();
        using MainViewModel viewModel = CreateMainViewModel(launcher: launcher);

        // Act
        await ExecuteAsync(viewModel.OpenWebsiteCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://mappingtools.github.io"));
    }

    [TestMethod]
    public async Task MainViewModel_OpenGitHubCommand_WhenPlatformRejects_PublishesWarning()
    {
        // Arrange
        RecordingLauncher launcher = new() { AcceptUris = false };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) =>
            published.Add(eventArgs.Notification);
        using MainViewModel viewModel = CreateMainViewModel(
            notifications: notifications,
            launcher: launcher);

        // Act
        await ExecuteAsync(viewModel.OpenGitHubCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://github.com/OliBomby/Mapping_Tools"));
        published.Should().ContainSingle();
        published[0].Severity.Should().Be(UserNotificationSeverity.Warning);
        published[0].Title.Should().Be("Could not open link");
    }

    [TestMethod]
    public void WindowPlacementCalculator_DisconnectedMonitor_UsesPrimaryWorkingArea()
    {
        // Arrange
        WindowBounds disconnected = new(4000, 200, 1200, 800);
        DesktopWorkingArea primary = new(0, 0, 1920, 1040, true);

        // Act
        WindowBounds restored = WindowPlacementCalculator.Restore(
            disconnected,
            [primary],
            new WindowBounds(80, 60, 1100, 720));

        // Assert
        restored.Should().Be(new WindowBounds(720, 200, 1200, 800));
    }

    [TestMethod]
    public void WindowPlacementCalculator_OversizedOrInvalidBounds_ClampsToWorkingArea()
    {
        // Arrange
        WindowBounds oversized = new(double.NaN, 10, 5000, 3000);
        WindowBounds fallback = new(-100, -100, 1100, 720);
        DesktopWorkingArea primary = new(0, 0, 1024, 700, true);

        // Act
        WindowBounds restored = WindowPlacementCalculator.Restore(
            oversized,
            [primary],
            fallback);

        // Assert
        restored.Should().Be(new WindowBounds(0, 0, 1024, 700));
    }

    private static MainViewModel CreateMainViewModel(
        IReadOnlyList<ShellFeatureRegistration>? registrations = null,
        ApplicationSettings? settings = null,
        IUserNotificationService? notifications = null,
        IPlatformLauncher? launcher = null)
    {
        return new MainViewModel(
            new ShellFeatureRegistry(registrations ??
                [Registration("get-started", "Get started")]),
            settings ?? new ApplicationSettings(),
            notifications ?? new UserNotificationService(),
            launcher ?? new RecordingLauncher(),
            new ImmediateDispatcher());
    }

    private static ShellFeatureRegistration Registration(
        string id,
        string displayName,
        Func<ObservableObject>? factory = null) =>
        new(
            id,
            displayName,
            "Tools",
            $"Open {displayName}.",
            [displayName, id],
            factory ?? (() => new StubFeatureViewModel()));

    private sealed class StubFeatureViewModel : ObservableObject, IShellFeatureActivation
    {
        public int ActivationCount { get; private set; }

        public int DeactivationCount { get; private set; }

        public void Activate() => ActivationCount++;

        public void Deactivate() => DeactivationCount++;
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Post(Action action) => action();
    }

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
