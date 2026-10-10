using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Core.Tools.SliderCompletionator.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.SliderCompletionator.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.ViewModels;
using Mapping_Tools.Desktop.Tools.SliderCompletionator.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.SliderCompletionator.Views;

[TestClass]
public sealed class SliderCompletionatorViewTests
{
    [DataTestMethod]
    [DataRow(1, SliderCompletionatorFreeVariable.Length)]
    [DataRow(2, SliderCompletionatorFreeVariable.Duration)]
    public async Task RunButton_KeyboardSelectedFreeVariable_PassesSelectionToService(
        int downKeyPresses,
        SliderCompletionatorFreeVariable expectedFreeVariable)
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        SliderCompletionatorViewModel viewModel =
            SliderCompletionatorViewModelTestFactory.Create(service, workspace);
        SliderCompletionatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        CheckBox currentEditorTime = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => ToolTip.GetTip(checkBox)?.ToString()?.StartsWith("Snap the slider ends", StringComparison.Ordinal) == true);
        ComboBox freeVariable = view.GetVisualDescendants().OfType<ComboBox>()
            .Single(comboBox => ToolTip.GetTip(comboBox)?.ToString()?.StartsWith("Choose which variable", StringComparison.Ordinal) == true);
        Button run = view.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Run this tool."));

        // Act
        host.Click(currentEditorTime, new Avalonia.Point(8, currentEditorTime.Bounds.Height / 2));
        bool freeVariableFocused = freeVariable.Focus();
        for (int press = 0; press < downKeyPresses; press++)
            host.PressKey(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "ArrowDown");
        host.Click(run);
        await HeadlessViewHost.DrainAsync(() => service.Options is not null && !viewModel.IsRunning);

        // Assert
        freeVariableFocused.Should().BeTrue();
        freeVariable.SelectedItem.Should().Be(expectedFreeVariable);
        service.Options.Should().NotBeNull();
        service.Options!.FreeVariableSetting.Should().Be(expectedFreeVariable);
        service.Paths.Should().Equal("selected.osu");
    }

    [TestMethod]
    public async Task RunButton_EndTimeControlsAndTypedInputs_PassesVisibleOptionsToService()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        SliderCompletionatorViewModel viewModel =
            SliderCompletionatorViewModelTestFactory.Create(service, workspace);
        SliderCompletionatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        CheckBox useEndTime = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => ToolTip.GetTip(checkBox)?.ToString()?.StartsWith("Lets you input", StringComparison.Ordinal) == true);
        CheckBox useCurrentEditorTime = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => ToolTip.GetTip(checkBox)?.ToString()?.StartsWith("Snap the slider ends", StringComparison.Ordinal) == true);
        ComboBox freeVariable = view.GetVisualDescendants().OfType<ComboBox>()
            .Single(comboBox => ToolTip.GetTip(comboBox)?.ToString()?.StartsWith("Choose which variable", StringComparison.Ordinal) == true);
        TextBox endTime = view.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => ToolTip.GetTip(textBox)?.ToString()?.StartsWith("Wanted slider end time", StringComparison.Ordinal) == true);
        Button run = view.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Run this tool."));

        // Act
        host.Click(useEndTime, new Avalonia.Point(8, useEndTime.Bounds.Height / 2));
        host.Click(endTime);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("00:12:345");
        host.Click(useCurrentEditorTime, new Avalonia.Point(8, useCurrentEditorTime.Bounds.Height / 2));
        bool freeVariableFocused = freeVariable.Focus();
        host.PressKey(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, "ArrowDown");
        host.Click(run);
        await HeadlessViewHost.DrainAsync(() => service.Options is not null && !viewModel.IsRunning);

        // Assert
        freeVariableFocused.Should().BeTrue();
        viewModel.UseEndTime.Should().BeTrue();
        viewModel.UseCurrentEditorTime.Should().BeTrue();
        freeVariable.SelectedItem.Should().Be(SliderCompletionatorFreeVariable.Length);
        service.Paths.Should().Equal("selected.osu");
        service.Options.Should().NotBeNull();
        service.Options!.UseEndTime.Should().BeTrue();
        service.Options.UseCurrentEditorTime.Should().BeTrue();
        service.Options.FreeVariableSetting.Should().Be(SliderCompletionatorFreeVariable.Length);
        service.Options.EndTime.Should().Be(12_345);
    }

    [TestMethod]
    public async Task RunButton_WhenEndTimeIsTurnedOff_DoesNotUseConfiguredEndTime()
    {
        // Arrange
        RecordingCompletionator service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        SliderCompletionatorViewModel viewModel =
            SliderCompletionatorViewModelTestFactory.Create(service, workspace);
        viewModel.UseEndTime = true;
        viewModel.EndTime = 12_345;
        SliderCompletionatorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view, height: 1200);
        CheckBox useEndTime = view.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => ToolTip.GetTip(checkBox)?.ToString()?.StartsWith("Lets you input", StringComparison.Ordinal) == true);
        Button run = view.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(ToolTip.GetTip(button), "Run this tool."));

        // Act
        host.Click(useEndTime, new Avalonia.Point(8, useEndTime.Bounds.Height / 2));
        host.Click(run);
        await HeadlessViewHost.DrainAsync(() => !viewModel.IsRunning && service.Options?.UseEndTime == false);

        // Assert
        useEndTime.IsChecked.Should().BeFalse();
        viewModel.UseEndTime.Should().BeFalse();
        service.Options.Should().NotBeNull();
        service.Options!.UseEndTime.Should().BeFalse();
        service.Options.EndTime.Should().Be(12_345);
    }
}
