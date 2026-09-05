using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Views;

/// <summary>Presents the Avalonia Hitsound Preview Helper form.</summary>
public sealed partial class HitsoundPreviewHelperView : UserControl
{
    private readonly ButtonModifierCapture addButtonModifiers;

    /// <summary>Creates the Hitsound Preview Helper view.</summary>
    public HitsoundPreviewHelperView()
    {
        InitializeComponent();
        addButtonModifiers = new ButtonModifierCapture(AddButton);
    }

    private void AddButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HitsoundPreviewHelperViewModel viewModel) return;

        bool addFromSelection = addButtonModifiers.Consume().HasFlag(KeyModifiers.Shift);

        if (addFromSelection)
        {
            viewModel.AddFromSelectionCommand.Execute(null);
        }
        else
        {
            viewModel.AddCommand.Execute(null);
        }

        e.Handled = true;
    }
}
