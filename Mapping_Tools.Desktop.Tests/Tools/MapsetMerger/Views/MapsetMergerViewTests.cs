using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.MapsetMerger.Contracts;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;
using Mapping_Tools.Desktop.Tools.MapsetMerger.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.MapsetMerger.Views;

[TestClass]
public sealed class MapsetMergerViewTests
{
    [TestMethod]
    public void PathCell_SinglePointerClick_FocusesEditorAndAcceptsText()
    {
        // Arrange
        MapsetMergerViewModel viewModel = CreateViewModel();
        MapsetMergerItemViewModel mapset = new(new TestFilePicker(), "set", @"C:\Maps\set");
        viewModel.Mapsets.Add(mapset);
        MapsetMergerView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        DataGrid grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell pathCell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, mapset) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .ElementAt(1);
        TextBlock pathDisplay = pathCell.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == mapset.Path);

        // Act
        host.Click(pathDisplay);
        TextBox editor = pathCell.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.IsVisible && textBox.Bounds.Width > 0);
        bool editorFocused = editor.IsFocused;
        host.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText(@"D:\Edited\set");

        // Assert
        editorFocused.Should().BeTrue();
        editor.Text.Should().Be(@"D:\Edited\set");
    }

    [TestMethod]
    public void NameCell_TabMovesToPathCell_AndTextInputBeginsEditing()
    {
        // Arrange
        MapsetMergerViewModel viewModel = CreateViewModel();
        MapsetMergerItemViewModel mapset = new(new TestFilePicker(), "set", @"C:\Maps\set");
        viewModel.Mapsets.Add(mapset);
        MapsetMergerView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        DataGrid grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell[] cells = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, mapset) && cell.IsVisible && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .ToArray();
        TextBlock nameDisplay = cells[0].GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == mapset.Name);

        // Act
        host.Click(nameDisplay);
        host.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("name edited from the keyboard");
        host.Window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        host.Window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        HeadlessViewHost.RunDispatcherJobs();
        bool gridRetainedFocus = grid.IsKeyboardFocusWithin;
        DataGridColumn? currentColumnAfterTab = grid.CurrentColumn;
        object? focusedElement = TopLevel.GetTopLevel(grid)?.FocusManager.GetFocusedElement();
        host.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("path edited from the keyboard");
        TextBox? pathEditor = cells[1].GetVisualDescendants().OfType<TextBox>()
            .FirstOrDefault(textBox => textBox.IsVisible && textBox.Bounds.Width > 0);

        // Assert
        mapset.Name.Should().Be("name edited from the keyboard");
        gridRetainedFocus.Should().BeTrue($"focused element was {focusedElement?.GetType().FullName}");
        currentColumnAfterTab.Should().Be(grid.Columns[1]);
        pathEditor.Should().NotBeNull();
        pathEditor.IsFocused.Should().BeTrue();
        pathEditor.Text.Should().Be("path edited from the keyboard");
    }

    [TestMethod]
    public void TabAndShiftTab_WrapAcrossRows_StayInsideGrid()
    {
        // Arrange
        MapsetMergerViewModel viewModel = CreateViewModel();
        MapsetMergerItemViewModel firstMapset = new(new TestFilePicker(), "first", @"C:\Maps\first");
        MapsetMergerItemViewModel secondMapset = new(new TestFilePicker(), "second", @"C:\Maps\second");
        viewModel.Mapsets.Add(firstMapset);
        viewModel.Mapsets.Add(secondMapset);
        MapsetMergerView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        DataGrid grid = view.GetVisualDescendants().OfType<DataGrid>().Single();
        DataGridCell firstNameCell = grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(cell => ReferenceEquals(cell.DataContext, firstMapset)
                           && cell.IsVisible
                           && cell.Bounds.Width > 0)
            .OrderBy(cell => cell.Bounds.X)
            .First();
        TextBlock firstNameDisplay = firstNameCell.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == firstMapset.Name);

        void pressTab(RawInputModifiers modifiers)
        {
            host.Window.KeyPress(Key.Tab, modifiers, PhysicalKey.Tab, "\t");
            host.Window.KeyRelease(Key.Tab, modifiers, PhysicalKey.Tab, "\t");
            HeadlessViewHost.RunDispatcherJobs();
        }

        // Act
        host.Click(firstNameDisplay);
        host.Window.KeyPress(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.Window.KeyRelease(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("first row edited");
        pressTab(RawInputModifiers.None);
        pressTab(RawInputModifiers.None);
        bool forwardTabSelectedNextRow = ReferenceEquals(grid.SelectedItem, secondMapset)
                                        && grid.CurrentColumn == grid.Columns[0];
        pressTab(RawInputModifiers.Shift);

        // Assert
        grid.IsKeyboardFocusWithin.Should().BeTrue();
        firstMapset.Name.Should().Be("first row edited");
        forwardTabSelectedNextRow.Should().BeTrue();
        grid.SelectedItem.Should().Be(firstMapset);
        grid.CurrentColumn.Should().Be(grid.Columns[1]);
    }

    private static MapsetMergerViewModel CreateViewModel()
    {
        UserNotificationService notifications = new();
        return new MapsetMergerViewModel(
            new RecordingMapsetMergerService(),
            new ToolExecutionService(notifications, TimeProvider.System),
            new TestFilePicker(),
            new TestBeatmapWorkspace(),
            new TestCurrentBeatmapDialogService(),
            new TestApplicationDirectories(),
            notifications);
    }

    private sealed class RecordingMapsetMergerService : IMapsetMergerService
    {
        public Task<MapsetMergerResult> MergeAsync(
            MapsetMergerServiceOptions project,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MapsetMergerResult(0, 0, 0, 0));
        }
    }
}
