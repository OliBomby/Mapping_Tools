using Avalonia.Controls;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Desktop.Services.Notifications;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views;

[TestClass]
public sealed class MainWindowNotificationSurfaceTests
{
    [TestMethod]
    public async Task ShowSnackbar_InformationNotification_RendersMessageInMainWindow()
    {
        // Arrange
        MainWindow window = new();
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        INotificationSurface surface = window;
        UserNotification notification = new(
            UserNotificationSeverity.Success,
            "Hitsound Studio",
            "Export complete.");

        // Act
        surface.ShowSnackbar(notification);
        await HeadlessViewHost.DrainAsync(() => window.GetVisualDescendants().OfType<TextBlock>()
            .Any(textBlock => textBlock.IsEffectivelyVisible
                              && textBlock.Text == "Hitsound Studio: Export complete."));

        // Assert
        window.GetVisualDescendants().OfType<TextBlock>()
            .Should().Contain(textBlock => textBlock.IsEffectivelyVisible
                                           && textBlock.Text == "Hitsound Studio: Export complete.");
    }
}
