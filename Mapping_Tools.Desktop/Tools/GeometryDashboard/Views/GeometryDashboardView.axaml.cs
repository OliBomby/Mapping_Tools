using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts the Geometry Dashboard generator list and dashboard actions.</summary>
public sealed partial class GeometryDashboardView : UserControl
{
    private readonly ButtonModifierCapture toggleSelectedButtonModifiers;
    private readonly ButtonModifierCapture toggleLockedButtonModifiers;
    private readonly ButtonModifierCapture toggleInheritableButtonModifiers;
    private bool restoreGeneratorsOffset;
    private double savedGeneratorsOffset;

    /// <summary>Creates the dashboard view.</summary>
    public GeometryDashboardView()
    {
        InitializeComponent();
        toggleSelectedButtonModifiers = new ButtonModifierCapture(ToggleSelectedButton);
        toggleLockedButtonModifiers = new ButtonModifierCapture(ToggleLockedButton);
        toggleInheritableButtonModifiers = new ButtonModifierCapture(ToggleInheritableButton);
    }

    private void ToggleSelectedClick(object? sender, RoutedEventArgs eventArgs)
    {
        (DataContext as GeometryDashboardViewModel)?.ToggleSelected(toggleSelectedButtonModifiers.Consume());
        eventArgs.Handled = true;
    }

    private void ToggleLockedClick(object? sender, RoutedEventArgs eventArgs)
    {
        (DataContext as GeometryDashboardViewModel)?.ToggleLocked(toggleLockedButtonModifiers.Consume());
        eventArgs.Handled = true;
    }

    private void ToggleInheritableClick(object? sender, RoutedEventArgs eventArgs)
    {
        (DataContext as GeometryDashboardViewModel)?.ToggleInheritable(toggleInheritableButtonModifiers.Consume());
        eventArgs.Handled = true;
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
