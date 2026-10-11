using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Tools.MapsetMerger.ViewModels;

namespace Mapping_Tools.Desktop.Tools.MapsetMerger.Views;

/// <summary>Hosts the Avalonia Mapset Merger form.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public partial class MapsetMergerView : UserControl
{
    private readonly ButtonModifierCapture addMapsetButtonModifiers;

    /// <summary>Creates the Mapset Merger view.</summary>
    public MapsetMergerView()
    {
        InitializeComponent();
        addMapsetButtonModifiers = new ButtonModifierCapture(AddMapsetButton);
    }

    private void DataGridPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        if (e.EditingElement is TextBox textBox)
        {
            textBox.Focus();
            return;
        }

        if (e.EditingElement.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() is { } childTextBox)
            childTextBox.Focus();
    }

    private void DataGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is DataGrid grid)
            DataGridTabNavigation.HandleKeyDown(grid, e);
    }

    private void DataGridPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not DataGrid grid || e.InitialPressMouseButton != MouseButton.Left) return;
        if (e.Source is not Control source || source.FindAncestorOfType<DataGridCell>() is null) return;

        grid.BeginEdit();
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
