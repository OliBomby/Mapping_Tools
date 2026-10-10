using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Views;

[TestClass]
public sealed class GeometryDashboardSavestatesWindowTests
{
    [TestMethod]
    public async Task AddRenameLoadAndRemove_ClickedControlsUpdateVisibleSaveRows()
    {
        // Arrange
        using GeometryDashboardViewModel viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();
        ProjectUndoHistory<GeometryDashboardProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        GeometryDashboardSaveSlot? loadedSlot = null;
        GeometryDashboardSavestatesViewModel saveViewModel = new(
            viewModel.Project,
            slot => loadedSlot = slot,
            () => { },
            history);
        GeometryDashboardSavestatesWindow window = new()
        {
            DataContext = saveViewModel,
        };
        window.AttachUndoHistory(history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        Button add = window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Add a new save slot."));

        // Act
        host.Click(add);
        int slotsAfterAdd = saveViewModel.SaveSlots.Count;
        string addedSlotName = saveViewModel.SaveSlots.Single().Name;
        TextBox name = window.GetVisualDescendants().OfType<TextBox>().Single(textBox => textBox.Text == "Save 1");
        bool nameVisibleAfterAdd = name.IsVisible;
        double nameWidthAfterAdd = name.Bounds.Width;
        host.Click(name);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("Renamed");
        Button load = window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(button.Content, "Load"));
        host.Click(load);
        Button remove = window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Remove all selected save slots or the last save if nothing is selected."));
        host.Click(remove);
        int slotsAfterRemove = saveViewModel.SaveSlots.Count;
        history.Undo();
        await HeadlessViewHost.DrainAsync(() =>
            window.GetVisualDescendants().OfType<TextBox>().Any(textBox => textBox.Text == "Renamed"));
        TextBox[] renamedTextBoxes = window.GetVisualDescendants().OfType<TextBox>()
            .Where(textBox => textBox.Text == "Renamed")
            .ToArray();

        // Assert
        slotsAfterAdd.Should().Be(1);
        addedSlotName.Should().Be("Save 1");
        nameVisibleAfterAdd.Should().BeTrue();
        nameWidthAfterAdd.Should().BeGreaterThan(0);
        loadedSlot.Should().NotBeNull();
        loadedSlot!.Name.Should().Be("Renamed");
        slotsAfterRemove.Should().Be(0);
        saveViewModel.SaveSlots.Should().ContainSingle().Which.Name.Should().Be("Renamed");
        viewModel.Project.SaveSlots.Should().ContainSingle();
        renamedTextBoxes.Should().ContainSingle();
    }
}
