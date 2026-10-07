using System.Diagnostics.CodeAnalysis;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Migration.Contracts;
using Mapping_Tools.Application.Migration.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Contracts;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Application.Workspace.Models;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.Controls.FeatureLoading;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.HitsoundPreviewHelper.Views;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels.Adapters;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Models;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Views;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Shell;

[TestClass]
public sealed class DesktopShellTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The feature factory is used only before the Preferences view model is disposed.")]
    public async Task MainWindow_WithFocusedPreferencesHotkey_UndoesEachGestureAndUpdatesInput(int inputIndex)
    {
        // Arrange
        DesktopApplicationSettings settings = new();
        TestHotkeyBindingCoordinator bindings = new();
        using PreferencesViewModel preferences = new(settings, new TestFilePicker(),
            new StubThemeService(), new UserNotificationService(), new QuickRunCommandRegistry(),
            bindings, new TestBetterSaveOverrideService());
        await using var viewModel = CreateMainViewModel(
            [Registration("preferences", "Preferences", () => preferences)], settings: settings);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        HotkeyEditor input = window.GetVisualDescendants().OfType<HotkeyEditor>().ElementAt(inputIndex);
        input.Focus();

        // Act
        host.PressKey(Key.B, RawInputModifiers.Alt, PhysicalKey.B, "b");
        window.KeyRelease(Key.B, RawInputModifiers.Alt, PhysicalKey.B, "b");
        host.PressKey(Key.C, RawInputModifiers.Alt, PhysicalKey.C, "c");
        window.KeyRelease(Key.C, RawInputModifiers.Alt, PhysicalKey.C, "c");
        host.PressKey(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        HotkeySettings? undone = input.Hotkey;
        string? undoneText = input.Text;
        host.PressKey(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        HotkeySettings? original = input.Hotkey;
        host.PressKey(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");

        // Assert
        HotkeySettings expected = new((int)Key.B, (int)KeyModifiers.Alt);
        undone.Should().Be(expected);
        undoneText.Should().Be(HotkeyEditor.Format(expected));
        original.Should().BeNull();
        input.Hotkey.Should().Be(expected);
        input.Text.Should().Be(HotkeyEditor.Format(expected));
        HotkeySettings?[] stored = [settings.QuickRunHotkey, settings.QuickUndoHotkey, settings.BetterSaveHotkey];
        HotkeySettings?[] live = [bindings.QuickRun, bindings.QuickUndo, bindings.BetterSave];
        stored[inputIndex].Should().Be(expected);
        live[inputIndex].Should().Be(expected);
    }

    [DataTestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, false)]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The feature factory is used only before the Preferences view model is disposed.")]
    public async Task MainWindow_AfterSmartQuickRunSelection_UndoesAndRedoesFocusedInput(int inputIndex, bool useMouse)
    {
        // Arrange
        DesktopApplicationSettings settings = new() { SmartQuickRunEnabled = true };
        QuickRunCommandRegistry registry = new();
        registry.Register(new QuickRunCommand("first", "First", QuickRunTargets.Always, _ => Task.CompletedTask));
        using PreferencesViewModel preferences = new(settings, new TestFilePicker(),
            new StubThemeService(), new UserNotificationService(), registry,
            new TestHotkeyBindingCoordinator(), new TestBetterSaveOverrideService());
        await using var viewModel = CreateMainViewModel(
            [Registration("preferences", "Preferences", () => preferences)], settings: settings);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        ComboBox input = window.GetVisualDescendants().OfType<PreferencesView>().Single()
            .GetVisualDescendants().OfType<ComboBox>()
            .Where(comboBox => comboBox.ItemsSource is IReadOnlyList<string>).ElementAt(inputIndex);
        input.Focus();

        // Act
        if (useMouse)
        {
            host.Click(input);
            Popup popup = input.GetVisualDescendants().OfType<Popup>().Single();
            TopLevel dropdown = TopLevel.GetTopLevel(popup.Child!)!;
            ComboBoxItem option = dropdown.GetVisualDescendants().OfType<ComboBoxItem>()
                .Single(item => Equals(item.DataContext, "First"));
            Point point = option.TranslatePoint(new Point(option.Bounds.Width / 2, option.Bounds.Height / 2), dropdown)!.Value;
            dropdown.MouseMove(point);
            dropdown.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
            dropdown.MouseUp(point, MouseButton.Left);
            HeadlessViewHost.RunDispatcherJobs();
        }
        else
        {
            host.PressKey(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "");
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "");
        }
        object? selected = input.SelectedItem;
        host.PressKey(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        object? undone = input.SelectedItem;
        string[] storedAfterUndo = [settings.NoneQuickRunTool, settings.SingleQuickRunTool, settings.MultipleQuickRunTool];
        host.PressKey(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");

        // Assert
        selected.Should().Be("First");
        undone.Should().Be("<Current Tool>");
        storedAfterUndo[inputIndex].Should().Be("<Current Tool>");
        input.SelectedItem.Should().Be("First");
        string[] storedAfterRedo = [settings.NoneQuickRunTool, settings.SingleQuickRunTool, settings.MultipleQuickRunTool];
        storedAfterRedo[inputIndex].Should().Be("First");
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The feature factory is used only before the Preferences view model is disposed.")]
    public async Task MainWindow_WithPreferences_UndoesAndRedoesThroughShortcutsWithoutProjectMenu()
    {
        // Arrange
        DesktopApplicationSettings settings = new() { MaxBackupFiles = 25 };
        using PreferencesViewModel preferences = new(settings, new TestFilePicker(),
            new StubThemeService(), new UserNotificationService(), new QuickRunCommandRegistry(),
            new TestHotkeyBindingCoordinator(), new TestBetterSaveOverrideService());
        await using var viewModel = CreateMainViewModel(
            [Registration("preferences", "Preferences", () => preferences)], settings: settings);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        preferences.MaxBackupFiles = 42;
        preferences.UndoHistory.Capture();

        // Act
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        int undone = settings.MaxBackupFiles;
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        int redone = settings.MaxBackupFiles;
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");

        // Assert
        undone.Should().Be(25);
        redone.Should().Be(42);
        settings.MaxBackupFiles.Should().Be(25);
        viewModel.ProjectHistory.Should().BeSameAs(preferences.UndoHistory);
        viewModel.HasProjectMenu.Should().BeFalse();
        viewModel.ProjectMenuItems.Should().BeEmpty();
        viewModel.SaveProjectCommand.CanExecute(null).Should().BeFalse();
        viewModel.OpenProjectCommand.CanExecute(null).Should().BeFalse();
        viewModel.NewProjectCommand.CanExecute(null).Should().BeFalse();
        window.GetVisualDescendants().OfType<MenuItem>()
            .Should().NotContain(item => Equals(item.Header, "_Edit"));
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The feature factory is used only before the Preferences view model is disposed.")]
    public async Task MainWindow_AfterPreferencesPathLosesFocus_UndoesAndRedoesTheWholeTextEdit()
    {
        // Arrange
        DesktopApplicationSettings settings = new() { OsuPath = @"C:\osu!" };
        using PreferencesViewModel preferences = new(settings, new TestFilePicker(),
            new StubThemeService(), new UserNotificationService(), new QuickRunCommandRegistry(),
            new TestHotkeyBindingCoordinator(), new TestBetterSaveOverrideService());
        await using var viewModel = CreateMainViewModel(
            [Registration("preferences", "Preferences", () => preferences)], settings: settings);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        TextBox path = window.GetVisualDescendants().OfType<PreferencesView>().Single()
            .GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Text == @"C:\osu!");

        // Act
        path.Focus();
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText(@"D:\Games\");
        host.TypeText("osu!");
        host.Find<TextBox>("ToolSearchBox").Focus();
        HeadlessViewHost.RunDispatcherJobs();
        string edited = settings.OsuPath;
        host.PressKey(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        string undone = settings.OsuPath;
        string undoneText = path.Text!;
        bool canUndoAgain = preferences.UndoHistory.CanUndo;
        host.PressKey(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");

        // Assert
        edited.Should().Be(@"D:\Games\osu!");
        undone.Should().Be(@"C:\osu!");
        undoneText.Should().Be(@"C:\osu!");
        canUndoAgain.Should().BeFalse();
        settings.OsuPath.Should().Be(@"D:\Games\osu!");
        path.Text.Should().Be(@"D:\Games\osu!");
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Command events are observed only during the view model's lifetime.")]
    public async Task InitializeAsync_WithDelayedRecovery_SubscribesToInitializedHistory()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        TaskCompletionSource<StubProject> recovery = new();
        await using var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)],
            projectService: new RecordingProjectService { RecoveryProject = recovery.Task },
            projectSerializer: new VersionedProjectJsonSerializer(),
            initialize: false);
        List<bool> undoAvailability = [];
        List<bool> redoAvailability = [];
        viewModel.UndoCommand.CanExecuteChanged += (_, _) =>
            undoAvailability.Add(viewModel.UndoCommand.CanExecute(null));
        viewModel.RedoCommand.CanExecuteChanged += (_, _) =>
            redoAvailability.Add(viewModel.RedoCommand.CanExecute(null));

        // Act
        Task initialization = viewModel.InitializeAsync();
        bool loadingDuringRecovery = viewModel.IsFeatureLoading;
        recovery.SetResult(new StubProject { Value = 42 });
        await initialization;
        bool recoveryWasRecorded = project.UndoHistory!.CanUndo;
        undoAvailability.Clear();
        redoAvailability.Clear();
        project.Value = 43;
        project.UndoHistory.Undo();

        // Assert
        loadingDuringRecovery.Should().BeTrue();
        recoveryWasRecorded.Should().BeFalse();
        viewModel.IsFeatureLoading.Should().BeFalse();
        viewModel.CurrentFeature.Should().BeSameAs(project);
        project.Value.Should().Be(42);
        undoAvailability.Should().Equal(true, false);
        redoAvailability.Should().Equal(false, true);
    }

    [TestMethod]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "The event counter is intentionally reset between synchronous edits.")]
    public async Task OnCurrentFeatureChanged_AfterSwitch_ObservesOnlyActiveHistory()
    {
        // Arrange
        StubProjectFeatureViewModel first = new();
        StubProjectFeatureViewModel second = new();
        await using var viewModel = CreateMainViewModel(
            [
                Registration("first", "First", () => first),
                Registration("second", "Second", () => second),
            ],
            projectSerializer: new VersionedProjectJsonSerializer());
        int notifications = 0;
        viewModel.UndoCommand.CanExecuteChanged += (_, _) => notifications++;

        // Act
        viewModel.SelectedFeature = viewModel.FeatureItems[1];
        notifications = 0;
        first.Value = 1;
        int inactiveNotifications = notifications;
        second.Value = 2;
        int activeNotifications = notifications;
        viewModel.SelectedFeature = viewModel.FeatureItems[0];
        notifications = 0;
        second.Value = 3;
        int switchedBackInactiveNotifications = notifications;
        first.Value = 4;

        // Assert
        inactiveNotifications.Should().Be(0);
        activeNotifications.Should().Be(1);
        switchedBackInactiveNotifications.Should().Be(0);
        notifications.Should().Be(1);
        viewModel.CurrentFeature.Should().BeSameAs(first);
    }

    [TestMethod]
    public async Task InitializeAsync_WhenRecoveryCompletesAfterSwitch_DoesNotSubscribeInactiveHistory()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        StubFeatureViewModel home = new();
        TaskCompletionSource<StubProject> recovery = new();
        await using var viewModel = CreateMainViewModel(
            [
                Registration("project", "Project", () => project),
                Registration("home", "Home", () => home),
            ],
            projectService: new RecordingProjectService { RecoveryProject = recovery.Task },
            projectSerializer: new VersionedProjectJsonSerializer(),
            initialize: false);
        Task initialization = viewModel.InitializeAsync();
        viewModel.SelectedFeature = viewModel.FeatureItems[1];
        int notifications = 0;
        viewModel.UndoCommand.CanExecuteChanged += (_, _) => notifications++;

        // Act
        recovery.SetResult(new StubProject { Value = 42 });
        await initialization;
        project.Value = 43;

        // Assert
        viewModel.CurrentFeature.Should().BeSameAs(home);
        viewModel.IsFeatureLoading.Should().BeFalse();
        project.UndoHistory!.CanUndo.Should().BeTrue();
        notifications.Should().Be(0);
    }

    [TestMethod]
    public async Task DisposeAsync_WithActiveUndoHistory_UnsubscribesCommandNotifications()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)],
            projectSerializer: new VersionedProjectJsonSerializer());
        int notifications = 0;
        viewModel.UndoCommand.CanExecuteChanged += (_, _) => notifications++;
        viewModel.RedoCommand.CanExecuteChanged += (_, _) => notifications++;

        // Act
        await viewModel.DisposeAsync();
        project.Value = 1;

        // Assert
        project.UndoHistory!.CanUndo.Should().BeTrue();
        notifications.Should().Be(0);
    }

    [TestMethod]
    public async Task UndoCommand_WhileMenuGestureIsOpen_UndoesCurrentProject()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        await using var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)]);
        ProjectUndoHistory<StubProject> history = new(project, new VersionedProjectJsonSerializer());
        project.UndoHistory = history;
        project.Value = 1;
        viewModel.UndoCommand.CanExecute(null).Should().BeTrue();

        // Act
        using (history.BeginGesture()) await viewModel.UndoCommand.ExecuteAsync(null);

        // Assert
        project.Value.Should().Be(0);
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public async Task MainWindow_ControlZAndY_UndoAndRedoCurrentProject()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        await using var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)]);
        ProjectUndoHistory<StubProject> history = new(project, new VersionedProjectJsonSerializer());
        project.UndoHistory = history;
        project.Value = 1;
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);

        // Act
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        int undone = project.Value;
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");

        // Assert
        undone.Should().Be(0);
        project.Value.Should().Be(1);
    }

    [TestMethod]
    public async Task MainWindow_SearchTypingAndEnter_ActivatesHighlightedFeature()
    {
        // Arrange
        StubFeatureViewModel first = new();
        StubFeatureViewModel timing = new();
        await using var viewModel = CreateMainViewModel(
        [
            Registration("first", "First", () => first),
            Registration("timing", "Timing copier", () => timing),
        ]);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        TextBox search = host.Find<TextBox>("ToolSearchBox");
        host.Click(search);

        // Act
        host.TypeText("Timing");
        host.PressKey(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");

        // Assert
        viewModel.SearchText.Should().Be("Timing");
        viewModel.SelectedFeature.Should().BeSameAs(viewModel.FeatureItems.Single(item => item.Id == "timing"));
        timing.ActivationCount.Should().Be(1);
        first.ActivationCount.Should().Be(1);
    }

    [TestMethod]
    public async Task MainWindow_FavoriteNavigationItems_AppearAlphabeticallyAndActivateOnClick()
    {
        // Arrange
        var zulu = new StubFeatureViewModel();
        var bravo = new StubFeatureViewModel();
        DesktopApplicationSettings settings = new() { FavoriteTools = ["zulu", "bravo"] };
        await using var viewModel = CreateMainViewModel(
        [
            Registration("zulu", "Zulu", () => zulu),
            Registration("bravo", "Bravo", () => bravo),
        ], settings);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        NavigationListBox navigation = host.Find<NavigationListBox>("ToolList");
        ListBoxItem[] favoriteItems = navigation.GetVisualDescendants().OfType<ListBoxItem>()
            .Where(item => item.DataContext is ShellFeatureItemViewModel feature && feature.IsFavorite)
            .ToArray();
        ListBoxItem zuluItem = favoriteItems.Single(item => ((ShellFeatureItemViewModel)item.DataContext!).Id == "zulu");

        // Act
        host.Click(zuluItem);
        await HeadlessViewHost.DrainAsync(() => ReferenceEquals(viewModel.CurrentFeature, zulu)
                                               && !viewModel.IsFeatureLoading);

        // Assert
        favoriteItems.Select(item => ((ShellFeatureItemViewModel)item.DataContext!).DisplayName)
            .Should().Equal("Bravo", "Zulu");
        viewModel.SelectedFeature.Should().BeSameAs(viewModel.FeatureItems.Single(item => item.Id == "zulu"));
        zulu.ActivationCount.Should().Be(1);
    }

    [TestMethod]
    public async Task MainWindow_ClosedNavigationDrawer_AllowsClickingActiveFeature()
    {
        // Arrange
        var feature = new FirstFeatureViewModel();
        await using var viewModel = CreateMainViewModel(
        [Registration("feature", "Feature", () => feature)]);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants()
            .OfType<FirstFeatureView>().Any());
        ToggleButton navigationToggle = window.GetVisualDescendants().OfType<ToggleButton>()
            .Single(toggle => toggle.Classes.Contains("navigation-toggle"));
        Button featureAction = window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Name == "FeatureAction");

        // Act
        host.Click(navigationToggle);
        bool drawerClosed = !viewModel.IsNavigationOpen;
        host.Click(featureAction);

        // Assert
        drawerClosed.Should().BeTrue();
        featureAction.Bounds.Width.Should().BeGreaterThan(0);
        feature.ActionCount.Should().Be(1);
    }

    [TestMethod]
    public async Task MainWindow_ProjectMenuUndoAndRedo_UseTheActiveProjectHistory()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        ProjectUndoHistory<StubProject> history = new(project, new VersionedProjectJsonSerializer());
        project.UndoHistory = history;
        project.Value = 1;
        await using var viewModel = CreateMainViewModel(
            [Registration("project", "Project", () => project)]);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        MenuItem projectMenu = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Project"));

        // Act
        host.Click(projectMenu);
        MenuItem undoItem = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Undo"));
        host.Click(undoItem);
        int undone = project.Value;
        host.Click(projectMenu);
        MenuItem redoItem = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Redo"));
        host.Click(redoItem);

        // Assert
        undone.Should().Be(0);
        project.Value.Should().Be(1);
        history.CanUndo.Should().BeTrue();
        history.CanRedo.Should().BeFalse();
    }

    [TestMethod]
    public async Task MainWindow_HitsoundPreviewGridNameEdit_UsesActiveProjectHistory()
    {
        // Arrange
        HitsoundPreviewHelperViewModel feature = HitsoundPreviewHelperViewModelTestFactory.CreateForShell();
        IShellProjectFeature<HitsoundPreviewHelperProject> projectFeature = feature;
        typeof(ObservableHitsoundZone).GetProperty(nameof(ObservableHitsoundZone.Name))!
            .IsDefined(typeof(UndoableAttribute), true).Should().BeTrue();
        await using var viewModel = CreateMainViewModelForFeature(
            Registration("hitsound-preview", "Hitsound Preview", () => feature),
            new VersionedProjectJsonSerializer());
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants().OfType<HitsoundPreviewHelperView>().Any());
        viewModel.ProjectHistory.Should().BeSameAs(feature.UndoHistory);

        // Act
        host.Click(host.Find<Control>("AddButton"));
        await HeadlessViewHost.DrainAsync(() => feature.UndoHistory?.CanUndo == true);
        ObservableHitsoundZone zone = feature.Items.Single();
        int itemCountAfterAdd = feature.Items.Count;
        string originalName = zone.Name;
        VersionedProjectJsonSerializer serializer = new();
        string serializedBeforeNameEdit = serializer.Serialize(projectFeature.ProjectDefinition.ConfigSchema, projectFeature.Snapshot());
        int zoneNameChanges = 0;
        zone.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ObservableHitsoundZone.Name)) zoneNameChanges++;
        };
        DataGrid grid = window.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell nameCell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, zone) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .First();
        host.Click(nameCell.GetVisualDescendants().OfType<TextBlock>().Single());
        TextBox editor = nameCell.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.IsVisible && textBox.Bounds.Width > 0);
        bool editorFocused = editor.Focus();
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("edited from the shell grid");
        host.Click(grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, zone) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .ElementAt(1));
        int zoneNameChangesAfterEdit = zoneNameChanges;
        string editedZoneName = projectFeature.Snapshot().Items.Single().Name;
        string serializedAfterNameEdit = serializer.Serialize(projectFeature.ProjectDefinition.ConfigSchema, projectFeature.Snapshot());
        bool serializedProjectChangedAfterEdit = serializedAfterNameEdit != serializedBeforeNameEdit;
        host.Click(window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Project")));
        host.Click(window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Undo")));
        string undoneName = feature.Items.Single().Name;
        await HeadlessViewHost.DrainAsync(() => grid.GetVisualDescendants().OfType<DataGridCell>()
            .Any(cell => cell.DataContext is ObservableHitsoundZone current
                         && current.Name == originalName
                         && cell.GetVisualDescendants().OfType<TextBlock>().Any(textBlock =>
                             textBlock.IsEffectivelyVisible && textBlock.Text == originalName)));
        host.Click(window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Project")));
        host.Click(window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Redo")));
        await HeadlessViewHost.DrainAsync(() => grid.GetVisualDescendants().OfType<DataGridCell>()
            .Any(cell => cell.DataContext is ObservableHitsoundZone current
                         && current.Name == "edited from the shell grid"
                         && cell.GetVisualDescendants().OfType<TextBlock>()
                             .Any(textBlock => textBlock.IsEffectivelyVisible
                                               && textBlock.Text == "edited from the shell grid")));

        // Assert
        itemCountAfterAdd.Should().Be(1);
        editorFocused.Should().BeTrue();
        zoneNameChangesAfterEdit.Should().BeGreaterThan(0);
        editedZoneName.Should().Be("edited from the shell grid");
        serializedProjectChangedAfterEdit.Should().BeTrue();
        undoneName.Should().Be(originalName);
        feature.Items.Single().Name.Should().Be("edited from the shell grid");
    }

    [TestMethod]
    [SuppressMessage(
        "ReSharper",
        "AccessToDisposedClosure",
        Justification = "The observer is used only while this test's view model is alive.")]
    public async Task MainWindow_SwitchingBetweenProjectFeatures_KeepsProjectMenuAvailable()
    {
        // Arrange
        var firstProject = new StubProjectFeatureViewModel();
        var secondProject = new StubProjectFeatureViewModel();
        await using var viewModel = CreateMainViewModel(
        [
            Registration("first-project", "First project", () => firstProject),
            Registration("second-project", "Second project", () => secondProject),
        ]);
        List<bool> menuAvailability = [];
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.HasProjectMenu))
                menuAvailability.Add(viewModel.HasProjectMenu);
        };
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        NavigationListBox navigation = host.Find<NavigationListBox>("ToolList");
        ListBoxItem secondProjectItem = navigation.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => item.DataContext is ShellFeatureItemViewModel feature
                            && feature.Id == "second-project");

        // Act
        host.Click(secondProjectItem);
        await HeadlessViewHost.DrainAsync(() => ReferenceEquals(viewModel.CurrentFeature, secondProject)
                                               && !viewModel.IsFeatureLoading);
        MenuItem projectMenu = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Project"));

        // Assert
        menuAvailability.Should().NotContain(false);
        viewModel.HasProjectMenu.Should().BeTrue();
        projectMenu.IsVisible.Should().BeTrue();
        projectMenu.IsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public async Task MainWindow_FileMenuOpenCurrentBeatmap_FetchesAndSelectsEditorPath()
    {
        // Arrange
        TestCurrentBeatmapDialogService currentBeatmap = new() { Path = "C:\\maps\\current.osu" };
        TestBeatmapWorkspace workspace = new();
        await using var viewModel = CreateMainViewModel(
            currentBeatmapDialog: currentBeatmap,
            beatmapWorkspace: workspace);
        MainWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        MenuItem fileMenu = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_File"));

        // Act
        host.Click(fileMenu);
        MenuItem openCurrentBeatmap = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Open current beatmap"));
        host.Click(openCurrentBeatmap);
        await HeadlessViewHost.DrainAsync(() => currentBeatmap.FetchCount == 1);

        // Assert
        currentBeatmap.FetchCount.Should().Be(1);
        workspace.SelectedPaths.Should().Equal("C:\\maps\\current.osu");
        workspace.LastSelectionSource.Should().Be(BeatmapSelectionSource.CurrentEditor);
    }

    [TestMethod]
    public async Task MainWindow_FileMenuOpenCurrentBeatmap_WhenLookupFails_ShowsOriginalProductionDialog()
    {
        // Arrange
        RecordingCurrentBeatmapLocator locator = new()
        {
            Failure = new InvalidOperationException("The editor state is unavailable."),
        };
        UserNotificationService notifications = new();
        CurrentBeatmapDialogService currentBeatmap = new(
            locator,
            new DialogService(),
            new PhysicalBeatmapsetFileSystem(),
            notifications);
        TestBeatmapWorkspace workspace = new();
        await using var viewModel = CreateMainViewModel(
            currentBeatmapDialog: currentBeatmap,
            beatmapWorkspace: workspace);
        MainWindow window = new() { DataContext = viewModel };
        IClassicDesktopStyleApplicationLifetime lifetime =
            Avalonia.Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("The headless test application has no classic desktop lifetime.");
        Window? previousMainWindow = lifetime.MainWindow;
        lifetime.MainWindow = window;
        HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);

        try
        {
            // Act
            OpenCurrentBeatmapFromFileMenu(host, window);
            Task openTask = viewModel.Workspace.OpenCurrentBeatmapCommand.ExecutionTask!;
            HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows.OfType<MessageDialog>()
                .Any(dialog => dialog.IsVisible));
            MessageDialog dialog = lifetime.Windows.OfType<MessageDialog>().Single(candidate => candidate.IsVisible);
            using HeadlessViewHost dialogHost = HeadlessViewHost.Attach(dialog);
            string displayedError = dialog.GetVisualDescendants().OfType<TextBlock>()
                .Single(textBlock => textBlock.Text == ApplicationStrings.Exception_LiveEditorUnavailable).Text!;
            dialogHost.Click(dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "OK"));
            HeadlessViewHost.PumpDispatcherUntil(() => openTask.IsCompleted);
            await openTask;

            // Assert
            displayedError.Should().Be(ApplicationStrings.Exception_LiveEditorUnavailable);
            dialog.Title.Should().Be(DesktopStrings.Shell_CurrentBeatmapUnavailable);
            locator.FindCount.Should().Be(1);
            workspace.SelectedPaths.Should().BeEmpty();
        }
        finally
        {
            host.Dispose();
            lifetime.MainWindow = previousMainWindow;
        }
    }

    [TestMethod]
    public async Task MainWindow_FileMenuOpenCurrentBeatmap_WhenSelectedFileIsMissing_ShowsWarningSnackbar()
    {
        // Arrange
        string missingPath = Path.Combine(Path.GetTempPath(), $"mapping-tools-missing-{Guid.NewGuid():N}.osu");
        RecordingCurrentBeatmapLocator locator = new(missingPath);
        UserNotificationService notifications = new();
        CurrentBeatmapDialogService currentBeatmap = new(
            locator,
            new DialogService(),
            new PhysicalBeatmapsetFileSystem(),
            notifications);
        TestBeatmapWorkspace workspace = new();
        await using var viewModel = CreateMainViewModel(
            currentBeatmapDialog: currentBeatmap,
            notifications: notifications,
            beatmapWorkspace: workspace);
        MainWindow window = new() { DataContext = viewModel };
        IClassicDesktopStyleApplicationLifetime lifetime =
            Avalonia.Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("The headless test application has no classic desktop lifetime.");
        Window? previousMainWindow = lifetime.MainWindow;
        lifetime.MainWindow = window;
        HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        NotificationPresenter presenter = new(notifications, new TestDialogService(), window);

        try
        {
            await presenter.StartAsync(CancellationToken.None);

            // Act
            OpenCurrentBeatmapFromFileMenu(host, window);
            Task openTask = viewModel.Workspace.OpenCurrentBeatmapCommand.ExecutionTask!;
            HeadlessViewHost.PumpDispatcherUntil(() => openTask.IsCompleted);
            await openTask;
            await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants().OfType<TextBlock>()
                .Any(textBlock => textBlock.IsEffectivelyVisible
                                  && textBlock.Text is { } text
                                  && text.Contains(missingPath, StringComparison.Ordinal)));

            // Assert
            locator.FindCount.Should().Be(1);
            workspace.SelectedPaths.Should().BeEmpty();
            window.GetVisualDescendants().OfType<TextBlock>()
                .Should().Contain(textBlock => textBlock.IsEffectivelyVisible
                                               && textBlock.Text != null
                                               && textBlock.Text.Contains(missingPath, StringComparison.Ordinal));
        }
        finally
        {
            try
            {
                await presenter.StopAsync(CancellationToken.None);
            }
            finally
            {
                try
                {
                    host.Dispose();
                }
                finally
                {
                    lifetime.MainWindow = previousMainWindow;
                }
            }
        }
    }

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

    [DataTestMethod]
    [DataRow("cop", "timing")]
    [DataRow("Get started", "get-started")]
    public void MainViewModel_SearchText_FiltersRegisteredFeatures(string searchText, string expectedFeatureId)
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
            [Registration("get-started", "Get started"), Registration("timing", "Timing copier")]);

        // Act
        viewModel.SearchText = searchText;
        string[] visibleFeatureIds = viewModel.VisibleFeatures.Select(item => item.Id).ToArray();

        // Assert
        visibleFeatureIds.Should().Equal(expectedFeatureId);
    }

    [TestMethod]
    public void MainViewModel_ClearSearchText_ShowsAllRegisteredFeatures()
    {
        // Arrange
        using var viewModel = CreateMainViewModel(
            [Registration("get-started", "Get started"), Registration("timing", "Timing copier")]);
        viewModel.SearchText = "cop";

        // Act
        viewModel.SearchText = string.Empty;
        string[] visibleFeatureIds = viewModel.VisibleFeatures.Select(item => item.Id).ToArray();

        // Assert
        visibleFeatureIds.Should().Equal("get-started", "timing");
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
        DesktopApplicationSettings settings = new();
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
    public void MainViewModel_WithToolsInUnsortedRegistrationOrder_SortsFavoritesAndToolsAlphabetically()
    {
        // Arrange
        DesktopApplicationSettings settings = new()
        {
            FavoriteTools = ["zulu", "bravo"],
        };
        using var viewModel = CreateMainViewModel(
        [
            Registration("zulu", "Zulu"),
            Registration("alpha", "Alpha"),
            Registration("charlie", "Charlie"),
            Registration("bravo", "Bravo"),
        ], settings);

        // Act
        string[] visibleToolIds = viewModel.VisibleFeatures.Select(item => item.Id).ToArray();

        // Assert
        visibleToolIds.Should().Equal("bravo", "zulu", "alpha", "charlie");
    }

    [TestMethod]
    public void MainViewModel_WithFoundationalFavoritesAndTools_GroupsItemsWithInertDividers()
    {
        // Arrange
        DesktopApplicationSettings settings = new()
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
    public void MainViewModel_ActivatingDifferentFeature_DoesNotAutoSavePreviousProject()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        RecordingProjectService projectService = new();
        using var viewModel = CreateMainViewModel(
        [
            Registration("home", "Home"),
            Registration("project", "Project", () => project),
        ], projectService: projectService);
        var projectItem = viewModel.FeatureItems.Single(item => item.Id == "project");
        var homeItem = viewModel.FeatureItems.Single(item => item.Id == "home");
        projectItem.ActivateCommand.Execute(null);

        // Act
        homeItem.ActivateCommand.Execute(null);

        // Assert
        projectService.AutoSaveCount.Should().Be(0);
    }

    [TestMethod]
    public void MainViewModel_Dispose_AutoSavesAllActivatedProjectFeatures()
    {
        // Arrange
        StubProjectFeatureViewModel first = new();
        StubProjectFeatureViewModel second = new();
        RecordingProjectService projectService = new();
        var viewModel = CreateMainViewModel(
        [
            Registration("home", "Home"),
            Registration("first", "First project", () => first),
            Registration("second", "Second project", () => second),
        ], projectService: projectService);
        viewModel.FeatureItems.Single(item => item.Id == "first").ActivateCommand.Execute(null);
        viewModel.FeatureItems.Single(item => item.Id == "second").ActivateCommand.Execute(null);

        // Act
        viewModel.Dispose();

        // Assert
        projectService.AutoSaveCount.Should().Be(2);
    }

    [TestMethod]
    public void MainViewModel_SuppressProjectAutosave_BeforeDispose_DoesNotAutoSaveProjects()
    {
        // Arrange
        StubProjectFeatureViewModel project = new();
        RecordingProjectService projectService = new();
        var viewModel = CreateMainViewModel(
        [
            Registration("home", "Home"),
            Registration("project", "Project", () => project),
        ], projectService: projectService);
        viewModel.FeatureItems.Single(item => item.Id == "project").ActivateCommand.Execute(null);

        // Act
        viewModel.SuppressProjectAutosave();
        viewModel.Dispose();

        // Assert
        projectService.AutoSaveCount.Should().Be(0);
    }

    [TestMethod]
    public void MainViewModel_ActivatesQuickRunFeature_UpdatesCurrentRegistryTool()
    {
        // Arrange
        QuickRunCommandRegistry quickRunRegistry = new();
        quickRunRegistry.Register(new QuickRunCommand(
            "quick",
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
        string? commandIdAfterQuickToolActivation = quickRunRegistry.CurrentCommandId;

        ordinaryItem.ActivateCommand.Execute(null);
        string? commandIdAfterOrdinaryToolActivation = quickRunRegistry.CurrentCommandId;

        // Assert
        commandIdAfterQuickToolActivation.Should().Be("quick");
        commandIdAfterOrdinaryToolActivation.Should().BeNull();
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
    public void MainViewModel_Constructor_DoesNotCreateInitialFeature()
    {
        // Arrange
        int factoryCalls = 0;

        // Act
        using var viewModel = CreateMainViewModel(
            [
                Registration("first", "First", () =>
                {
                    factoryCalls++;
                    return new StubFeatureViewModel();
                }),
            ],
            initialize: false);

        // Assert
        factoryCalls.Should().Be(0);
        viewModel.CurrentFeature.Should().BeNull();
        viewModel.IsFeatureLoading.Should().BeTrue();
    }

    [TestMethod]
    public async Task MainViewModel_InitializeAsync_CreatesAndActivatesInitialFeature()
    {
        // Arrange
        StubFeatureViewModel feature = new();
        await using var viewModel = CreateMainViewModel(
            [Registration("first", "First", () => feature)],
            initialize: false);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.CurrentFeature.Should().BeSameAs(feature);
        feature.ActivationCount.Should().Be(1);
        viewModel.IsFeatureLoading.Should().BeFalse();
        viewModel.FeatureLoadError.Should().BeNull();
    }

    [TestMethod]
    public async Task MainViewModel_InitializeAsync_WithPendingLegacyMigration_DoesNotTriggerMigration()
    {
        // Arrange
        StubFeatureViewModel feature = new();
        TestDialogService dialogs = new() { BooleanResult = true };
        RecordingMigrationService migration = new();
        await using var viewModel = CreateMainViewModel(
            [Registration("first", "First", () => feature)],
            dialogs: dialogs,
            migrationService: migration,
            initialize: false);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        migration.CopyCount.Should().Be(0);
        dialogs.MessageCount.Should().Be(0);
        viewModel.CurrentFeature.Should().BeSameAs(feature);
    }

    [TestMethod]
    public async Task MainViewModel_SwitchingBetweenProjectFeatures_PreservesMenuVisibilityWhileLoading()
    {
        // Arrange
        QueuedTestDispatcher dispatcher = new();
        await using var viewModel = CreateMainViewModel(
            [
                Registration("first", "First", () => new StubProjectFeatureViewModel()),
                Registration("second", "Second", () => new StubProjectFeatureViewModel()),
            ],
            dispatcher: dispatcher,
            initialize: false);
        var initialActivation = viewModel.InitializeAsync();
        dispatcher.RunAll();
        await initialActivation;
        var second = viewModel.FeatureItems.Single(item => item.Id == "second");

        // Act
        second.ActivateCommand.Execute(null);
        bool isLoadingWhileSecondFeatureIsQueued = viewModel.IsFeatureLoading;
        bool hasProjectMenuWhileSecondFeatureIsQueued = viewModel.HasProjectMenu;

        dispatcher.RunAll();
        bool isLoadingAfterSecondFeatureActivation = viewModel.IsFeatureLoading;
        bool hasProjectMenuAfterSecondFeatureActivation = viewModel.HasProjectMenu;

        // Assert
        isLoadingWhileSecondFeatureIsQueued.Should().BeTrue();
        hasProjectMenuWhileSecondFeatureIsQueued.Should().BeTrue();
        isLoadingAfterSecondFeatureActivation.Should().BeFalse();
        viewModel.CurrentFeature.Should().BeOfType<StubProjectFeatureViewModel>();
        hasProjectMenuAfterSecondFeatureActivation.Should().BeTrue();
    }

    [TestMethod]
    public async Task MainViewModel_FeatureFactoryThrows_ExposesRecoverableLoadingError()
    {
        // Arrange
        await using var viewModel = CreateMainViewModel(
            [Registration("broken", "Broken", () => throw new InvalidOperationException("factory failed"))],
            initialize: false);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.CurrentFeature.Should().BeNull();
        viewModel.IsFeatureLoading.Should().BeFalse();
        viewModel.HasProjectMenu.Should().BeFalse();
        viewModel.FeatureLoadError.Should().Be(ApplicationStrings.Exception_UnexpectedFailure);
        viewModel.FeatureLoadDetails.Should().Contain("factory failed");
    }

    [TestMethod]
    public void MainViewModel_SupersededActivation_DoesNotPublishTheStaleFeature()
    {
        // Arrange
        QueuedTestDispatcher dispatcher = new();
        int firstFactoryCalls = 0;
        StubFeatureViewModel second = new();
        using var viewModel = CreateMainViewModel(
            [
                Registration("first", "First", () =>
                {
                    firstFactoryCalls++;
                    return new StubFeatureViewModel();
                }),
                Registration("second", "Second", () => second),
            ],
            dispatcher: dispatcher,
            initialize: false);

        // Act
        _ = viewModel.InitializeAsync();
        viewModel.FeatureItems.Single(item => item.Id == "second").ActivateCommand.Execute(null);
        dispatcher.RunAll();

        // Assert
        firstFactoryCalls.Should().Be(0);
        viewModel.CurrentFeature.Should().BeSameAs(second);
        second.ActivationCount.Should().Be(1);
        viewModel.IsFeatureLoading.Should().BeFalse();
    }

    [TestMethod]
    public async Task MainViewModel_OpenWebsiteCommand_WhenExecuted_OpensWebsite()
    {
        // Arrange
        RecordingPlatformLauncher launcher = new();
        await using var viewModel = CreateMainViewModel(launcher: launcher);

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
        RecordingPlatformLauncher launcher = new() { AcceptUris = false };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) =>
            published.Add(eventArgs.Notification);
        await using var viewModel = CreateMainViewModel(
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
    public async Task MainViewModel_OpenIssuesCommand_WhenExecuted_OpensGitHubIssuesPage()
    {
        // Arrange
        RecordingPlatformLauncher launcher = new();
        await using var viewModel = CreateMainViewModel(launcher: launcher);

        // Act
        await ExecuteAsync(viewModel.OpenIssuesCommand);

        // Assert
        launcher.OpenedUris.Should().ContainSingle()
            .Which.Should().Be(new Uri("https://github.com/OliBomby/Mapping_Tools/issues"));
    }

    [TestMethod]
    public async Task MainViewModel_OpenDonateCommand_WhenExecuted_OpensLegacyDonationPage()
    {
        // Arrange
        RecordingPlatformLauncher launcher = new();
        await using var viewModel = CreateMainViewModel(launcher: launcher);

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
        await using var viewModel = CreateMainViewModel(dialogs: dialogs);

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
        await using var viewModel = CreateMainViewModel(betterSave: betterSave);

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
        await using var viewModel = CreateMainViewModel(
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
        await using var viewModel = CreateMainViewModel(
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
        DesktopApplicationSettings? settings = null,
        IUserNotificationService? notifications = null,
        IPlatformLauncher? launcher = null,
        IBetterSaveService? betterSave = null,
        TestDialogService? dialogs = null,
        IQuickRunCommandRegistry? quickRunRegistry = null,
        RecordingProjectService? projectService = null,
        IProjectSerializer? projectSerializer = null,
        IUiDispatcher? dispatcher = null,
        IApplicationDataMigrationService? migrationService = null,
        ICurrentBeatmapDialogService? currentBeatmapDialog = null,
        TestBeatmapWorkspace? beatmapWorkspace = null,
        bool initialize = true)
    {
        var resolvedSettings = settings ?? new DesktopApplicationSettings();
        var resolvedNotifications = notifications ?? new UserNotificationService();
        var resolvedDialogs = dialogs ?? new TestDialogService();
        var resolvedQuickRunRegistry = quickRunRegistry ?? new QuickRunCommandRegistry();
        projectService ??= new RecordingProjectService();
        ImmediateTestDispatcher workspaceDispatcher = new();
        var resolvedWorkspace = beatmapWorkspace ?? new TestBeatmapWorkspace();
        BeatmapWorkspaceViewModel workspace = new(
            resolvedWorkspace,
            new TestBeatmapBackupService(),
            new TestQuickUndoCommandService(),
            new TestFilePicker(),
            new TestFileRevealService(),
            new TestApplicationDirectories(),
            resolvedSettings,
            new TestDialogService(),
            resolvedNotifications,
            workspaceDispatcher,
            currentBeatmapDialog ?? new TestCurrentBeatmapDialogService());
        MainViewModel viewModel = new(
            new ShellFeatureRegistry(registrations ?? [Registration("get-started", "Get started")]),
            resolvedQuickRunRegistry,
            resolvedSettings,
            resolvedNotifications,
            launcher ?? new RecordingPlatformLauncher(),
            workspace,
            betterSave ?? new TestBetterSaveService(),
            resolvedDialogs,
            new ProjectAutosaveCoordinator(
                projectService,
                resolvedDialogs,
                resolvedNotifications,
                serializer: projectSerializer),
            dispatcher ?? workspaceDispatcher,
            null,
            migrationService);
        if (initialize) viewModel.InitializeAsync().GetAwaiter().GetResult();
        return viewModel;
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
            horizontalScrollBarVisibility,
            verticalScrollBarVisibility);
    }

    private static MainViewModel CreateMainViewModelForFeature(
        ShellFeatureRegistration registration,
        IProjectSerializer projectSerializer)
    {
        return CreateMainViewModel([registration], projectSerializer: projectSerializer);
    }

    private static void OpenCurrentBeatmapFromFileMenu(HeadlessViewHost host, MainWindow window)
    {
        MenuItem fileMenu = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_File"));
        host.Click(fileMenu);
        MenuItem openCurrentBeatmap = window.GetVisualDescendants().OfType<MenuItem>()
            .Single(item => Equals(item.Header, "_Open current beatmap"));
        host.Click(openCurrentBeatmap);
    }

    private static Task ExecuteAsync(IAsyncRelayCommand command)
    {
        return command.ExecuteAsync(null);
    }

    private sealed class QueuedTestDispatcher : IUiDispatcher
    {
        private readonly Queue<Action> backgroundActions = new();

        public void Post(Action action)
        {
            action();
        }

        public void PostBackground(Action action)
        {
            backgroundActions.Enqueue(action);
        }

        public void RunAll()
        {
            while (backgroundActions.Count > 0) backgroundActions.Dequeue()();
        }
    }

    private sealed class StubThemeService : IApplicationThemeService
    {
        public void Apply(ApplicationTheme theme)
        {
        }
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
        public Task RunQuickAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class StubProjectFeatureViewModel : ObservableObject, IShellProjectFeature<StubProject>
    {
        public IProjectUndoHistory? UndoHistory { get; set; }

        private static readonly ProjectDefinition<StubProject> definition = new(
            "stubproject.json",
            "Stub Projects",
            static () => new StubProject());

        public int InstallCount { get; private set; }

        [Undoable]
        public int Value
        {
            get;
            set => SetProperty(ref field, value);
        }

        public ProjectDefinition<StubProject> ProjectDefinition => definition;

        public StubProject Snapshot()
        {
            return new StubProject { Value = Value };
        }

        public void Install(StubProject project)
        {
            InstallCount++;
            Value = project.Value;
        }
    }

    private sealed record StubProject
    {
        public int Value { get; init; }
    }

    private sealed class RecordingProjectService : IProjectService
    {
        public Task<StubProject>? RecoveryProject { get; init; }

        public int SaveAsCount { get; private set; }

        public int AutoSaveCount { get; private set; }

        public int OpenCount { get; private set; }

        public int CreateNewCount { get; private set; }

        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
        {
            return Path.Combine(Path.GetTempPath(), definition.AutoSaveFileName);
        }

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
        {
            return Path.GetTempPath();
        }

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition)
        {
            CreateNewCount++;
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

        public async Task<TProject> LoadAutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            if (RecoveryProject is null) throw new FileNotFoundException();
            return (TProject)(object)await RecoveryProject;
        }

        public Task AutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            AutoSaveCount++;
            return Task.CompletedTask;
        }

        public Task<string?> SaveAsAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default)
        {
            SaveAsCount++;
            return Task.FromResult<string?>(null);
        }

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult<ProjectOpenResult<TProject>?>(null);
        }
    }

    private sealed class RecordingMigrationService : IApplicationDataMigrationService
    {
        public int CopyCount { get; private set; }
        public bool RequiresMigration => true;
        public ApplicationDataMigrationResult? LastMigrationResult { get; set; }

        public Task<ApplicationDataMigrationResult> CopyLegacyDataAsync(
            CancellationToken cancellationToken = default)
        {
            CopyCount++;
            ApplicationDataMigrationResult result = new(1, 2, 0);
            LastMigrationResult = result;
            return Task.FromResult(result);
        }
    }
}
