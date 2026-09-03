using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard preferences dialog.</summary>
public sealed partial class GeometryDashboardPreferencesWindow : Window
{
    /// <summary>Creates the preferences window.</summary>
    public GeometryDashboardPreferencesWindow()
    {
        InitializeComponent();
    }

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs)
    {
        Close(null);
    }

    private void DragWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(eventArgs);
    }

    private void ToggleMaximizeWindow(object? sender, TappedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

}
