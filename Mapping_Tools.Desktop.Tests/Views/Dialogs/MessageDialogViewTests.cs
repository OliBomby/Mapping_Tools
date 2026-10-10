using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views.Dialogs;

[TestClass]
public sealed class MessageDialogViewTests
{
    [TestMethod]
    public void MessageDialog_ChoicesAndErrorDetails_RendersSharedMessageSurface()
    {
        // Arrange
        int accepted = 0;
        MessageDialogViewModel viewModel = new(
            "Solution 1",
            "Auto-fail fix guide. Do you want to use this solution?",
            "First decoder failure detail",
            [new DialogChoiceViewModel("Yes", true, false, () => accepted++),
             new DialogChoiceViewModel("No", false, true, () => { })]);
        MessageDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button yes = FindButton(dialog, "Yes");
        Button no = FindButton(dialog, "No");
        TextBlock detailsHeader = dialog.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == "Show Error Details");
        TextBlock detailsText = dialog.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == "First decoder failure detail");

        // Act
        host.Click(detailsHeader);
        bool yesVisibleAfterExpandingDetails = yes.IsEffectivelyVisible;
        bool yesCommandAvailable = yes.Command is not null;
        bool yesCommandCanExecute = yes.Command?.CanExecute(null) == true;
        yes.Focus();
        host.PressKey(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");

        // Assert
        yesVisibleAfterExpandingDetails.Should().BeTrue();
        yesCommandAvailable.Should().BeTrue();
        yesCommandCanExecute.Should().BeTrue();
        dialog.Title.Should().Be("Solution 1");
        dialog.GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == "Auto-fail fix guide. Do you want to use this solution?")
            .IsEffectivelyVisible.Should().BeTrue();
        detailsText.IsEffectivelyVisible.Should().BeTrue();
        FindButton(dialog, "No").IsCancel.Should().BeTrue();
        no.Should().NotBeNull();
        accepted.Should().Be(1);
    }

    [TestMethod]
    public void MessageDialog_CancelChoice_EscapeInvokesRealCancelButton()
    {
        // Arrange
        int cancelled = 0;
        MessageDialogViewModel viewModel = new(
            "Solution 1",
            "Use this auto-fail fix?",
            null,
            [new DialogChoiceViewModel("Yes", true, false, () => { }),
             new DialogChoiceViewModel("No", false, true, () => cancelled++)]);
        MessageDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button cancel = FindButton(dialog, "No");

        // Act
        cancel.Focus();
        host.PressKey(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "\u001b");

        // Assert
        cancelled.Should().Be(1);
        cancel.IsCancel.Should().BeTrue();
    }

    private static Button FindButton(MessageDialog dialog, string content)
    {
        return dialog.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == content);
    }
}
