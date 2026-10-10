using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Views;

[TestClass]
public sealed class GeometryDashboardViewTests
{
    [TestMethod]
    public async Task StatusLabel_UnfocusedEditor_RendersRunningStatus()
    {
        // Arrange
        var editor = GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(
            DecodeHitObject("64,96,1000,1,0,0:0:0:0:"), 0, []).Editor;
        using GeometryDashboardViewModel viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(
            snapshots: new GeometryDashboardRuntimeSnapshot(editor, false));
        GeometryDashboardView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);

        // Act
        await viewModel.RefreshOnceAsync();
        await HeadlessViewHost.DrainAsync(() => view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Any(block => block.Text?.StartsWith("Unfocused:", StringComparison.Ordinal) == true));
        string visibleStatus = view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Single(block => block.Text?.StartsWith("Unfocused:", StringComparison.Ordinal) == true).Text!;
        bool statusIsVisible = view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Single(block => block.Text == visibleStatus).IsEffectivelyVisible;

        // Assert
        statusIsVisible.Should().BeTrue();
        visibleStatus.Should().Be(viewModel.Status);
        visibleStatus.Should().StartWith("Unfocused:");
    }

    [TestMethod]
    public async Task StatusLabel_UnavailableInput_RendersErrorStatus()
    {
        // Arrange
        using GeometryDashboardViewModel viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(false);
        GeometryDashboardView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);

        // Act
        await viewModel.RefreshOnceAsync();
        await HeadlessViewHost.DrainAsync(() => view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Any(block => block.Text?.StartsWith("Unable to run:", StringComparison.Ordinal) == true));
        string visibleStatus = view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Single(block => block.Text?.StartsWith("Unable to run:", StringComparison.Ordinal) == true).Text!;
        bool statusIsVisible = view.GetVisualDescendants().OfType<SelectableTextBlock>()
            .Single(block => block.Text == visibleStatus).IsEffectivelyVisible;

        // Assert
        statusIsVisible.Should().BeTrue();
        visibleStatus.Should().Be(viewModel.Status);
        visibleStatus.Should().Be("Unable to run: Geometry Dashboard requires Windows.");
    }

    [TestMethod]
    public async Task GeneratorSettingsDialog_ApplyAndUndo_RefreshesVisibleTableCells()
    {
        // Arrange
        WindowOwner ownerWindow = new();
        GeometryDashboardViewModel viewModel = GeometryDashboardViewModelTestFactory
            .CreateViewModelSession(owner: () => ownerWindow.Window!).ViewModel;
        using (viewModel)
        {
            var originalRow = viewModel.Generators[0];
            string generatorName = originalRow.Name;
            bool originalActive = originalRow.IsActive;
            bool originalSequential = originalRow.IsSequential;
            double originalRelevancy = originalRow.RelevancyRatio;
            ProjectUndoHistory<GeometryDashboardProject> history = new(viewModel, new VersionedProjectJsonSerializer());
            viewModel.UndoHistory = history;
            GeometryDashboardView dashboard = new() { DataContext = viewModel };
            using HeadlessViewHost dashboardHost = HeadlessViewHost.Show(dashboard, height: 1200);
            Window owner = dashboardHost.Window;
            ownerWindow.Window = owner;
            ProjectUndoWindowInput.Attach(owner, () => history);
            Button configure = dashboard.GetVisualDescendants().OfType<Button>()
                .Single(button => Equals(button.Content, "Configure") && ReferenceEquals(button.DataContext, originalRow));

            // Act
            dashboardHost.Click(configure);
            await HeadlessViewHost.DrainAsync(() => owner.OwnedWindows.Count > 0);
            GeometryDashboardGeneratorSettingsWindow dialog = owner.OwnedWindows
                .OfType<GeometryDashboardGeneratorSettingsWindow>().Single();
            using HeadlessViewHost dialogHost = HeadlessViewHost.Attach(dialog);
            var dialogViewModel = (GeometryDashboardGeneratorSettingsDialogViewModel)dialog.DataContext!;
            var activeSetting = dialogViewModel.SharedRows.Single(row => row.Name == "Active");
            var sequentialSetting = dialogViewModel.SharedRows.Single(row => row.Name == "Sequential");
            var relevancySetting = dialogViewModel.SharedRows.Single(row => row.Name == "Relevancy Ratio");
            ToggleSwitch activeToggle = dialog.GetVisualDescendants().OfType<ToggleSwitch>()
                .Single(control => ReferenceEquals(control.DataContext, activeSetting));
            ToggleSwitch sequentialToggle = dialog.GetVisualDescendants().OfType<ToggleSwitch>()
                .Single(control => ReferenceEquals(control.DataContext, sequentialSetting));
            TextBox relevancyInput = dialog.GetVisualDescendants().OfType<TextBox>()
                .Single(control => ReferenceEquals(control.DataContext, relevancySetting));
            Button apply = dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => Equals(button.Content, "Apply"));

            dialogHost.Click(activeToggle, new Avalonia.Point(8, activeToggle.Bounds.Height / 2));
            dialogHost.Click(sequentialToggle, new Avalonia.Point(8, sequentialToggle.Bounds.Height / 2));
            dialogHost.Click(relevancyInput);
            dialogHost.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
            dialogHost.TypeText("0.75");
            dialogHost.Click(apply);
            await HeadlessViewHost.DrainAsync(() => history.CanUndo && owner.OwnedWindows.Count == 0);
            GeometryDashboardGeneratorViewModel editedRow = viewModel.Generators.Single(row => row.Name == generatorName);
            bool editedActive = editedRow.IsActive;
            bool editedSequential = editedRow.IsSequential;
            double editedRelevancy = editedRow.RelevancyRatio;
            history.Undo();
            GeometryDashboardGeneratorViewModel restoredRow = viewModel.Generators.Single(row => row.Name == generatorName);
            ToggleSwitch[] restoredSwitches = dashboard.GetVisualDescendants().OfType<ToggleSwitch>()
                .Where(control => ReferenceEquals(control.DataContext, restoredRow)).ToArray();
            TextBox restoredRelevancy = dashboard.GetVisualDescendants().OfType<TextBox>()
                .Single(control => ReferenceEquals(control.DataContext, restoredRow));

            // Assert
            editedActive.Should().Be(!originalActive);
            editedSequential.Should().Be(!originalSequential);
            editedRelevancy.Should().Be(0.75);
            restoredRow.IsActive.Should().Be(originalActive);
            restoredRow.IsSequential.Should().Be(originalSequential);
            restoredRow.RelevancyRatio.Should().Be(originalRelevancy);
            restoredSwitches.Should().HaveCount(2);
            restoredSwitches[0].IsChecked.Should().Be(originalActive);
            restoredSwitches[1].IsChecked.Should().Be(originalSequential);
            double.Parse(restoredRelevancy.Text!, System.Globalization.CultureInfo.InvariantCulture).Should().Be(originalRelevancy);
        }
    }

    [TestMethod]
    public void GeneratorRowToggle_Click_ChangesOnlyItsBoundGenerator()
    {
        // Arrange
        GeometryDashboardViewModel viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();
        using (viewModel)
        {
            var target = viewModel.Generators[0];
            var other = viewModel.Generators[1];
            bool targetWasActive = target.IsActive;
            bool otherWasActive = other.IsActive;
            GeometryDashboardView view = new() { DataContext = viewModel };
            using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
            ToggleSwitch toggle = view.GetVisualDescendants().OfType<ToggleSwitch>()
                .First(control => ReferenceEquals(control.DataContext, target));
            toggle.IsVisible.Should().BeTrue();
            toggle.Bounds.Width.Should().BeGreaterThan(0);

            // Act
            host.Click(toggle, new Avalonia.Point(8, toggle.Bounds.Height / 2));

            // Assert
            target.IsActive.Should().Be(!targetWasActive);
            other.IsActive.Should().Be(otherWasActive);
        }
    }

    [TestMethod]
    public async Task ToggleSelected_ShiftAndControlClicks_ForceEnableThenDisable()
    {
        // Arrange
        var session = GeometryDashboardViewModelTestFactory.CreateViewModelSession(
            snapshots:
            [
                GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(
                    DecodeHitObject("64,96,1000,1,0,0:0:0:0:"),
                    0,
                    []),
            ]);
        using GeometryDashboardViewModel viewModel = session.ViewModel;
        await viewModel.RefreshOnceAsync();
        int drawableCount = viewModel.DrawableCount;
        drawableCount.Should().BeGreaterThan(0);
        GeometryDashboardView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("ToggleSelectedButton"), KeyModifiers.Shift);
        int selectedAfterShift = viewModel.SelectedCount;
        host.Click(host.Find<Button>("ToggleSelectedButton"), KeyModifiers.Control);

        // Assert
        selectedAfterShift.Should().Be(drawableCount);
        viewModel.SelectedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task ToggleLocked_ShiftClick_PreservesObjectsAcrossRefreshThenControlUnlocksThem()
    {
        // Arrange
        HitObject source = DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        var session = GeometryDashboardViewModelTestFactory.CreateViewModelSession(
            snapshots:
            [
                GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(source, 0, []),
                CreateEmptyRuntimeSnapshot(),
                CreateEmptyRuntimeSnapshot(),
            ]);
        using GeometryDashboardViewModel viewModel = session.ViewModel;
        await viewModel.RefreshOnceAsync();
        int initialDrawableCount = viewModel.DrawableCount;
        initialDrawableCount.Should().BeGreaterThan(0);
        GeometryDashboardView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("ToggleLockedButton"), KeyModifiers.Shift);
        await viewModel.RefreshOnceAsync();
        int lockedCountAfterRefresh = session.Service.GetLockedObjects().Values.Sum(objects => objects.Count);
        host.Click(host.Find<Button>("ToggleLockedButton"), KeyModifiers.Control);
        await viewModel.RefreshOnceAsync();
        int lockedCountAfterUnlockRefresh = session.Service.GetLockedObjects().Values.Sum(objects => objects.Count);

        // Assert
        lockedCountAfterRefresh.Should().Be(initialDrawableCount);
        lockedCountAfterUnlockRefresh.Should().Be(0);
    }

    [TestMethod]
    public async Task ToggleInheritable_ShiftAndControlClicks_ForceDrawableFlags()
    {
        // Arrange
        RelevantPoint[] points = [new(new Vector2(64, 64)), new(new Vector2(128, 128)), new(new Vector2(192, 192))];
        foreach (RelevantPoint point in points)
        {
            point.IsLocked = true;
            point.IsInheritable = false;
        }
        var session = GeometryDashboardViewModelTestFactory.CreateViewModelSession(
            snapshots: [CreateEmptyRuntimeSnapshot()]);
        session.Service.SetLockedObjects(new RelevantObjectCollection { [typeof(RelevantPoint)] = [.. points] });
        using GeometryDashboardViewModel viewModel = session.ViewModel;
        await viewModel.RefreshOnceAsync();
        GeometryDashboardView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(host.Find<Button>("ToggleInheritableButton"), KeyModifiers.Shift);
        RelevantPoint[] enabled = session.Service.GetLockedObjects().Values.SelectMany(objects => objects).OfType<RelevantPoint>().ToArray();
        host.Click(host.Find<Button>("ToggleInheritableButton"), KeyModifiers.Control);
        RelevantPoint[] disabled = session.Service.GetLockedObjects().Values.SelectMany(objects => objects).OfType<RelevantPoint>().ToArray();

        // Assert
        enabled.Should().HaveCount(3);
        enabled.Should().OnlyContain(point => point.IsInheritable);
        disabled.Should().HaveCount(3);
        disabled.Should().OnlyContain(point => !point.IsInheritable);
    }

    private static GeometryDashboardRuntimeSnapshot CreateEmptyRuntimeSnapshot()
    {
        return new GeometryDashboardRuntimeSnapshot(
            new LiveBeatmapSnapshot("C:/Songs/map/map.osu", [], [], [], 0, 1.4, 1, 5, 4, 0),
            true);
    }

    private sealed class WindowOwner
    {
        public Window? Window { get; set; }
    }
}
