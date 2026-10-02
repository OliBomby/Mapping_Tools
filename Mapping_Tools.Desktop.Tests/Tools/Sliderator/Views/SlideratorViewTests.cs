using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tools.Sliderator.Contracts;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.Sliderator.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Controls.Graph;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Desktop.Tools.Sliderator.ViewModels;
using Mapping_Tools.Desktop.Tools.Sliderator.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.Sliderator.Views;

[TestClass]
public sealed class SlideratorViewTests
{
    [TestMethod]
    public async Task ImportButton_Undo_RestoresWholePreImportProjectInOneStep()
    {
        // Arrange
        HitObject importedSlider = DecodeHitObject("64,64,0,2,0,L|164:64,1,100");
        RecordingSliderator service = new() { ImportedSliders = [importedSlider] };
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "source.osu" };
        SlideratorViewModel viewModel = CreateViewModel(service, workspace: workspace);
        viewModel.GraphBeats = 4;
        viewModel.BeatsPerMinute = 210;
        viewModel.ExportTime = 321;
        double originalBeatLength = viewModel.GraphDuration;
        viewModel.ExportAsNormal = false;
        viewModel.ExportAsStream = true;
        GraphState originalGraph = viewModel.GraphState.Clone();
        ProjectUndoHistory<Mapping_Tools.Desktop.Tools.Sliderator.Models.SlideratorProject> history =
            new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        Button importButton = view.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Import sliders into the preview."));

        // Act
        host.Click(importButton);
        await HeadlessViewHost.DrainAsync(() => history.CanUndo);
        int loadedObjectsAfterImport = viewModel.LoadedHitObjects.Count;
        history.Undo();

        // Assert
        loadedObjectsAfterImport.Should().Be(1);
        viewModel.LoadedHitObjects.Should().BeEmpty();
        viewModel.GraphBeats.Should().Be(4);
        viewModel.BeatsPerMinute.Should().Be(210);
        viewModel.ExportTime.Should().Be(321);
        viewModel.GraphDuration.Should().Be(originalBeatLength);
        viewModel.ExportAsNormal.Should().BeFalse();
        viewModel.ExportAsStream.Should().BeTrue();
        viewModel.GraphState.Anchors.Select(anchor => anchor.Pos).Should().Equal(originalGraph.Anchors.Select(anchor => anchor.Pos));
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void MinDendriteTextBox_TypedValue_UpdatesExpectedSegmentCount()
    {
        // Arrange
        SlideratorViewModel viewModel = CreateViewModel(new RecordingSliderator());
        viewModel.ManualVelocity = true;
        viewModel.NewVelocity = 10;
        long initialExpectedSegments = viewModel.ExpectedSegments;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        TextBox input = view.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => ToolTip.GetTip(textBox)?.ToString()?.StartsWith("Minimum length", StringComparison.Ordinal) == true);
        Button reset = view.GetVisualDescendants().OfType<Button>()
            .Single(button => ToolTip.GetTip(button)?.ToString() == "Reset graph.");

        // Act
        host.Click(input);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("4");
        host.Click(reset);

        // Assert
        viewModel.MinDendrite.Should().Be(4);
        viewModel.ExpectedSegments.Should().BeLessThan(initialExpectedSegments);
    }

    [TestMethod]
    public void ScaleCompleteButton_InVelocityMode_ScalesGraphUsingVelocityCompletion()
    {
        // Arrange
        TestDialogService dialogs = new() { ValueResult = 1d };
        SlideratorViewModel viewModel = CreateViewModel(new RecordingSliderator(), dialogs: dialogs);
        viewModel.GlobalSv = 0.7;
        viewModel.GraphModeSetting = SlideratorGraphMode.Velocity;
        viewModel.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 1)), new GraphAnchor(new Vector2(1, 1))],
            0,
            -10,
            1,
            10);
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Button scaleButton = view.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Scale graph to completion."));

        // Act
        host.Click(scaleButton);

        // Assert
        viewModel.GraphModeSetting.Should().Be(SlideratorGraphMode.Velocity);
        double completion = viewModel.GraphState.GetIntegral(0, viewModel.GraphBeats) * viewModel.SvGraphMultiplier;
        completion.Should().BeApproximately(1, 0.000001);
    }

    [TestMethod]
    public void RedAnchorCheckbox_Click_UpdatesGraphAndPreviewMarkers()
    {
        // Arrange
        SlideratorViewModel viewModel = CreateViewModel(new RecordingSliderator());
        HitObject sourceSlider = DecodeHitObject("64,64,0,2,0,L|264:64,1,200");
        sourceSlider.SetSliderPath(new SliderPath(
        [
            new PathControlPoint(new Vector2(64, 64), PathType.Bezier),
            new PathControlPoint(new Vector2(164, 64), PathType.Bezier),
            new PathControlPoint(new Vector2(264, 64), PathType.Linear),
        ]));
        viewModel.LoadedHitObjects.Add(sourceSlider);
        viewModel.VisibleHitObjectIndex = 0;
        viewModel.GraphModeSetting = SlideratorGraphMode.Position;
        Core.ToolHelpers.Sliders.SliderPathUtil.GetRedAnchorCompletions(
            viewModel.VisibleHitObject!.GetSliderPath()).Should().NotBeEmpty();
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        GraphControl graph = host.Find<GraphControl>("GraphControlElement");
        ObjectVisualiserControl preview = host.Find<ObjectVisualiserControl>("PreviewControl");
        CheckBox redAnchors = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => Equals(checkBox.Content, "Show red anchors"));
        CheckBox graphAnchors = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => Equals(checkBox.Content, "Show graph anchors"));
        redAnchors.IsVisible.Should().BeTrue();
        redAnchors.IsEnabled.Should().BeTrue();
        redAnchors.Bounds.Width.Should().BeGreaterThan(0);
        redAnchors.Bounds.Height.Should().BeGreaterThan(0);
        redAnchors.BringIntoView();
        HeadlessViewHost.RunDispatcherJobs();
        // Act
        host.Click(redAnchors, new Point(8, redAnchors.Bounds.Height / 2));
        bool showRedAnchorsAfterFirstClick = viewModel.ShowRedAnchors;
        int redGraphMarkers = graph.Markers.Count;
        int redPreviewMarkers = preview.ExtraMarkers.Count;
        host.Click(graphAnchors, new Point(8, graphAnchors.Bounds.Height / 2));
        int combinedPreviewMarkers = preview.ExtraMarkers.Count;

        // Assert
        showRedAnchorsAfterFirstClick.Should().BeTrue();
        redGraphMarkers.Should().BeGreaterThan(0);
        redPreviewMarkers.Should().BeGreaterThan(0);
        combinedPreviewMarkers.Should().BeGreaterThan(redPreviewMarkers);
        viewModel.ShowRedAnchors.Should().BeTrue();
        viewModel.ShowGraphAnchors.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(typeof(SingleCurveInterpolator3))]
    [DataRow(typeof(DoubleCurveInterpolator3))]
    [DataRow(typeof(WaveInterpolator))]
    public void GraphContextMenu_ChoosingInterpolator_ChangesAnchorAndUndoRestoresIt(Type interpolatorType)
    {
        // Arrange
        SlideratorViewModel viewModel = CreateViewModel(new RecordingSliderator());
        viewModel.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 0)), new GraphAnchor(new Vector2(1.5f, 0.5f)), new GraphAnchor(new Vector2(3, 1))],
            0,
            0,
            3,
            1);
        ProjectUndoHistory<Mapping_Tools.Desktop.Tools.Sliderator.Models.SlideratorProject> history =
            new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Window window = host.Window;
        ProjectUndoWindowInput.Attach(window, () => history);
        GraphControl graph = view.FindControl<GraphControl>("GraphControlElement")!;
        Point anchor = graph.TranslatePoint(graph.GetControlPosition(new Vector2(1.5f, 0.5f)), window)
                       ?? throw new InvalidOperationException("Graph anchor is not in the host window.");
        Type originalType = viewModel.GraphState.Anchors[1].Interpolator.GetType();
        ContextMenu? openedMenu = null;
        using IDisposable openedMenuSubscription = MenuBase.OpenedEvent.AddClassHandler<ContextMenu>(
            (menu, _) => openedMenu = menu);

        // Act
        OpenGraphContextMenu(host, anchor);
        ClickGraphContextMenuItem(openedMenu, item => Equals(item.Tag, interpolatorType));
        Type selectedType = viewModel.GraphState.Anchors[1].Interpolator.GetType();
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        Type undoneType = viewModel.GraphState.Anchors[1].Interpolator.GetType();
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        Type redoneType = viewModel.GraphState.Anchors[1].Interpolator.GetType();

        // Assert
        selectedType.Should().Be(interpolatorType);
        undoneType.Should().Be(originalType);
        redoneType.Should().Be(interpolatorType);
        history.CanUndo.Should().BeTrue();
    }

    [TestMethod]
    public void GraphContextMenu_DeleteAnchor_UndoRestoresAnchor()
    {
        // Arrange
        SlideratorViewModel viewModel = CreateViewModel(new RecordingSliderator());
        viewModel.GraphState = new GraphState(
            [new GraphAnchor(new Vector2(0, 0)), new GraphAnchor(new Vector2(1.5f, 0.5f)), new GraphAnchor(new Vector2(3, 1))],
            0,
            0,
            3,
            1);
        ProjectUndoHistory<Mapping_Tools.Desktop.Tools.Sliderator.Models.SlideratorProject> history =
            new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Window window = host.Window;
        ProjectUndoWindowInput.Attach(window, () => history);
        GraphControl graph = view.FindControl<GraphControl>("GraphControlElement")!;
        Point anchor = graph.TranslatePoint(graph.GetControlPosition(new Vector2(1.5f, 0.5f)), window)
                       ?? throw new InvalidOperationException("Graph anchor is not in the host window.");
        ContextMenu? openedMenu = null;
        using IDisposable openedMenuSubscription = MenuBase.OpenedEvent.AddClassHandler<ContextMenu>(
            (menu, _) => openedMenu = menu);

        // Act
        OpenGraphContextMenu(host, anchor);
        ClickGraphContextMenuItem(openedMenu, item => Equals(item.Header, "Delete"));
        int countAfterDelete = viewModel.GraphState.Anchors.Count;
        history.Undo();
        int countAfterUndo = viewModel.GraphState.Anchors.Count;

        // Assert
        countAfterDelete.Should().Be(2);
        countAfterUndo.Should().Be(3);
        history.CanUndo.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MoveRight_ShiftClick_RunsFastPlacementBeforeAdvancing(bool clickShift)
    {
        // Arrange
        RecordingSliderator service = new();
        SlideratorViewModel viewModel = CreateViewModel(service);
        AddTwoSliders(viewModel);
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("MoveRightButton"), clickShift ? KeyModifiers.Shift : KeyModifiers.None);
        await HeadlessViewHost.DrainAsync(() => viewModel.VisibleHitObjectIndex == 1 && !viewModel.IsRunning);

        // Assert
        service.RunCount.Should().Be(clickShift ? 1 : 0);
        viewModel.VisibleHitObjectIndex.Should().Be(1);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task MoveLeft_ShiftClick_RunsFastPlacementBeforeMovingBack(bool clickShift)
    {
        // Arrange
        RecordingSliderator service = new();
        SlideratorViewModel viewModel = CreateViewModel(service);
        AddTwoSliders(viewModel);
        viewModel.VisibleHitObjectIndex = 1;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("MoveLeftButton"), clickShift ? KeyModifiers.Shift : KeyModifiers.None);
        await HeadlessViewHost.DrainAsync(() => viewModel.VisibleHitObjectIndex == 0 && !viewModel.IsRunning);

        // Assert
        service.RunCount.Should().Be(clickShift ? 1 : 0);
        viewModel.VisibleHitObjectIndex.Should().Be(0);
    }

    [TestMethod]
    public async Task MoveRight_ShiftClick_WhenFastPlacementFails_DoesNotAdvance()
    {
        // Arrange
        RecordingSliderator service = new() { FailRun = true };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        SlideratorViewModel viewModel = CreateViewModel(service, notifications);
        AddTwoSliders(viewModel);
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("MoveRightButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => service.RunCount > 0 && !viewModel.IsRunning);

        // Assert
        service.RunCount.Should().Be(1);
        viewModel.VisibleHitObjectIndex.Should().Be(0);
        var notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Error);
        notification.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("placement failed");
    }

    [TestMethod]
    public async Task MoveLeft_ShiftClick_WhenFastPlacementFails_DoesNotAdvance()
    {
        // Arrange
        RecordingSliderator service = new() { FailRun = true };
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        SlideratorViewModel viewModel = CreateViewModel(service, notifications);
        AddTwoSliders(viewModel);
        viewModel.VisibleHitObjectIndex = 1;
        SlideratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("MoveLeftButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => service.RunCount > 0 && !viewModel.IsRunning);

        // Assert
        service.RunCount.Should().Be(1);
        viewModel.VisibleHitObjectIndex.Should().Be(1);
        var notification = published.Should().ContainSingle().Which;
        notification.Severity.Should().Be(UserNotificationSeverity.Error);
        notification.Exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("placement failed");
    }

    private static SlideratorViewModel CreateViewModel(
        RecordingSliderator service,
        UserNotificationService? notifications = null,
        TestBeatmapWorkspace? workspace = null,
        TestDialogService? dialogs = null)
    {
        return new SlideratorViewModel(
            service,
            new ToolExecutionService(notifications ?? new UserNotificationService(), TimeProvider.System),
            new TestCurrentBeatmapDialogService { Path = "current.osu" },
            workspace ?? new TestBeatmapWorkspace(),
            new DesktopApplicationSettings(),
            dialogs ?? new TestDialogService());
    }

    private static void OpenGraphContextMenu(HeadlessViewHost host, Point anchor)
    {
        Window window = host.Window;
        window.MouseMove(anchor);
        window.MouseDown(anchor, MouseButton.Right, RawInputModifiers.RightMouseButton);
        window.MouseUp(anchor, MouseButton.Right);
        HeadlessViewHost.RunDispatcherJobs();
    }

    private static void ClickGraphContextMenuItem(
        ContextMenu? menu,
        Func<MenuItem, bool> predicate)
    {
        ContextMenu openedMenu = menu
                                 ?? throw new InvalidOperationException("The graph context menu was not opened.");
        MenuItem item = openedMenu.Items.OfType<MenuItem>().Single(predicate);
        TopLevel popup = TopLevel.GetTopLevel(openedMenu)
                         ?? throw new InvalidOperationException("The graph context menu has no input root.");
        Point point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), popup)
                      ?? throw new InvalidOperationException("Could not locate the graph menu item.");
        popup.MouseMove(point);
        popup.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        popup.MouseUp(point, MouseButton.Left);
        HeadlessViewHost.RunDispatcherJobs();
    }

    private static void AddTwoSliders(SlideratorViewModel viewModel)
    {
        viewModel.LoadedHitObjects.Add(DecodeHitObject("64,64,0,2,0,L|164:64,1,100"));
        viewModel.LoadedHitObjects.Add(DecodeHitObject("164,64,1000,2,0,L|264:64,1,100"));
    }

    private sealed class RecordingSliderator : ISlideratorService
    {
        public IReadOnlyList<HitObject> ImportedSliders { get; init; } = [];

        public int RunCount { get; private set; }

        public bool FailRun { get; init; }

        public Task<SlideratorImportResult> ImportAsync(
            string path,
            HitObjectSelectionMode mode,
            string? timeCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SlideratorImportResult(ImportedSliders, 1.4, false, false));
        }

        public Task<SlideratorResult> RunAsync(
            string path,
            SlideratorServiceOptions project,
            HitObject sourceSlider,
            bool quickRun,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default,
            bool preferLiveEditor = true)
        {
            RunCount++;
            if (FailRun) return Task.FromException<SlideratorResult>(new InvalidOperationException("placement failed"));

            return Task.FromResult(new SlideratorResult(
                path,
                new SlideratorApplyResult(100, 1, false, 1),
                false));
        }
    }
}
