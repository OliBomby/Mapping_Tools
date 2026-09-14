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

}
