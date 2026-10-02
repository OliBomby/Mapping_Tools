using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Infrastructure.Projects;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.ComboColourStudio.Views;

[TestClass]
public sealed class ComboColourStudioViewTests
{
    [TestMethod]
    public void ImportDialogAcceptButton_WithBeatmapPath_ReturnsTrimmedPath()
    {
        // Arrange
        ComboColourStudioImportDialogViewModel viewModel = new(
            "  C:/Maps/map.osu  ",
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace(),
            new TestFilePicker());
        string? result = null;
        viewModel.Close = value => result = value;
        ComboColourStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(dialog);
        Button accept = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "ACCEPT");

        // Act
        host.Click(accept);

        // Assert
        result.Should().Be("C:/Maps/map.osu");
    }

    [TestMethod]
    public void ImportDialogCancelButton_DismissesWithoutImportPath()
    {
        // Arrange
        ComboColourStudioImportDialogViewModel viewModel = new(
            "C:/Maps/map.osu",
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace(),
            new TestFilePicker());
        string? result = "unchanged";
        viewModel.Close = value => result = value;
        ComboColourStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(dialog);
        Button cancel = host.Window.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "CANCEL");

        // Act
        host.Click(cancel);

        // Assert
        result.Should().BeNull();
        viewModel.Path.Should().Be("C:/Maps/map.osu");
    }

    [TestMethod]
    public async Task ImportColoursCommand_WhenSelectionExists_PrefillsPathWithoutFetchingEditorAndCancelPreservesProject()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["C:/Selected/map.osu"]);
        TestCurrentBeatmapDialogService currentBeatmap = new() { Path = "C:/Editor/current.osu" };
        StubComboColourStudioService studio = new();
        ComboColourStudioViewModel viewModel = new(
            studio,
            new ToolExecutionService(new UserNotificationService(), TimeProvider.System),
            new UserNotificationService(),
            workspace,
            currentBeatmap,
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new TestFilePicker());
        ComboColourStudioView view = new() { DataContext = viewModel };
        MainWindow window = new();
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        DialogHost dialogHost = window.GetVisualDescendants().OfType<DialogHost>().Single();
        dialogHost.Content = view;
        await HeadlessViewHost.DrainAsync(dialogHost.IsAttachedToVisualTree);

        // Act
        host.Click(view.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Import colours"));
        Task importTask = viewModel.ImportColoursCommand.ExecutionTask!;
        await HeadlessViewHost.DrainAsync(() => dialogHost.IsOpen);
        TextBox path = window.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.DataContext is ComboColourStudioImportDialogViewModel
                               && textBox.IsEffectivelyVisible
                               && textBox.Bounds.Width > 0);
        string prefilledPath = path.Text!;
        host.Click(dialogHost.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "CANCEL"));
        await importTask;

        // Assert
        prefilledPath.Should().Be("C:/Selected/map.osu");
        currentBeatmap.FetchCount.Should().Be(0);
        studio.ImportCount.Should().Be(0);
        viewModel.ImportPath.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RunButton_WhenExportSucceeds_ShowsSuccessThroughMainWindowSnackbar()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["C:/Selected/map.osu"]);
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, eventArgs) => published.Add(eventArgs.Notification);
        StubComboColourStudioService studio = new();
        ComboColourStudioViewModel viewModel = new(
            studio,
            new ToolExecutionService(notifications, TimeProvider.System),
            notifications,
            workspace,
            new TestCurrentBeatmapDialogService(),
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new TestFilePicker());
        ComboColourStudioView view = new() { DataContext = viewModel };
        MainWindow window = new();
        using HeadlessViewHost viewHost = HeadlessViewHost.Show(view);
        using HeadlessViewHost windowHost = HeadlessViewHost.ShowWindow(window);
        NotificationPresenter presenter = new(notifications, new TestDialogService(), window);

        try
        {
            await presenter.StartAsync(CancellationToken.None);
            Button run = view.GetVisualDescendants().OfType<ToolRunButton>()
                .Single().GetVisualDescendants().OfType<Button>().Single();

            // Act
            viewHost.Click(run);
            await HeadlessViewHost.DrainAsync(() => published.Count > 0);
            Task runTask = viewModel.RunCommand.ExecutionTask!;
            await HeadlessViewHost.DrainAsync(() => runTask.IsCompleted);
            await runTask;
            await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants().OfType<TextBlock>()
                .Any(textBlock => textBlock.IsEffectivelyVisible
                                  && textBlock.Text == "Combo Colour Studio: Successfully exported colours to 1 beatmap!"));

            // Assert
            studio.ApplyCount.Should().Be(1);
            published.Should().ContainSingle().Which.Message.Should().Be("Successfully exported colours to 1 beatmap!");
            window.GetVisualDescendants().OfType<TextBlock>()
                .Should().Contain(textBlock => textBlock.IsEffectivelyVisible
                                               && textBlock.Text == "Combo Colour Studio: Successfully exported colours to 1 beatmap!");
        }
        finally
        {
            await presenter.StopAsync(CancellationToken.None);
        }
    }

    [TestMethod]
    public void RunButton_WhenBurstLengthTextIsInvalid_BlocksExportAndPreservesValidValue()
    {
        // Arrange
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["C:/Selected/map.osu"]);
        UserNotificationService notifications = new();
        StubComboColourStudioService studio = new();
        ComboColourStudioViewModel viewModel = new(
            studio,
            new ToolExecutionService(notifications, TimeProvider.System),
            notifications,
            workspace,
            new TestCurrentBeatmapDialogService(),
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null),
            new TestFilePicker());
        int validBurstLength = viewModel.Project.MaxBurstLength;
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBox input = view.GetVisualDescendants().OfType<TextBox>().Single();
        ToolRunButton run = view.GetVisualDescendants().OfType<ToolRunButton>().Single();
        Button runButton = run.GetVisualDescendants().OfType<Button>().Single();

        // Act
        host.Click(input);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("not a number");
        string? typedTextBeforeRun = input.Text;
        bool runButtonFocused = runButton.Focus();
        host.Click(runButton);
        string? typedTextAfterRun = input.Text;

        // Assert
        DataValidationErrors.GetHasErrors(input).Should().BeTrue();
        typedTextBeforeRun.Should().Be("not a number");
        typedTextAfterRun.Should().Be("not a number");
        runButtonFocused.Should().BeTrue();
        viewModel.Project.MaxBurstLength.Should().Be(validBurstLength);
        studio.ApplyCount.Should().Be(0);
        runButton.Command!.CanExecute(null).Should().BeFalse();
    }

    [TestMethod]
    public void RemoveSequenceColourButton_WhenRepeatedColoursExist_RemovesClickedOccurrence()
    {
        // Arrange
        ComboColourStudioViewModel viewModel = CreateViewModel(
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null));
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddColourPointCommand.Execute(null);
        var point = viewModel.SelectedColourPoint!;
        var first = viewModel.ComboColours[0];
        var second = viewModel.ComboColours[1];
        viewModel.AddSequenceColour(point, first);
        viewModel.AddSequenceColour(point, second);
        viewModel.AddSequenceColour(point, first);
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        var cell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(candidate => ReferenceEquals(candidate.DataContext, point)
                                && candidate.IsVisible
                                && candidate.Bounds.Width > 0
                                && candidate.Bounds.Height > 0)
            .OrderBy(candidate => candidate.Bounds.X)
            .ElementAt(2);

        // Act
        host.DoubleClick(cell);
        ListBoxItem[] repeatedColourItems = view.GetVisualDescendants().OfType<ListBoxItem>()
            .Where(item => ReferenceEquals(item.DataContext, first)
                           && item.GetVisualAncestors().OfType<ItemsControl>()
                               .Any(itemsControl => ReferenceEquals(itemsControl.DataContext, point))
                           && item.IsVisible
                           && item.Bounds.Width > 0
                           && item.Bounds.Height > 0
                           && item.GetVisualDescendants().OfType<Button>().Any(button => button.IsVisible))
            .OrderBy(item => item.Bounds.Y)
            .ToArray();
        Button? remove = repeatedColourItems.ElementAtOrDefault(1)?.GetVisualDescendants().OfType<Button>().SingleOrDefault();
        if (remove is not null) host.Click(remove);

        // Assert
        repeatedColourItems.Should().HaveCount(2);
        remove.Should().NotBeNull();
        point.ColourSequence.Should().Equal(first, second);
    }

    [TestMethod]
    public void AddSequenceColourMenu_WhenPaletteHasMultipleColours_AddsClickedPaletteReference()
    {
        // Arrange
        ComboColourStudioViewModel viewModel = CreateViewModel(
            new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null));
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddColourPointCommand.Execute(null);
        var firstPoint = viewModel.SelectedColourPoint!;
        viewModel.AddColourPointCommand.Execute(null);
        var point = viewModel.SelectedColourPoint!;
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        var grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        var sequenceCell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(candidate => ReferenceEquals(candidate.DataContext, point)
                                && candidate.IsVisible
                                && candidate.Bounds.Width > 0
                                && candidate.Bounds.Height > 0)
            .OrderBy(candidate => candidate.Bounds.X)
            .ElementAt(2);

        // Act
        host.DoubleClick(sequenceCell);
        Button add = sequenceCell.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Command is null && button.IsVisible);
        host.Click(add);
        ContextMenu menu = add.ContextMenu!;
        MenuItem item = menu.GetVisualDescendants().OfType<MenuItem>()
            .Single(menuItem => menuItem.Header?.ToString() == "Combo2");
        string?[] menuHeaders = menu.GetVisualDescendants().OfType<MenuItem>()
            .Select(menuItem => menuItem.Header?.ToString())
            .ToArray();
        bool menuOmitsNotFoundPlaceholder = !menu.GetVisualDescendants().OfType<TextBlock>()
            .Any(textBlock => textBlock.Text == "NotFound");
        bool itemRendersColourBorder = item.GetVisualDescendants().OfType<Border>().Any();
        TopLevel popup = TopLevel.GetTopLevel(menu)
                         ?? throw new InvalidOperationException("The colour menu has no input root.");
        Avalonia.Point pointInPopup = Avalonia.VisualExtensions.TranslatePoint(
                                          item,
                                          new Avalonia.Point(item.Bounds.Width / 2, item.Bounds.Height / 2),
                                          popup)
                                      ?? throw new InvalidOperationException("Could not locate the menu item.");
        popup.MouseMove(pointInPopup);
        popup.MouseDown(pointInPopup, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        popup.MouseUp(pointInPopup, MouseButton.Left);
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        menuHeaders.Should().Contain("Combo1", "the palette menu should render its first colour");
        menuHeaders.Should().Contain("Combo2", "the palette menu should render its second colour");
        menuOmitsNotFoundPlaceholder.Should().BeTrue();
        itemRendersColourBorder.Should().BeTrue();
        point.ColourSequence.Should().ContainSingle().Which.Should().BeSameAs(viewModel.ComboColours[1]);
        firstPoint.ColourSequence.Should().BeEmpty();
    }

    [TestMethod]
    public void PaletteColorPickerChange_UpdatesSequenceAndUndoRestoresAllReferences()
    {
        // Arrange
        ComboColourStudioViewModel viewModel = CreateViewModel(new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null));
        viewModel.AddComboColourCommand.Execute(null);
        viewModel.AddColourPointCommand.Execute(null);
        viewModel.AddSequenceColour(viewModel.SelectedColourPoint!, viewModel.ComboColours[0]);
        viewModel.AddSequenceColour(viewModel.SelectedColourPoint!, viewModel.ComboColours[0]);
        var original = viewModel.ComboColours[0].Color;
        ProjectUndoHistory<Mapping_Tools.Desktop.Tools.ComboColourStudio.Models.ComboColourProject> history =
            new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        Window window = host.Window;
        ProjectUndoWindowInput.Attach(window, () => history);
        ColorPicker picker = view.GetVisualDescendants().OfType<ColorPicker>().First();
        var selectedColourPoint = viewModel.SelectedColourPoint!;
        Border[] sequenceSwatches = FindSequenceSwatches(view, selectedColourPoint, viewModel.ComboColours[0]);
        sequenceSwatches.Should().HaveCount(2);

        // Act
        ColorPickerTestDriver.SetHexColor(host, picker, "#FF00FF");
        Color edited = picker.Color;
        Color[] editedSwatches = FindSequenceSwatches(view, selectedColourPoint, viewModel.ComboColours[0])
            .Select(ReadSwatchColor).ToArray();
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        Core.BeatmapHelper.RgbaColour restored = viewModel.ComboColours[0].Color;
        Color[] restoredSwatches = FindSequenceSwatches(view, viewModel.SelectedColourPoint!, viewModel.ComboColours[0])
            .Select(ReadSwatchColor).ToArray();
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        Core.BeatmapHelper.RgbaColour redone = viewModel.ComboColours[0].Color;
        Color[] redoneSwatches = FindSequenceSwatches(view, viewModel.SelectedColourPoint!, viewModel.ComboColours[0])
            .Select(ReadSwatchColor).ToArray();

        // Assert
        edited.Should().NotBe(Color.FromRgb(original.R, original.G, original.B));
        editedSwatches.Should().HaveCount(2);
        editedSwatches.Should().OnlyContain(color => color == edited);
        restored.Should().Be(original);
        restoredSwatches.Should().HaveCount(2);
        restoredSwatches.Should().OnlyContain(color => color == Color.FromRgb(original.R, original.G, original.B));
        redone.Should().Be(Core.BeatmapHelper.RgbaColour.FromRgb(edited.R, edited.G, edited.B));
        redoneSwatches.Should().HaveCount(2);
        redoneSwatches.Should().OnlyContain(color => color == edited);
    }

    private static Border[] FindSequenceSwatches(ComboColourStudioView view, object point, object colour)
    {
        return view.GetVisualDescendants().OfType<Border>()
            .Where(border => ReferenceEquals(border.DataContext, colour)
                             && border.GetVisualAncestors().OfType<ItemsControl>()
                                 .Any(itemsControl => ReferenceEquals(itemsControl.DataContext, point))
                             && border.IsVisible
                             && border.Bounds.Width > 0
                             && border.Bounds.Height > 0)
            .ToArray();
    }

    private static Color ReadSwatchColor(Border swatch)
    {
        return ((SolidColorBrush)swatch.Background!).Color;
    }

    [TestMethod]
    public async Task AddColourPointButton_ShiftClick_UsesCurrentEditorTimestamp()
    {
        // Arrange
        const double editor_time = 1234;
        RecordingLiveBeatmapReader reader = new(new LiveBeatmapSnapshot(
            "C:/Songs/map/map.osu", [], [], [], 0, 1.4, 1, 5, 4, editor_time));
        ComboColourStudioViewModel viewModel = CreateViewModel(reader);
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("AddColourPointButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => viewModel.Project.ColourPoints.Count > 0);

        // Assert
        viewModel.Project.ColourPoints.Should().ContainSingle().Which.Time.Should().Be(editor_time);
        reader.ReadCount.Should().Be(1);
    }

    [TestMethod]
    public async Task AddColourPointButton_NormalClick_AddsOrdinaryPointWithoutReadingEditor()
    {
        // Arrange
        RecordingLiveBeatmapReader reader = new((LiveBeatmapSnapshot?)null);
        ComboColourStudioViewModel viewModel = CreateViewModel(reader);
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("AddColourPointButton"));
        await HeadlessViewHost.DrainAsync(() => viewModel.Project.ColourPoints.Count > 0);

        // Assert
        viewModel.Project.ColourPoints.Should().ContainSingle().Which.Time.Should().Be(0);
        reader.ReadCount.Should().Be(0);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AddColourPointButton_ShiftClick_WhenEditorIsUnavailableOrReaderFails_ReportsErrorWithoutAddingPoint(
        bool readerFails)
    {
        // Arrange
        UserNotificationService notifications = new();
        List<UserNotification> published = [];
        notifications.Published += (_, args) => published.Add(args.Notification);
        RecordingLiveBeatmapReader reader = readerFails
            ? new RecordingLiveBeatmapReader(new InvalidOperationException("reader failed"))
            : new RecordingLiveBeatmapReader((LiveBeatmapSnapshot?)null);
        ComboColourStudioViewModel viewModel = CreateViewModel(reader, notifications);
        ComboColourStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("AddColourPointButton"), KeyModifiers.Shift);
        await HeadlessViewHost.DrainAsync(() => published.Count > 0);

        // Assert
        published.Should().ContainSingle().Which.Severity.Should().Be(UserNotificationSeverity.Error);
        viewModel.Project.ColourPoints.Should().BeEmpty();
    }

    private static ComboColourStudioViewModel CreateViewModel(
        RecordingLiveBeatmapReader reader,
        IUserNotificationService? notifications = null)
    {
        IUserNotificationService effectiveNotifications = notifications ?? new UserNotificationService();
        return new ComboColourStudioViewModel(
            new StubComboColourStudioService(),
            new ToolExecutionService(effectiveNotifications, TimeProvider.System),
            effectiveNotifications,
            new TestBeatmapWorkspace(),
            new TestCurrentBeatmapDialogService(),
            reader,
            new TestFilePicker());
    }

    private sealed class StubComboColourStudioService : IComboColourStudioService
    {
        public int ImportCount { get; private set; }

        public int ApplyCount { get; private set; }

        public Task<Mapping_Tools.Core.Tools.ComboColourStudio.Models.ComboColourEngineOptions> ImportComboColoursAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            ImportCount++;
            return Task.FromResult(new Mapping_Tools.Core.Tools.ComboColourStudio.Models.ComboColourEngineOptions());
        }

        public Task<Mapping_Tools.Core.Tools.ComboColourStudio.Models.ComboColourEngineOptions> ImportColourHaxAsync(
            string path,
            int maxBurstLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Mapping_Tools.Core.Tools.ComboColourStudio.Models.ComboColourEngineOptions());
        }

        public Task<ComboColourStudioRunResult> ApplyAsync(
            IReadOnlyList<string> paths,
            ComboColourServiceOptions project,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            return Task.FromResult(new ComboColourStudioRunResult(paths.Count));
        }
    }
}
