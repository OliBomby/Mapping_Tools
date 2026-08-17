using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Views;

/// <summary>Displays Pattern Gallery's collection cards and placement options.</summary>
public sealed partial class PatternGalleryView : UserControl
{
    /// <summary>Creates the Pattern Gallery view and loads its compiled AXAML.</summary>
    public PatternGalleryView()
    {
        InitializeComponent();
    }

    private void PatternPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is not Control control ||
            control.DataContext is not PatternGalleryItemViewModel item ||
            DataContext is not PatternGalleryViewModel viewModel)
        {
            return;
        }

        PointerPoint point = eventArgs.GetCurrentPoint(control);
        if (point.Properties.IsLeftButtonPressed && eventArgs.Source is not CheckBox)
        {
            viewModel.SelectOnly(item);
        }
    }

    private async void PatternDoubleTapped(object? sender, TappedEventArgs eventArgs)
    {
        if (DataContext is PatternGalleryViewModel viewModel &&
            sender is Control { DataContext: PatternGalleryItemViewModel item })
        {
            viewModel.SelectOnly(item);
            await viewModel.RunQuickAsync(CancellationToken.None);
        }
    }

    private async void CollectionNamePointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            DataContext is PatternGalleryViewModel viewModel)
        {
            await viewModel.RenameCollectionCommand.ExecuteAsync(null);
            eventArgs.Handled = true;
        }
    }

    private async void RemoveButtonPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Shift) == 0 ||
            DataContext is not PatternGalleryViewModel viewModel)
        {
            return;
        }

        eventArgs.Handled = true;
        await viewModel.RemoveSelectedAsync(skipConfirmation: true);
    }

    private void PatternContextMenuOpened(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not ContextMenu menu || DataContext is not PatternGalleryViewModel viewModel)
        {
            return;
        }

        PatternGalleryItemViewModel? item =
            (menu.PlacementTarget as Control)?.DataContext as PatternGalleryItemViewModel ??
            menu.DataContext as PatternGalleryItemViewModel;
        if (item is null)
        {
            return;
        }

        viewModel.SelectOnly(item);
        menu.Items.Clear();
        menu.Items.Add(new MenuItem { Header = "Delete", Command = viewModel.RemoveCommand });
        menu.Items.Add(new MenuItem { Header = "Open in Explorer", Command = viewModel.OpenExplorerSelectedCommand });
        menu.Items.Add(new MenuItem { Header = "Properties", Command = viewModel.ShowDetailsCommand });
        menu.Items.Add(new Separator());

        MenuItem groupMenu = new() { Header = "Group" };
        groupMenu.Items.Add(new MenuItem
        {
            Header = "None",
            Command = viewModel.AssignGroupCommand,
            CommandParameter = string.Empty
        });
        foreach (string group in viewModel.GroupNames)
        {
            groupMenu.Items.Add(new MenuItem
            {
                Header = group,
                Command = viewModel.AssignGroupCommand,
                CommandParameter = group
            });
        }

        groupMenu.Items.Add(new Separator());
        groupMenu.Items.Add(new MenuItem { Header = "Type new group name...", Command = viewModel.NewGroupCommand });
        groupMenu.Items.Add(new MenuItem { Header = "Rename group...", Command = viewModel.RenameGroupCommand });
        menu.Items.Add(groupMenu);
    }
}
