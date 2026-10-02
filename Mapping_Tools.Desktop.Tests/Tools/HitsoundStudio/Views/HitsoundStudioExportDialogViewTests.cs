using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.Views;

[TestClass]
public sealed class HitsoundStudioExportDialogViewTests
{
    [TestMethod]
    public void ShowResultsCheckBox_RealAccept_PersistsDetailedResultsPreference()
    {
        // Arrange
        HitsoundStudioProject project = new()
        {
            ExportFolder = @"C:\Export",
            HitsoundDiffName = "Hitsounds",
        };
        HitsoundStudioExportDialogViewModel viewModel = new(project, new TestFilePicker());
        HitsoundStudioProject? acceptedProject = null;
        viewModel.Close = value => acceptedProject = (HitsoundStudioProject?)value;
        HitsoundStudioExportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(dialog, height: 1200);
        CheckBox showResults = dialog.GetVisualDescendants().OfType<CheckBox>()
            .Single(checkBox => checkBox.Content?.ToString() == "Show results");

        // Act
        host.Click(showResults, new Point(8, showResults.Bounds.Height / 2));
        Button accept = dialog.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "ACCEPT");
        accept.Focus();
        host.PressKey(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");

        // Assert
        viewModel.ShowResults.Should().BeTrue();
        acceptedProject.Should().NotBeNull();
        acceptedProject!.ShowResults.Should().BeTrue();
    }
}
