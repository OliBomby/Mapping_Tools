using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Views;

/// <summary>
///     Displays the release notes, updater decisions, and package progress.
/// </summary>
public sealed partial class UpdaterWindow : Window
{
    /// <summary>Creates the updater window; its state is supplied by the data context.</summary>
    public UpdaterWindow()
    {
        InitializeComponent();
    }

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs)
    {
        Close();
    }

    private void DragWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(eventArgs);
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs eventArgs)
    {
        WindowState = WindowState.Minimized;
    }

    private void ToggleMaximizeWindow(object? sender, RoutedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void ResizeWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is Control { Tag: string edge } && Enum.TryParse(edge, out WindowEdge windowEdge)
            && eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginResizeDrag(windowEdge, eventArgs);
    }
}
