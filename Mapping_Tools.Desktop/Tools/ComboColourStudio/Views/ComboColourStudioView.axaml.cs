using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.Views;

/// <summary>Presents the Avalonia Combo Colour Studio editor.</summary>
public partial class ComboColourStudioView : UserControl
{
    private readonly ButtonModifierCapture addColourPointButtonModifiers;

    /// <summary>Creates the feature view and loads its compiled AXAML.</summary>
    public ComboColourStudioView()
    {
        InitializeComponent();
        addColourPointButtonModifiers = new ButtonModifierCapture(AddColourPointButton);
    }

    private void ColourPointsSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is ComboColourStudioViewModel viewModel && sender is DataGrid grid)
            viewModel.SetSelectedColourPoints(grid.SelectedItems?.OfType<ObservableColourPoint>() ?? []);
    }

    private async void AddColourPointButtonClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ComboColourStudioViewModel viewModel) return;

        if (addColourPointButtonModifiers.Consume().HasFlag(KeyModifiers.Shift))
            await viewModel.AddColourPointAtEditorTimeCommand.ExecuteAsync(null);
        else
            viewModel.AddColourPointCommand.Execute(null);

        eventArgs.Handled = true;
    }

    private void RemoveSequenceColour_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ComboColourStudioViewModel viewModel || sender is not Button button)
            return;

        var item = button.FindAncestorOfType<ListBoxItem>(includeSelf: true);
        var listBox = item?.FindAncestorOfType<ListBox>(includeSelf: true);
        if (item is null || listBox is null) return;

        int index = listBox.IndexFromContainer(item);
        if (index >= 0) viewModel.RemoveSequenceColourAt(index);
    }
}
