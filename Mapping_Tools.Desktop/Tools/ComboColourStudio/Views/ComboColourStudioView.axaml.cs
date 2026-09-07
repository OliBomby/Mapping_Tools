using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Core.BeatmapHelper;
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

    private void AddSequenceColourButtonClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not ComboColourStudioViewModel viewModel
            || sender is not Button button
            || button.DataContext is not ObservableColourPoint point)
            return;

        ContextMenu contextMenu = new()
        {
            Placement = PlacementMode.Bottom,
        };

        if (viewModel.ComboColours.Count == 0)
        {
            contextMenu.Items.Add(new MenuItem
            {
                Header = "Add at least one combo colour before adding colours to this sequence.",
                IsEnabled = false,
            });
        }
        else
        {
            foreach (var colour in viewModel.ComboColours)
            {
                contextMenu.Items.Add(new MenuItem
                {
                    Header = colour.Name ?? string.Empty,
                    Icon = CreateColourMenuIcon(colour.Color),
                    Command = new RelayCommand(() => viewModel.AddSequenceColour(point, colour)),
                });
            }
        }

        button.ContextMenu = contextMenu;
        contextMenu.Open(button);
        eventArgs.Handled = true;
    }

    private static Border CreateColourMenuIcon(RgbaColour colour)
    {
        return new Border
        {
            Width = 16,
            Height = 16,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(colour.A, colour.R, colour.G, colour.B)),
        };
    }
}
