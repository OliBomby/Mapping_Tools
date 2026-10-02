using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.QuickRun;
using Mapping_Tools.Application.QuickRun.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels.Adapters;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Views;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Projects;
using Material.Styles.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundPreviewHelper.Views;

[TestClass]
public sealed class HitsoundPreviewHelperViewTests
{
    [TestMethod]
    public void NameCell_SingleClickAndTypedText_CommitsNameOnBlur()
    {
        // Arrange
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(new RecordingPreviewService());
        ObservableHitsoundZone zone = new() { Name = "before" };
        viewModel.Items.Add(zone);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        var rowCells = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(candidate => ReferenceEquals(candidate.DataContext, zone)
                                && candidate.IsVisible
                                && candidate.Bounds.Width > 0
                                && candidate.Bounds.Height > 0)
            .OrderBy(candidate => candidate.Bounds.X)
            .ToArray();
        DataGridCell nameCell = rowCells[0];
        TextBlock nameDisplay = nameCell.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == "before");

        // Act
        host.Click(nameDisplay);
        bool zoneSelectedAfterClick = zone.IsSelected;
        TextBox editor = nameCell.GetVisualDescendants().OfType<TextBox>()
            .First(textBox => textBox.IsVisible && textBox.Bounds.Width > 0);
        bool editorFocused = editor.Focus();
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("edited from the grid");
        host.Click(rowCells[1]);

        // Assert
        zoneSelectedAfterClick.Should().BeTrue();
        editorFocused.Should().BeTrue();
        zone.Name.Should().Be("edited from the grid");
        zone.Model.Name.Should().Be("edited from the grid");
    }

    [TestMethod]
    public void HitsoundCell_SingleClickAndKeyboardSelection_UpdatesZone()
    {
        // Arrange
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(new RecordingPreviewService());
        ObservableHitsoundZone zone = new() { Name = "zone" };
        viewModel.Items.Add(zone);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell hitsoundCell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, zone) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .ElementAt(4);
        var originalHitsound = zone.Hitsound;

        // Act
        host.Click(hitsoundCell);
        ComboBox editor = hitsoundCell.GetVisualDescendants().OfType<ComboBox>()
            .Single(comboBox => comboBox.IsVisible && comboBox.Bounds.Width > 0);
        bool editorFocused = editor.Focus();
        host.PressKey(Key.F4, RawInputModifiers.None, PhysicalKey.F4, "");
        bool dropDownOpenedWithF4 = editor.IsDropDownOpen;
        Popup dropdownPopup = editor.GetVisualDescendants().OfType<Popup>().Single();
        Control dropdownContent = dropdownPopup.Child
                                   ?? throw new InvalidOperationException("The hitsound dropdown has no content.");
        TopLevel dropdown = TopLevel.GetTopLevel(dropdownContent)
                           ?? throw new InvalidOperationException("The hitsound dropdown has no input root.");
        ComboBoxItem option = dropdown.GetVisualDescendants().OfType<ComboBoxItem>()
            .First(item => item.IsVisible
                           && item.Bounds.Width > 0
                           && item.DataContext is not null
                           && !Equals(item.DataContext, originalHitsound));
        Point popupPoint = option.TranslatePoint(new Point(option.Bounds.Width / 2, option.Bounds.Height / 2), dropdown)
                           ?? throw new InvalidOperationException("Could not locate the hitsound option.");
        dropdown.MouseMove(popupPoint);
        dropdown.MouseDown(popupPoint, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        dropdown.MouseUp(popupPoint, MouseButton.Left);
        HeadlessViewHost.RunDispatcherJobs();
        host.Click(grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, zone) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .First());

        // Assert
        editorFocused.Should().BeTrue();
        dropDownOpenedWithF4.Should().BeTrue();
        zone.Hitsound.Should().NotBe(originalHitsound);
    }

    [TestMethod]
    public async Task GridFocus_CtrlZAndCtrlYUseProjectHistory()
    {
        // Arrange
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(new RecordingPreviewService());
        ProjectUndoHistory<Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Models.HitsoundPreviewHelperProject> history =
            new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        ProjectUndoWindowInput.Attach(host.Window, () => history);

        // Act
        host.Click(host.Find<Control>("AddButton"));
        await HeadlessViewHost.DrainAsync(() => history.CanUndo);
        int itemCountAfterAdd = viewModel.Items.Count;
        ObservableHitsoundZone zone = viewModel.Items.Single();
        var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell cell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .First(candidate => ReferenceEquals(candidate.DataContext, zone)
                                && candidate.IsVisible
                                && candidate.Bounds.Width > 0
                                && candidate.Bounds.Height > 0);
        host.Click(cell);
        host.Window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        int undoneCount = viewModel.Items.Count;
        host.Window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");

        // Assert
        itemCountAfterAdd.Should().Be(1);
        undoneCount.Should().Be(0);
        viewModel.Items.Should().ContainSingle();
    }

    [TestMethod]
    public async Task QuickRunService_WhenCurrentToolSucceeds_ShowsSuccessThroughMainWindowSnackbar()
    {
        // Arrange
        UserNotificationService notifications = new();
        RecordingPreviewService preview = new();
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(preview, notifications: notifications);
        QuickRunCommandRegistry registry = new();
        registry.Register(new QuickRunCommand(
            "hitsound-preview",
            "Hitsound Preview Helper",
            QuickRunTargets.Always,
            viewModel.RunQuickAsync));
        registry.SelectCurrent("hitsound-preview");
        QuickRunService quickRun = new(
            registry,
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new ApplicationSettings { SmartQuickRunEnabled = false },
            notifications);
        MainWindow window = new();
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        NotificationPresenter presenter = new(notifications, new TestDialogService(), window);

        try
        {
            await presenter.StartAsync(CancellationToken.None);

            // Act
            Task<QuickRunResult> quickRunTask = quickRun.RunAsync();
            HeadlessViewHost.PumpDispatcherUntil(() => quickRunTask.IsCompleted);
            var result = await quickRunTask;
            await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants().OfType<TextBlock>()
                .Any(textBlock => textBlock.IsEffectivelyVisible
                                  && textBlock.Text == "Hitsound Preview Helper: Placed 0 preview hitsounds."));

            // Assert
            result.Status.Should().Be(QuickRunStatus.Executed);
            preview.LastPaths.Should().Equal("current.osu");
            preview.LastQuickRun.Should().BeTrue();
            window.GetVisualDescendants().OfType<TextBlock>()
                .Should().Contain(textBlock => textBlock.IsEffectivelyVisible
                                               && textBlock.Text == "Hitsound Preview Helper: Placed 0 preview hitsounds.");
        }
        finally
        {
            await presenter.StopAsync(CancellationToken.None);
        }
    }

    [TestMethod]
    public void AddButton_ShiftClick_AddsDistinctSelectedLivePositions()
    {
        // Arrange
        RecordingPreviewService preview = new()
        {
            Positions = [new Vector2(64, 192), new Vector2(256, 192)],
        };
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(preview);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Control>("AddButton"), KeyModifiers.Shift);

        // Assert
        viewModel.Items.Select(item => (item.XPos, item.YPos))
            .Should().Equal((64d, 192d), (256d, 192d));
    }

    [TestMethod]
    public void AddButton_NormalClick_AddsOneDefaultZone()
    {
        // Arrange
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(new RecordingPreviewService());
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Control>("AddButton"));

        // Assert
        viewModel.Items.Should().ContainSingle();
        viewModel.Items[0].XPos.Should().Be(-1);
        viewModel.Items[0].YPos.Should().Be(-1);
    }

    [TestMethod]
    public async Task AddButton_ShiftClick_WhenLocatorCannotFindBeatmap_ShowsOneInformativeErrorAndAddsNothing()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        TestDialogService dialogs = new();
        CurrentBeatmapDialogService currentBeatmap = CreateCurrentBeatmapService(
            new RecordingCurrentBeatmapLocator(),
            dialogs,
            new RecordingBeatmapsetFileSystem { FileExistsResult = true },
            notifications);
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(new RecordingPreviewService(), currentBeatmap, notifications);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Control>("AddButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => dialogs.MessageCount > 0 || published.Count > 0);

        // Assert
        viewModel.Items.Should().BeEmpty();
        dialogs.MessageCount.Should().Be(1);
        dialogs.LastMessage.Should().Contain("Open a beatmap in osu!");
        published.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AddButton_ShiftClick_WhenReportedBeatmapFileIsMissing_PublishesOneWarningAndAddsNothing()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        TestDialogService dialogs = new();
        CurrentBeatmapDialogService currentBeatmap = CreateCurrentBeatmapService(
            new RecordingCurrentBeatmapLocator("missing.osu"),
            dialogs,
            new RecordingBeatmapsetFileSystem { FileExistsResult = false },
            notifications);
        RecordingPreviewService preview = new();
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(preview, currentBeatmap, notifications);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Control>("AddButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => published.Count > 0);

        // Assert
        viewModel.Items.Should().BeEmpty();
        dialogs.MessageCount.Should().Be(0);
        var notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Warning);
        notification.Title.Should().Be("Current beatmap is missing");
        notification.Message.Should().Contain("missing.osu");
        preview.PositionReadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task AddButton_ShiftClick_WhenEditorSelectionReadFails_PublishesOneErrorAndAddsNothing()
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        TestDialogService dialogs = new();
        CurrentBeatmapDialogService currentBeatmap = CreateCurrentBeatmapService(
            new RecordingCurrentBeatmapLocator("current.osu"),
            dialogs,
            new RecordingBeatmapsetFileSystem { FileExistsResult = true },
            notifications);
        Exception failure = new InvalidOperationException("reader failed");
        RecordingPreviewService preview = new() { Failure = failure };
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(preview, currentBeatmap, notifications);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Control>("AddButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => published.Count > 0);

        // Assert
        viewModel.Items.Should().BeEmpty();
        dialogs.MessageCount.Should().Be(0);
        var notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Error);
        notification.Exception.Should().BeSameAs(failure);
    }

    [TestMethod]
    public void RhythmGuideButton_Click_OpensSharedGuideWindow()
    {
        // Arrange
        RhythmGuideWindowService windows = new();
        HitsoundPreviewHelperViewModel viewModel = CreateViewModel(
            new RecordingPreviewService(),
            rhythmGuideWindowService: windows);
        HitsoundPreviewHelperView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        FloatingButton guideButton = view.GetVisualDescendants().OfType<FloatingButton>()
            .First(button => button.Command == viewModel.OpenRhythmGuideCommand);

        // Act
        host.Click(guideButton);

        // Assert
        windows.ShowCount.Should().Be(1);
        windows.ViewModel.Should().NotBeNull();
    }

    private static HitsoundPreviewHelperViewModel CreateViewModel(
        RecordingPreviewService preview,
        ICurrentBeatmapDialogService? currentBeatmapService = null,
        IUserNotificationService? notifications = null,
        RhythmGuideWindowService? rhythmGuideWindowService = null)
    {
        IUserNotificationService effectiveNotifications = notifications ?? new UserNotificationService();
        ICurrentBeatmapDialogService effectiveCurrentBeatmap = currentBeatmapService
            ?? new TestCurrentBeatmapDialogService { Path = "current.osu" };
        ToolExecutionService execution = new(effectiveNotifications, TimeProvider.System);
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "current.osu" };
        RhythmGuideWindowService windows = rhythmGuideWindowService ?? new();
        RhythmGuideViewModel rhythmGuide = new(
            new StubRhythmGuideService(),
            execution,
            new TestFilePicker(),
            new TestFileRevealService(),
            effectiveCurrentBeatmap,
            workspace,
            windows,
            new TestApplicationDirectories());
        return new HitsoundPreviewHelperViewModel(
            preview,
            execution,
            workspace,
            effectiveCurrentBeatmap,
            new DesktopApplicationSettings(),
            effectiveNotifications,
            windows,
            rhythmGuide,
            new TestApplicationDirectories());
    }

    private static CurrentBeatmapDialogService CreateCurrentBeatmapService(
        ICurrentBeatmapLocator locator,
        TestDialogService dialogs,
        IBeatmapsetFileSystem fileSystem,
        IUserNotificationService notifications)
    {
        return new CurrentBeatmapDialogService(locator, dialogs, fileSystem, notifications);
    }

    private sealed class RecordingPreviewService : IHitsoundPreviewHelperService
    {
        public IReadOnlyList<Vector2> Positions { get; init; } = [];

        public Exception? Failure { get; init; }

        public int PositionReadCount { get; private set; }

        public IReadOnlyList<string> LastPaths { get; private set; } = [];

        public bool LastQuickRun { get; private set; }

        public Task<HitsoundPreviewHelperResult> ApplyAsync(
            IReadOnlyList<string> paths,
            HitsoundPreviewHelperServiceOptions options,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastPaths = paths;
            LastQuickRun = quickRun;
            return Task.FromResult(new HitsoundPreviewHelperResult(paths, 0));
        }

        public Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            PositionReadCount++;
            return Failure is null
                ? Task.FromResult(Positions)
                : Task.FromException<IReadOnlyList<Vector2>>(Failure);
        }
    }

    private sealed class StubRhythmGuideService : IRhythmGuideService
    {
        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideServiceOptions.RhythmGuideRunOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RhythmGuideResult(options.ExportPath, 0, options.ExportMode));
        }
    }

    private sealed class RhythmGuideWindowService : IRhythmGuideWindowService
    {
        public RhythmGuideViewModel? ViewModel { get; private set; }

        public int ShowCount { get; private set; }

        public void Show(RhythmGuideViewModel viewModel)
        {
            ShowCount++;
            ViewModel = viewModel;
        }
    }

    private sealed class RecordingBeatmapsetFileSystem : IBeatmapsetFileSystem
    {
        public bool FileExistsResult { get; init; }

        public bool FileExists(string path) => FileExistsResult;
        public bool DirectoryExists(string path) => false;
        public string ReadAllText(string path) => string.Empty;
        public void WriteAllText(string path, string text) { }
        public void Delete(string path) { }
        public string GetParentFolder(string path) => Path.GetDirectoryName(path) ?? string.Empty;
        public string CombinePath(string parent, string child) => Path.Combine(parent, child);
        public string? GetParentDirectory(string filePath) => Path.GetDirectoryName(filePath);
        public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly) => [];
        public void EnsureDirectoryExists(string path) { }
        public byte[] ReadAllBytes(string path) => [];
        public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes, bool overwrite = false) { }
        public void CopyFile(string sourcePath, string destinationPath, bool overwrite = false) { }
        public void MoveFile(string sourcePath, string destinationPath, bool overwrite = false) { }
        public IBeatmapsetFileTransaction BeginTransaction(string targetDirectory) => throw new NotSupportedException();
    }
}
