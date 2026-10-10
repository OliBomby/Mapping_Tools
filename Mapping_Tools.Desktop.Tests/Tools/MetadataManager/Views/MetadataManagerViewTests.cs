using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.MetadataManager.ViewModels;
using Mapping_Tools.Desktop.Tools.MetadataManager.Models;
using Mapping_Tools.Desktop.Tools.MetadataManager.Views;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.MetadataManager.Views;

[TestClass]
public sealed class MetadataManagerViewTests
{
    [TestMethod]
    public async Task ComboColorPickerChange_UndoRestoresColorAndRedoReappliesIt()
    {
        // Arrange
        var viewModel = MetadataManagerViewModelTestFactory.Create();
        viewModel.AddComboColourCommand.Execute(null);
        RgbaColour original = viewModel.ComboColours[0].Color;
        ProjectUndoHistory<MetadataManagerProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        MetadataManagerView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Window window = host.Window;
        ProjectUndoWindowInput.Attach(window, () => history);
        ColorPicker picker = view.GetVisualDescendants().OfType<ColorPicker>().First();

        // Act
        ColorPickerTestDriver.SetHexColor(host, picker, "#FF00FF");
        Color editedColor = picker.Color;
        RgbaColour editedModelColor = viewModel.ComboColours[0].Color;
        await HeadlessViewHost.DrainAsync(() => history.CanUndo);
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        RgbaColour undoneColor = viewModel.ComboColours[0].Color;
        Color visibleUndoneColor = view.GetVisualDescendants().OfType<ColorPicker>().First().Color;
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        RgbaColour redoneColor = viewModel.ComboColours[0].Color;
        Color visibleColor = view.GetVisualDescendants().OfType<ColorPicker>().First().Color;

        // Assert
        editedModelColor.Should().Be(RgbaColour.FromRgb(editedColor.R, editedColor.G, editedColor.B));
        undoneColor.Should().Be(original);
        visibleUndoneColor.Should().Be(Color.FromRgb(original.R, original.G, original.B));
        redoneColor.Should().Be(editedModelColor);
        visibleColor.Should().Be(editedColor);
    }
}
