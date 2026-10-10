using Avalonia.Controls;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Converters;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Views.Dialogs;

[TestClass]
public sealed class ValueDialogViewTests
{
    [TestMethod]
    public void ValueTextBox_OnLoad_FocusesAndSelectsExistingText()
    {
        // Arrange
        ValueDialogViewModel viewModel = CreateViewModel(() => { });
        ValueDialog dialog = new() { DataContext = viewModel };

        // Act
        using HeadlessViewHost host = HeadlessViewHost.Show(dialog);
        TextBox input = host.Find<TextBox>("ValueTextBox");

        // Assert
        input.IsFocused.Should().BeTrue();
        input.SelectionStart.Should().Be(0);
        input.SelectionEnd.Should().Be(input.Text!.Length);
    }

    [TestMethod]
    public void CancelButton_RealClick_DismissesWithoutAcceptance()
    {
        // Arrange
        int accepted = 0;
        int cancelled = 0;
        ValueDialogViewModel viewModel = CreateViewModel(() => cancelled++, _ => accepted++);
        ValueDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(dialog);
        Button cancel = dialog.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Content?.ToString() == "Cancel");

        // Act
        host.Click(cancel);

        // Assert
        accepted.Should().Be(0);
        cancelled.Should().Be(1);
    }

    private static ValueDialogViewModel CreateViewModel(Action cancel, Action<object?>? accept = null)
    {
        return new ValueDialogViewModel(
            "Type a value",
            "Value",
            42,
            new InvariantInt32Converter(),
            typeof(int),
            "OK",
            "Cancel",
            _ => null,
            accept ?? (_ => { }),
            cancel);
    }
}
