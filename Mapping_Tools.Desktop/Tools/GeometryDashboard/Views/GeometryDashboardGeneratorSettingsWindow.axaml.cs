using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Interactions;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Views;

/// <summary>Hosts one generator's reflected settings.</summary>
public sealed partial class GeometryDashboardGeneratorSettingsWindow : Window
{
    /// <summary>Creates the generator settings window.</summary>
    public GeometryDashboardGeneratorSettingsWindow()
    {
        InitializeComponent();
    }

    private void PredicatesSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is GeometryDashboardGeneratorSettingsDialogViewModel viewModel && sender is ListBox listBox)
            viewModel.SetSelectedPredicates(listBox.SelectedItems?.OfType<SelectionPredicate>() ?? []);
    }

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs)
    {
        Close(false);
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
