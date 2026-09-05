using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.ComboColourStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.ComboColourStudio.Views;

/// <summary>Presents the Avalonia Combo Colour Studio editor.</summary>
public partial class ComboColourStudioView : UserControl
{
    /// <summary>Creates the feature view and loads its compiled AXAML.</summary>
    public ComboColourStudioView()
    {
        InitializeComponent();
    }

    private void ColourPointsSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is ComboColourStudioViewModel viewModel && sender is DataGrid grid)
            viewModel.SetSelectedColourPoints(grid.SelectedItems?.OfType<ObservableColourPoint>() ?? []);
    }

    private void AddColourPointButtonPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Shift) == 0 || DataContext is not ComboColourStudioViewModel viewModel)
            return;

        eventArgs.Handled = true;
        viewModel.AddColourPointAtEditorTimeCommand.Execute(null);
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
