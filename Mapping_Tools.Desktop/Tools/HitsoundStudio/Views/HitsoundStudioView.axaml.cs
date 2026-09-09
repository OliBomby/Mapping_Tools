using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.ComponentModel;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;

/// <summary>Presents the Avalonia Hitsound Studio editor and export surface.</summary>
public sealed partial class HitsoundStudioView : UserControl
{
    private readonly ButtonModifierCapture raiseButtonModifiers;
    private readonly ButtonModifierCapture lowerButtonModifiers;
    private INotifyPropertyChanged? observedViewModel;

    /// <summary>Creates the Hitsound Studio view.</summary>
    public HitsoundStudioView()
    {
        InitializeComponent();
        DataContextChanged += ViewDataContextChanged;
        raiseButtonModifiers = new ButtonModifierCapture(RaiseButton);
        lowerButtonModifiers = new ButtonModifierCapture(LowerButton);
        UpdateEditorLayout(DataContext as HitsoundStudioViewModel);
    }

    private void ViewDataContextChanged(object? sender, EventArgs e)
    {
        if (observedViewModel is not null)
            observedViewModel.PropertyChanged -= ViewModelPropertyChanged;

        observedViewModel = DataContext as INotifyPropertyChanged;
        if (observedViewModel is not null)
            observedViewModel.PropertyChanged += ViewModelPropertyChanged;

        UpdateEditorLayout(DataContext as HitsoundStudioViewModel);
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HitsoundStudioViewModel.HasLayers))
            UpdateEditorLayout(DataContext as HitsoundStudioViewModel);
    }

    private void UpdateEditorLayout(HitsoundStudioViewModel? viewModel)
    {
        var hasLayers = viewModel?.HasLayers == true;
        EditorAndLayersGrid.ColumnDefinitions[0].Width = hasLayers
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);
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

    private void RaiseLayers(object? sender, RoutedEventArgs e)
    {
        MoveLayers(-1, raiseButtonModifiers.Consume());
        e.Handled = true;
    }

    private void LowerLayers(object? sender, RoutedEventArgs e)
    {
        MoveLayers(1, lowerButtonModifiers.Consume());
        e.Handled = true;
    }

    private void MoveLayers(int direction, KeyModifiers modifiers)
    {
        if (DataContext is HitsoundStudioViewModel viewModel) viewModel.MoveSelectedLayers(direction, (modifiers & KeyModifiers.Shift) != 0);
    }
}
