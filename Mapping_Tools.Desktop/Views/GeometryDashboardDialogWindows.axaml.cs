using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Interactions;

namespace Mapping_Tools.Desktop.Views;

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

    private void ResizeWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        GeometryDashboardWindowChrome.Resize(this, sender, eventArgs);
    }
}

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

    private void ResizeWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        GeometryDashboardWindowChrome.Resize(this, sender, eventArgs);
    }
}

internal static class GeometryDashboardWindowChrome
{
    public static void Resize(Window window, object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is Control { Tag: string edge } && Enum.TryParse(edge, out WindowEdge windowEdge) && eventArgs.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            window.BeginResizeDrag(windowEdge, eventArgs);
    }
}
