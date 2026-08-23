using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Shell;

[TestClass]
public sealed class DesktopShellTests
{
    [TestMethod]
    public void ShellFeatureRegistry_DuplicateIdentifier_Throws()
    {
        // Arrange
        var first = Registration("same", "First", () => new StubFeatureViewModel());
        var second = Registration("SAME", "Second", () => new StubFeatureViewModel());

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
        using var viewModel = CreateMainViewModel(
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
    public void MainViewModel_SearchExcludesHighlightedItem_HighlightsFirstVisibleFeature()
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
            [Registration("get-started", "Get started"), Registration("timing", "Timing copier")]);

        // Act
        viewModel.SearchText = "timing";

        // Assert
        viewModel.HighlightedFeature.Should().BeSameAs(viewModel.VisibleFeatures.Single());
    }

    [TestMethod]
    public void MoveHighlightedFeature_WithKeyboardOffsets_ChangesOnlyHighlightedItem()
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
            [Registration("first", "First"), Registration("second", "Second")]);
        var initiallyActive = viewModel.SelectedFeature!;

        // Act
        viewModel.MoveHighlightedFeature(1);

        // Assert
        viewModel.HighlightedFeature.Should().BeSameAs(viewModel.VisibleFeatures[1]);
        viewModel.SelectedFeature.Should().BeSameAs(initiallyActive);
    }

    [TestMethod]
    public void ActivateHighlightedFeature_WithKeyboardSelection_OpensHighlightedPage()
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
            [Registration("first", "First"), Registration("second", "Second")]);
        viewModel.MoveHighlightedFeature(1);

        // Act
        viewModel.ActivateHighlightedFeature();

        // Assert
        viewModel.SelectedFeature.Should().BeSameAs(viewModel.VisibleFeatures[1]);
        viewModel.HighlightedFeature.Should().BeSameAs(viewModel.SelectedFeature);
    }

    [TestMethod]
    public void MainViewModel_ToggleFavorite_UpdatesSettingsAndSortsFavoriteFirst()
    {
        // Arrange
        ApplicationSettings settings = new();
        using var viewModel = CreateMainViewModel(
            [Registration("alpha", "Alpha"), Registration("zulu", "Zulu")],
            settings);
        var zulu = viewModel.FeatureItems.Single(item => item.Id == "zulu");

        // Act
        zulu.ToggleFavoriteCommand.Execute(null);

        // Assert
        settings.FavoriteTools.Should().Equal("zulu");
        viewModel.VisibleFeatures.Select(item => item.Id).Should().Equal("zulu", "alpha");
        zulu.IsFavorite.Should().BeTrue();
    }

    [TestMethod]
    public void MainViewModel_WithFoundationalFavoritesAndTools_GroupsItemsWithInertDividers()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            FavoriteTools = ["favorite"],
        };
        using var viewModel = CreateMainViewModel(
        [
            Registration("get-started", "Get started", category: "General"),
            Registration("preferences", "Preferences", category: "General"),
            Registration("ordinary", "Ordinary"),
            Registration("favorite", "Favorite tool"),
        ], settings);

        // Act
        object[] entries = viewModel.NavigationEntries.ToArray();

        // Assert
        entries.OfType<ShellFeatureItemViewModel>().Select(item => item.Id).Should().Equal(
            "get-started",
            "preferences",
            "favorite",
            "ordinary");
        entries.Select(entry => entry.GetType()).Should().Equal(
            typeof(ShellFeatureItemViewModel),
            typeof(ShellFeatureItemViewModel),
            typeof(NavigationDividerViewModel),
            typeof(ShellFeatureItemViewModel),
            typeof(NavigationDividerViewModel),
            typeof(ShellFeatureItemViewModel));
    }

    [TestMethod]
    public void Prepare_WithFeature_AssignsTooltipAndContextMenuToListBoxItem()
    {
        // Arrange
        var registration = Registration("feature", "Feature");
        ShellFeatureItemViewModel feature = new(
            registration,
            0,
            false,
            _ => { },
            _ => { });
        NavigationListBoxItem container = new();

        // Act
        container.Prepare(feature, null);

        // Assert
        ToolTip.GetTip(container).Should().Be(feature.Description);
        container.ContextMenu.Should().NotBeNull();
        var menuItem = container.ContextMenu!.Items.Single().Should().BeOfType<MenuItem>().Subject;
        menuItem.Header.Should().Be("Favorite");
        menuItem.Command.Should().BeSameAs(feature.ToggleFavoriteCommand);
    }

    [TestMethod]
    public void MainViewModel_ActivateDifferentFeature_DeactivatesPreviousAndCachesInstances()
    {
        // Arrange
        StubFeatureViewModel first = new();
        StubFeatureViewModel second = new();
        using var viewModel = CreateMainViewModel(
        [
            Registration("first", "First", () => first),
            Registration("second", "Second", () => second),
        ]);
        var secondItem = viewModel.FeatureItems.Single(item => item.Id == "second");
        var firstItem = viewModel.FeatureItems.Single(item => item.Id == "first");

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
    public void MainViewModel_ActivatesQuickRunFeature_UpdatesCurrentRegistryTool()
    {
        // Arrange
        QuickRunCommandRegistry quickRunRegistry = new();
        quickRunRegistry.Register(new QuickRunCommand(
            "quick-tool",
            "Quick tool",
            QuickRunTargets.Always,
            _ => Task.CompletedTask));
        StubQuickRunFeatureViewModel quickTool = new();
        using var viewModel = CreateMainViewModel(
            [
                Registration("ordinary", "Ordinary"),
                Registration("quick", "Quick", () => quickTool),
            ],
            quickRunRegistry: quickRunRegistry);
        var quickItem = viewModel.FeatureItems.Single(item => item.Id == "quick");
        var ordinaryItem = viewModel.FeatureItems.Single(item => item.Id == "ordinary");

        // Act
        quickItem.ActivateCommand.Execute(null);

        // Assert
        quickRunRegistry.CurrentCommandId.Should().Be("quick-tool");

        // Act
        ordinaryItem.ActivateCommand.Execute(null);

        // Assert
        quickRunRegistry.CurrentCommandId.Should().BeNull();
    }

    [TestMethod]
    public void MainViewModel_ActivateFeature_AppliesShellOwnedScrollContract()
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
        [
            Registration("first", "First"),
            Registration(
                "second",
                "Second",
                horizontalScrollBarVisibility: ScrollBarVisibility.Auto,
                verticalScrollBarVisibility: ScrollBarVisibility.Visible),
        ]);
        var second = viewModel.FeatureItems.Single(item => item.Id == "second");

        // Act
        second.ActivateCommand.Execute(null);

        // Assert
        viewModel.ContentHorizontalScrollBarVisibility.Should().Be(ScrollBarVisibility.Auto);
        viewModel.ContentVerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Visible);
    }

    [TestMethod]
    public async Task MainViewModel_RepeatedNotifications_QueuesInOrderAndDismissesIndependently()
    {
        // Arrange
        UserNotificationService notifications = new();
        using var viewModel = CreateMainViewModel(notifications: notifications);
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
        using var viewModel = CreateMainViewModel(launcher: launcher);

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
        using var viewModel = CreateMainViewModel(
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
    public async Task MainViewModel_OpenDonateCommand_WhenExecuted_OpensLegacyDonationPage()
    {
        // Arrange
        RecordingLauncher launcher = new();
        using var viewModel = CreateMainViewModel(launcher: launcher);

        // Act
        await ExecuteAsync(viewModel.OpenDonateCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://ko-fi.com/olibomby"));
    }

    [TestMethod]
    public async Task MainViewModel_OpenAboutCommand_WhenExecuted_PresentsLegacyCredits()
    {
        // Arrange
        TestDialogService dialogs = new();
        using var viewModel = CreateMainViewModel(dialogs: dialogs);

        // Act
        await ExecuteAsync(viewModel.OpenAboutCommand);

        // Assert
        dialogs.MessageCount.Should().Be(1);
        dialogs.LastMessageRequest.Should().BeOfType<MessageDialogRequest<bool>>()
            .Which.Message.Should().Contain("Supporters:").And.Contain("Contributors:");
    }

    [TestMethod]
    public async Task MainViewModel_BetterSaveCommand_WhenExecuted_UsesSharedService()
    {
        // Arrange
        TestBetterSaveService betterSave = new();
        using var viewModel = CreateMainViewModel(betterSave: betterSave);

        // Act
        await ExecuteAsync(viewModel.BetterSaveCommand);

        // Assert
        betterSave.ExecutionCount.Should().Be(1);
    }

    [TestMethod]
    public async Task ProjectCommands_WithProjectFeature_ShowMenuAndDelegateToFeature()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        RecordingProjectService projectService = new();
        TestDialogService dialogs = new() { BooleanResult = true };
        using var viewModel = CreateMainViewModel(
            [
                Registration("home", "Home"),
                Registration("project", "Project", () => project),
            ],
            dialogs: dialogs,
            projectService: projectService);
        var projectItem = viewModel.FeatureItems
            .Single(item => item.Id == "project");

        // Act
        projectItem.ActivateCommand.Execute(null);
        await ExecuteAsync(viewModel.SaveProjectCommand);
        await ExecuteAsync(viewModel.OpenProjectCommand);
        await ExecuteAsync(viewModel.NewProjectCommand);

        // Assert
        viewModel.HasProjectMenu.Should().BeTrue();
        projectService.SaveAsCount.Should().Be(1);
        projectService.OpenCount.Should().Be(1);
        projectService.CreateNewCount.Should().Be(1);
    }

    [TestMethod]
    public async Task NewProjectCommand_WhenConfirmationIsDeclined_DoesNotReplaceProject()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        TestDialogService dialogs = new() { BooleanResult = false };
        using var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)],
            dialogs: dialogs);

        // Act
        await ExecuteAsync(viewModel.NewProjectCommand);

        // Assert
        project.InstallCount.Should().Be(0);
        dialogs.MessageCount.Should().Be(1);
        var request = dialogs.LastMessageRequest.Should()
            .BeOfType<MessageDialogRequest<bool>>().Subject;
        request.Title.Should().Be("Confirm new project");
        request.Message.Should().Be(
            "Are you sure you want to start a new project? All unsaved progress will be lost.");
        request.Choices.Select(choice => choice.Label).Should().Equal("Yes", "No");
        request.DismissResult.Should().BeFalse();
    }

    [TestMethod]
    public void WindowPlacementCalculator_DisconnectedMonitor_UsesPrimaryWorkingArea()
    {
        // Arrange
        WindowBounds disconnected = new(4000, 200, 1200, 800);
        DesktopWorkingArea primary = new(0, 0, 1920, 1040, true);

        // Act
        var restored = WindowPlacementCalculator.Restore(
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
        var restored = WindowPlacementCalculator.Restore(
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
        IPlatformLauncher? launcher = null,
        IBetterSaveService? betterSave = null,
        TestDialogService? dialogs = null,
        IQuickRunCommandRegistry? quickRunRegistry = null,
        RecordingProjectService? projectService = null)
    {
        var resolvedSettings = settings ?? new ApplicationSettings();
        var resolvedNotifications = notifications ?? new UserNotificationService();
        var resolvedDialogs = dialogs ?? new TestDialogService();
        var resolvedQuickRunRegistry = quickRunRegistry ?? new QuickRunCommandRegistry();
        projectService ??= new RecordingProjectService();
        ImmediateDispatcher dispatcher = new();
        BeatmapWorkspaceViewModel workspace = new(
            new TestBeatmapWorkspace(),
            new TestBeatmapBackupService(),
            new TestQuickUndoCommandService(),
            new TestFilePicker(),
            new TestFileRevealService(),
            new TestApplicationDirectories(),
            resolvedSettings,
            new TestDialogService(),
            resolvedNotifications,
            dispatcher);
        return new MainViewModel(
            new ShellFeatureRegistry(registrations ?? [Registration("get-started", "Get started")]),
            resolvedQuickRunRegistry,
            resolvedSettings,
            resolvedNotifications,
            launcher ?? new RecordingLauncher(),
            dispatcher,
            workspace,
            betterSave ?? new TestBetterSaveService(),
            resolvedDialogs,
            new ProjectAutosaveCoordinator(
                projectService,
                resolvedDialogs,
                resolvedNotifications));
    }

    private static ShellFeatureRegistration Registration(
        string id,
        string displayName,
        Func<ObservableObject>? factory = null,
        string category = "Tools",
        ScrollBarVisibility horizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        ScrollBarVisibility verticalScrollBarVisibility = ScrollBarVisibility.Disabled)
    {
        return new ShellFeatureRegistration(
            id,
            displayName,
            category,
            $"Open {displayName}.",
            [displayName, id],
            factory ?? (() => new StubFeatureViewModel()),
            horizontalScrollBarVisibility: horizontalScrollBarVisibility,
            verticalScrollBarVisibility: verticalScrollBarVisibility);
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

    private sealed class StubFeatureViewModel : ObservableObject, IShellFeatureActivation
    {
        public int ActivationCount { get; private set; }

        public int DeactivationCount { get; private set; }

        public void Activate()
        {
            ActivationCount++;
        }

        public void Deactivate()
        {
            DeactivationCount++;
        }
    }

    private sealed class StubQuickRunFeatureViewModel : ObservableObject, IQuickRun
    {
        public string OperationId => "quick-tool";

        public Task RunQuickAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubProjectFeatureViewModel : ObservableObject, IShellProjectFeature
    {
        private static readonly ProjectDefinition<StubProject> Definition = new(
            "stubproject.json",
            "Stub Projects",
            static () => new StubProject());

        public int InstallCount { get; private set; }

        public IProjectDefinition ProjectDefinition => Definition;

        public object Snapshot()
        {
            return new StubProject();
        }

        public void Install(object project)
        {
            InstallCount++;
        }
    }

    private sealed record StubProject;

    private sealed class RecordingProjectService : IProjectService
    {
        public int SaveAsCount { get; private set; }

        public int OpenCount { get; private set; }

        public int CreateNewCount { get; private set; }

        public string GetAutoSavePath(IProjectDefinition definition)
        {
            return Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);
        }

        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
        {
            return Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);
        }

        public string GetProjectFolder(IProjectDefinition definition)
        {
            return Path.GetTempPath();
        }

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
        {
            return Path.GetTempPath();
        }

        public object CreateNew(IProjectDefinition definition)
        {
            CreateNewCount++;
            return definition.CreateProject();
        }

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition)
        {
            return definition.CreateProject();
        }

        public Task SaveAsync<TProject>(
            string path,
            TProject project,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TProject> LoadAsync<TProject>(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<TProject>(new FileNotFoundException());
        }

        public Task<object> LoadAsync(
            IProjectDefinition definition,
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<object>(new FileNotFoundException());
        }

        public Task AutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AutoSaveAsync(
            IProjectDefinition definition,
            object project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string?> SaveAsAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> SaveAsAsync(
            IProjectDefinition definition,
            object project,
            CancellationToken cancellationToken = default)
        {
            SaveAsCount++;
            return Task.FromResult<string?>("saved.json");
        }

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProjectOpenResult<TProject>?>(null);
        }

        public Task<ProjectOpenResult?> OpenAsync(
            IProjectDefinition definition,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult<ProjectOpenResult?>(
                new ProjectOpenResult("opened.json", new StubProject()));
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public void Post(Action action)
        {
            action();
        }
    }

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
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> OpenFolderAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
