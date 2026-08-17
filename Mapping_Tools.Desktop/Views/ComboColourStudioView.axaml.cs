using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>Presents the Avalonia Combo Colour Studio editor.</summary>
public partial class ComboColourStudioView : UserControl
{
    /// <summary>Creates the feature view and loads its compiled AXAML.</summary>
    public ComboColourStudioView()
    {
        InitializeComponent();
    }

    private void AddColourPointButtonPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Shift) == 0 ||
            DataContext is not ComboColourStudioViewModel viewModel)
        {
            return;
        }

        eventArgs.Handled = true;
        viewModel.AddColourPointAtEditorTimeCommand.Execute(null);
    }

    private void RemoveSequenceColour_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ComboColourStudioViewModel viewModel ||
            sender is not Button { Tag: SpecialColour colour })
        {
            return;
        }

        viewModel.RemoveSequenceColourCommand.Execute(colour);
    }
}
