using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Tools.RhythmGuide.Views;

/// <summary>Hosts the shared Rhythm Guide view model in a modeless auxiliary window.</summary>
public sealed partial class RhythmGuideWindow : Window
{
    /// <summary>Creates the auxiliary Rhythm Guide window.</summary>
    public RhythmGuideWindow()
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

    private void ToggleMaximizeWindow(object? sender, TappedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

}
