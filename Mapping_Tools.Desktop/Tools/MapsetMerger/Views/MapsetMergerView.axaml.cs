using Avalonia.Controls;
using Avalonia.Input;
using Mapping_Tools.Desktop.ViewModels;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;

namespace Mapping_Tools.Desktop.Tools.MapsetMerger.Views;

/// <summary>Hosts the Avalonia Mapset Merger form.</summary>
public partial class MapsetMergerView : UserControl
{
    /// <summary>Creates the Mapset Merger view.</summary>
    public MapsetMergerView()
    {
        InitializeComponent();
    }

    private void AddMapsetButtonPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Shift) == 0 || DataContext is not MapsetMergerViewModel viewModel)
            return;

        eventArgs.Handled = true;
        viewModel.AddMapsetFromCurrentCommand.Execute(null);
    }
}
