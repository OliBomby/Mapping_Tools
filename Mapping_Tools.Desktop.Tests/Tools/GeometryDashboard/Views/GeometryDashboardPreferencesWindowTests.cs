using Avalonia.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.Views;

[TestClass]
[DoNotParallelize]
public sealed class GeometryDashboardPreferencesWindowTests
{
    [TestMethod]
    public void SnapHotkey_EditorLosesFocus_UpdatesSettings()
    {
        // Arrange
        GeometryDashboardPreferencesDialogViewModel viewModel = new(new GeometryDashboardPreferences(), false);
        GeometryDashboardPreferencesWindow window = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        HotkeyEditor editor = window.GetVisualDescendants().OfType<HotkeyEditor>().First();

        // Act
        editor.ApplyKey(Key.B, KeyModifiers.Control);
        editor.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent) { Source = editor });

        // Assert
        viewModel.Preferences.SnapHotkey.Should().Be(new HotkeySettings(45, 2));
        editor.Hotkey.Should().Be(viewModel.Preferences.SnapHotkey);
        editor.Text.Should().Be("Ctrl + B");
    }

    [TestMethod]
    public void Show_DutchPreferenceLabel_DisplaysTextWithoutClipping()
    {
        // Arrange
        string? previousLanguage = TranslationManager.Language;
        TranslationManager.SetLanguage("nl");
        GeometryDashboardPreferencesDialogViewModel viewModel = new(new GeometryDashboardPreferences(), false);
        GeometryDashboardPreferencesWindow window = new() { DataContext = viewModel };

        try
        {
            using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
            TextBlock label = window.GetVisualDescendants().OfType<TextBlock>()
                .Single(block => block.Text == "Bruikbaarheidstoets");
            // Act
            double textWidth = label.TextLayout.TextLines.Max(line => line.WidthIncludingTrailingWhitespace);

            // Assert
            label.Bounds.Width.Should().BeGreaterThanOrEqualTo(textWidth);
        }
        finally
        {
            TranslationManager.SetLanguage(previousLanguage);
        }
    }

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
