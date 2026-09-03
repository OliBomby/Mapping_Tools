using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Mapping_Tools.Desktop.ViewModels.Dialogs;

namespace Mapping_Tools.Desktop.Views.Dialogs;

/// <summary>
///     Renders a reusable message dialog with typed actions supplied by its view model.
/// </summary>
public partial class MessageDialog : Window
{
    /// <summary>
    ///     Loads the compiled message-dialog view.
    /// </summary>
    public MessageDialog()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateActionAlignment();
        AttachedToVisualTree += (_, _) => UpdateActionAlignment();
    }

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }

    private void DragWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(eventArgs);
    }

    private void ToggleMaximizeWindow(object? sender, TappedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateActionAlignment()
    {
        ActionPanel.HorizontalAlignment = DataContext is MessageDialogViewModel { Choices.Count: 2 }
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Right;
    }
}
