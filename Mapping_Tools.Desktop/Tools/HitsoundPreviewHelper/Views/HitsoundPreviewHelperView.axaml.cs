using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;

namespace Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.Views;

/// <summary>Presents the Avalonia Hitsound Preview Helper form.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public sealed partial class HitsoundPreviewHelperView : UserControl
{
    private readonly ButtonModifierCapture addButtonModifiers;

    /// <summary>Creates the Hitsound Preview Helper view.</summary>
    public HitsoundPreviewHelperView()
    {
        InitializeComponent();
        addButtonModifiers = new ButtonModifierCapture(AddButton);
    }

    private void DataGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not DataGrid grid || e.InitialPressMouseButton != MouseButton.Left
            || grid.DataContext is not HitsoundPreviewHelperViewModel) return;
        if (e.Source is not Control source || source.FindAncestorOfType<DataGridCell>() is null) return;

        grid.BeginEdit();
    }

    private void AddButtonClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HitsoundPreviewHelperViewModel viewModel) return;

        bool addFromSelection = addButtonModifiers.Consume().HasFlag(KeyModifiers.Shift);

        if (addFromSelection)
            viewModel.AddFromSelectionCommand.Execute(null);
        else
            viewModel.AddCommand.Execute(null);

        e.Handled = true;
    }
}
