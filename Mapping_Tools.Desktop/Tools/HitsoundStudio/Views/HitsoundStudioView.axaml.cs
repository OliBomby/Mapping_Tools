using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;

/// <summary>Presents the Avalonia Hitsound Studio editor and export surface.</summary>
public sealed partial class HitsoundStudioView : UserControl
{
    private readonly ButtonModifierCapture raiseButtonModifiers;
    private readonly ButtonModifierCapture lowerButtonModifiers;

    /// <summary>Creates the Hitsound Studio view.</summary>
    public HitsoundStudioView()
    {
        InitializeComponent();
        raiseButtonModifiers = new ButtonModifierCapture(RaiseButton);
        lowerButtonModifiers = new ButtonModifierCapture(LowerButton);
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
        if (DataContext is not HitsoundStudioViewModel viewModel) return;

        viewModel.MoveSelectedLayers(direction, (modifiers & KeyModifiers.Shift) != 0);
    }
}
