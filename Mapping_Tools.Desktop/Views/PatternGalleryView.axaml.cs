using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.ViewModels;
using PatternGalleryItemViewModel = Mapping_Tools.Desktop.ViewModels.PatternGallery.PatternGalleryItemViewModel;
using PatternGalleryViewModel = Mapping_Tools.Desktop.ViewModels.PatternGallery.PatternGalleryViewModel;

namespace Mapping_Tools.Desktop.Views;

/// <summary>Displays Pattern Gallery's collection cards and placement options.</summary>
public sealed partial class PatternGalleryView : UserControl
{
    private ListBox? selectedPatternList;

    /// <summary>Creates the Pattern Gallery view and loads its compiled AXAML.</summary>
    public PatternGalleryView()
    {
        InitializeComponent();
    }

    private void PatternPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not Control control || control.DataContext is not PatternGalleryItemViewModel item || DataContext is not PatternGalleryViewModel viewModel)
            return;

        var point = eventArgs.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed && eventArgs.Source is not CheckBox) viewModel.SelectOnly(item);
    }

    private async void PatternDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is PatternGalleryViewModel viewModel && sender is Control { DataContext: PatternGalleryItemViewModel item })
        {
            viewModel.SelectOnly(item);
            await viewModel.RunQuickAsync(CancellationToken.None);
        }
    }

    private void PatternSelectionChanged(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (DataContext is not PatternGalleryViewModel viewModel) return;

        if (sender is ListBox list && !ReferenceEquals(selectedPatternList, list))
        {
            if (selectedPatternList is not null) selectedPatternList.SelectedItem = null;

            selectedPatternList = list;
        }

        var item = eventArgs.AddedItems
            .OfType<PatternGalleryItemViewModel>()
            .FirstOrDefault();
        if (item is not null) viewModel.SelectOnly(item);
    }

    private async void CollectionNamePointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed && DataContext is PatternGalleryViewModel viewModel)
        {
            await viewModel.RenameCollectionCommand.ExecuteAsync(null);
            eventArgs.Handled = true;
        }
    }

    private async void RemoveButtonPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Shift) == 0 || DataContext is not PatternGalleryViewModel viewModel)
            return;

        eventArgs.Handled = true;
        await viewModel.RemoveSelectedAsync(true);
    }

    private void PatternContextMenuOpened(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not ContextMenu menu || DataContext is not PatternGalleryViewModel viewModel) return;

        var item =
            menu.PlacementTarget?.DataContext as PatternGalleryItemViewModel ?? menu.DataContext as PatternGalleryItemViewModel;
        if (item is null) return;

        viewModel.SelectOnly(item);
        menu.Items.Clear();
        MenuItem deleteItem = new()
        {
            Header = "_Delete",
            Command = viewModel.RemoveCommand,
        };
        ToolTip.SetTip(deleteItem, "Delete selected patterns. Hold shift to skip dialog.");
        menu.Items.Add(deleteItem);

        MenuItem openItem = new()
        {
            Header = "_Open in File Explorer",
            Command = viewModel.OpenExplorerSelectedCommand,
        };
        ToolTip.SetTip(openItem, "Open the source files of the selected patterns in the File Explorer.");
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        MenuItem groupMenu = new() { Header = "_Group" };
        groupMenu.Items.Add(new MenuItem
        {
            Header = "None",
            Command = viewModel.AssignGroupCommand,
            CommandParameter = string.Empty,
        });
        foreach (string group in viewModel.GroupNames)
            groupMenu.Items.Add(new MenuItem
            {
                Header = group,
                Command = viewModel.AssignGroupCommand,
                CommandParameter = group,
            });

        groupMenu.Items.Add(new Separator());
        groupMenu.Items.Add(new MenuItem { Header = "Type new group name...", Command = viewModel.NewGroupCommand });
        groupMenu.Items.Add(new MenuItem { Header = "Rename group...", Command = viewModel.RenameGroupCommand });
        menu.Items.Add(groupMenu);
        MenuItem propertiesItem = new()
        {
            Header = "_Properties",
            Command = viewModel.ShowDetailsCommand,
        };
        ToolTip.SetTip(propertiesItem, "View additional properties of the pattern.");
        menu.Items.Add(propertiesItem);
    }
}
