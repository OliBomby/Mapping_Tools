using Avalonia.Controls;
using Avalonia;
using Avalonia.VisualTree;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Views;

[TestClass]
public sealed class GeometryDashboardPreferencesWindowTests
{
    [TestMethod]
    public void UpdatingModeRadioButton_Click_SetsHotkeyDownPreference()
    {
        // Arrange
        GeometryDashboardPreferencesDialogViewModel viewModel = new(new GeometryDashboardPreferences(), false);
        GeometryDashboardPreferencesWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        RadioButton hotkeyDown = window.GetVisualDescendants().OfType<RadioButton>()
            .Single(button => Equals(button.Content, "Hotkey down"));
        hotkeyDown.BringIntoView();
        HeadlessViewHost.RunDispatcherJobs();

        // Act
        host.Click(hotkeyDown, new Point(8, hotkeyDown.Bounds.Height / 2));

        // Assert
        viewModel.UpdatingHotkeyDown.Should().BeTrue();
        viewModel.Preferences.UpdateMode.Should().Be(UpdateMode.HotkeyDown);
        window.GetVisualDescendants().OfType<RadioButton>()
            .Single(button => Equals(button.Content, "Hotkey down")).IsChecked.Should().BeTrue();
    }
}
