using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard generator list and dashboard actions.</summary>
public sealed partial class GeometryDashboardView : UserControl
{
    private bool restoreGeneratorsOffset;
    private double savedGeneratorsOffset;

    /// <summary>Creates the dashboard view.</summary>
    public GeometryDashboardView()
    {
        InitializeComponent();
    }

    private void ToggleSelected(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            (DataContext as GeometryDashboardViewModel)?.ToggleSelected(eventArgs.KeyModifiers);
    }

    private void ToggleLocked(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            (DataContext as GeometryDashboardViewModel)?.ToggleLocked(eventArgs.KeyModifiers);
    }

    private void ToggleInheritable(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            (DataContext as GeometryDashboardViewModel)?.ToggleInheritable(eventArgs.KeyModifiers);
    }

    private async void ShowPreferences(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is GeometryDashboardViewModel viewModel) await viewModel.ShowPreferencesAsync();
    }

    private async void ShowProjects(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is GeometryDashboardViewModel viewModel) await viewModel.ShowProjectSlotsAsync();
    }

    private void GeneratorsPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not ScrollViewer scrollViewer || !eventArgs.GetCurrentPoint(scrollViewer).Properties.IsLeftButtonPressed)
            return;

        savedGeneratorsOffset = scrollViewer.Offset.Y;
        restoreGeneratorsOffset = eventArgs.Source is not ScrollViewer;
    }

    private void GeneratorsScrollChanged(object? sender, ScrollChangedEventArgs eventArgs)
    {
        if (sender is ScrollViewer scrollViewer && restoreGeneratorsOffset && Math.Abs(scrollViewer.Offset.Y - savedGeneratorsOffset) > 0.5)
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, savedGeneratorsOffset);
    }

    private void GeneratorsPointerWheelChanged(object? sender, PointerWheelEventArgs eventArgs)
    {
        if (sender is not ScrollViewer scrollViewer) return;

        scrollViewer.Offset = new Vector(
            scrollViewer.Offset.X,
            Math.Max(0, scrollViewer.Offset.Y - eventArgs.Delta.Y * 50));
        eventArgs.Handled = true;
    }
}
