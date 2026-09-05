using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.MapsetMerger.Views;

/// <summary>Hosts the Avalonia Mapset Merger form.</summary>
public partial class MapsetMergerView : UserControl
{
    private readonly ButtonModifierCapture addMapsetButtonModifiers;

    /// <summary>Creates the Mapset Merger view.</summary>
    public MapsetMergerView()
    {
        InitializeComponent();
        addMapsetButtonModifiers = new ButtonModifierCapture(AddMapsetButton);
    }

    private async void AddMapsetButtonClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not MapsetMergerViewModel viewModel) return;

        if (addMapsetButtonModifiers.Consume().HasFlag(KeyModifiers.Shift))
            await viewModel.AddMapsetFromCurrentCommand.ExecuteAsync(null);
        else
            await viewModel.AddMapsetCommand.ExecuteAsync(null);

        eventArgs.Handled = true;
    }
}
