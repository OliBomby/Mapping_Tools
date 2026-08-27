using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;

/// <summary>Presents the Avalonia Hitsound Studio editor and export surface.</summary>
public sealed partial class HitsoundStudioView : UserControl
{
    /// <summary>Creates the Hitsound Studio view.</summary>
    public HitsoundStudioView()
    {
        InitializeComponent();
        RaiseButton.PointerPressed += RaiseLayers;
        RaiseButton.KeyDown += RaiseLayersKeyDown;
        LowerButton.PointerPressed += LowerLayers;
        LowerButton.KeyDown += LowerLayersKeyDown;
    }

    private void LayersSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is HitsoundStudioViewModel viewModel && sender is MaterialGridListView grid)
            viewModel.SetSelection(grid.SelectedItems?.OfType<ObservableHitsoundLayer>() ?? []);
    }

    private void LayersDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HitsoundStudioViewModel viewModel) viewModel.PreviewCommand.Execute(null);
    }

    private void RaiseLayers(object? sender, PointerEventArgs e)
    {
        MoveLayers(-1, e.KeyModifiers);
        e.Handled = true;
    }

    private void LowerLayers(object? sender, PointerEventArgs e)
    {
        MoveLayers(1, e.KeyModifiers);
        e.Handled = true;
    }

    private void RaiseLayersKeyDown(object? sender, KeyEventArgs e)
    {
        MoveLayersFromKeyboard(-1, e);
    }

    private void LowerLayersKeyDown(object? sender, KeyEventArgs e)
    {
        MoveLayersFromKeyboard(1, e);
    }

    private void MoveLayersFromKeyboard(int direction, KeyEventArgs e)
    {
        if (e.Key is not (Key.Space or Key.Enter)) return;

        MoveLayers(direction, e.KeyModifiers);
        e.Handled = true;
    }

    private void MoveLayers(int direction, KeyModifiers modifiers)
    {
        if (DataContext is HitsoundStudioViewModel viewModel) viewModel.MoveSelectedLayers(direction, (modifiers & KeyModifiers.Shift) != 0);
    }
}
