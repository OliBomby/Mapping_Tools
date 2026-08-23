using Avalonia.Controls;
using Avalonia.Input;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>Presents the Avalonia Hitsound Preview Helper form.</summary>
public sealed partial class HitsoundPreviewHelperView : UserControl
{
    /// <summary>Creates the Hitsound Preview Helper view.</summary>
    public HitsoundPreviewHelperView()
    {
        InitializeComponent();
    }

    private void AddButtonPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Shift) == 0) return;
        if (DataContext is not HitsoundPreviewHelperViewModel viewModel) return;

        e.Handled = true;
        viewModel.AddFromSelectionCommand.Execute(null);
    }
}
