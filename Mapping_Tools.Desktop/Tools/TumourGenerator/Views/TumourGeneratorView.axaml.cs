using Avalonia.Controls;
using Avalonia.Input;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Adapters;

namespace Mapping_Tools.Desktop.Tools.TumourGenerator.Views;

/// <summary>Displays Tumour Generator 2 layers, graph-backed settings, and preview.</summary>
public sealed partial class TumourGeneratorView : UserControl
{
    /// <summary>Creates the view and enables compiled bindings.</summary>
    public TumourGeneratorView()
    {
        InitializeComponent();
    }

    private void LayerNamePointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (DataContext is TumourGeneratorViewModel viewModel && sender is Control { DataContext: ObservableTumourLayer layer })
            viewModel.CurrentLayer = layer;
    }
}
