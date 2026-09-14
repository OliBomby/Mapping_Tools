using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Interactions;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard savestate editor.</summary>
public sealed partial class GeometryDashboardSavestatesWindow : Window
{
    /// <summary>Creates the savestate window.</summary>
    public GeometryDashboardSavestatesWindow()
    {
        InitializeComponent();
    }

    private void SaveSlotsSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is GeometryDashboardSavestatesViewModel viewModel && sender is MaterialGridListView listView)
            viewModel.SetSelectedSlots(listView.SelectedItems?.OfType<GeometryDashboardSaveSlot>() ?? []);
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
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
}
