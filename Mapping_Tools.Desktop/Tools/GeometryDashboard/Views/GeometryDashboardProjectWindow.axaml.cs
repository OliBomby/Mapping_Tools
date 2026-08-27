using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using GeometryDashboardProjectSlotsViewModel = Mapping_Tools.Desktop.Tools.GeometryDashboard.Interactions.GeometryDashboardProjectSlotsViewModel;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard save-slot editor.</summary>
public sealed partial class GeometryDashboardProjectWindow : Window
{
    /// <summary>Creates the save-slot window.</summary>
    public GeometryDashboardProjectWindow()
    {
        InitializeComponent();
    }

    private void SaveSlotsSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is GeometryDashboardProjectSlotsViewModel viewModel && sender is ListBox listBox)
            viewModel.SetSelectedSlots(listBox.SelectedItems?.OfType<SnappingToolsSaveSlot>() ?? []);
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

    private void ResizeWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        GeometryDashboardWindowChrome.Resize(this, sender, eventArgs);
    }
}

