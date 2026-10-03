using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;

namespace Mapping_Tools.Desktop.Tools.PatternGallery.Views;

/// <summary>Displays Pattern Gallery's collection cards and placement options.</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public sealed partial class PatternGalleryView : UserControl
{
    private readonly ButtonModifierCapture removeButtonModifiers;
    private ListBox? selectedPatternList;

    /// <summary>Creates the Pattern Gallery view and loads its compiled AXAML.</summary>
    public PatternGalleryView()
    {
        InitializeComponent();
        removeButtonModifiers = new ButtonModifierCapture(RemoveButton);
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
            eventArgs.Handled = true;
            await viewModel.RunPatternQuickAsync(item, CancellationToken.None);
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

    private async void RemoveButtonClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not PatternGalleryViewModel viewModel) return;

        if (removeButtonModifiers.Consume().HasFlag(KeyModifiers.Shift))
            await viewModel.RemoveSelectedAsync(true);
        else
            await viewModel.RemoveCommand.ExecuteAsync(null);

        eventArgs.Handled = true;
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
            Header = DesktopStrings.PatternGallery_DeleteMenu,
            Command = viewModel.RemoveCommand,
        };
        ToolTip.SetTip(deleteItem, DesktopStrings.PatternGallery_DeleteButtonTip);
        menu.Items.Add(deleteItem);

        MenuItem openItem = new()
        {
            Header = DesktopStrings.PatternGallery_OpenInExplorer,
            Command = viewModel.OpenExplorerSelectedCommand,
        };
        ToolTip.SetTip(openItem, DesktopStrings.PatternGallery_OpenSourceFilesTip);
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());
        MenuItem groupMenu = new() { Header = DesktopStrings.PatternGallery_GroupMenu };
        groupMenu.Items.Add(new MenuItem
        {
            Header = DesktopStrings.Common_None,
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
        groupMenu.Items.Add(new MenuItem { Header = DesktopStrings.PatternGallery_NewGroupName, Command = viewModel.NewGroupCommand });
        groupMenu.Items.Add(new MenuItem { Header = DesktopStrings.PatternGallery_RenameGroup, Command = viewModel.RenameGroupCommand });
        menu.Items.Add(groupMenu);
        MenuItem propertiesItem = new()
        {
            Header = DesktopStrings.PatternGallery_PropertiesMenu,
            Command = viewModel.ShowDetailsCommand,
        };
        ToolTip.SetTip(propertiesItem, DesktopStrings.PatternGallery_ViewPropertiesTip);
        menu.Items.Add(propertiesItem);
    }
}
