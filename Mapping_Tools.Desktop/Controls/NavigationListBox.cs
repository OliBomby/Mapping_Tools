using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Mapping_Tools.Desktop.ViewModels;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Presents selectable shell features while realizing section dividers as
///     inert separators instead of selectable item containers.
/// </summary>
public sealed class NavigationListBox : ListBox
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(ListBox);

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(
        object? item,
        int index,
        out object? recycleKey)
    {
        if (item is NavigationDividerViewModel)
        {
            recycleKey = typeof(Separator);
            return true;
        }

        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(
        object? item,
        int index,
        object? recycleKey)
    {
        if (item is NavigationDividerViewModel)
        {
            Separator separator = new()
            {
                Focusable = false,
            };
            separator.Classes.Add("navigation-divider");
            return separator;
        }

        return new NavigationListBoxItem();
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(
        Control container,
        object? item,
        int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is NavigationListBoxItem navigationItem && item is ShellFeatureItemViewModel feature)
            navigationItem.Prepare(feature);
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(Control container)
    {
        if (container is NavigationListBoxItem navigationItem) navigationItem.Clear();

        base.ClearContainerForItemOverride(container);
    }
}

internal sealed class NavigationListBoxItem : ListBoxItem
{
    protected override Type StyleKeyOverride => typeof(ListBoxItem);

    internal void Prepare(ShellFeatureItemViewModel item)
    {
        Prepare(
            item,
            new MaterialIcon
            {
                Kind = item.IsFavorite
                    ? MaterialIconKind.Star
                    : MaterialIconKind.StarOutline,
            });
    }

    internal void Prepare(ShellFeatureItemViewModel item, object? icon)
    {
        ToolTip.SetTip(this, item.Description);
        ContextMenu = new ContextMenu
        {
            ItemsSource =
                new object[]
                {
                    new MenuItem
                    {
                        Header = item.IsFavorite ? "Unfavorite" : "Favorite",
                        Command = item.ToggleFavoriteCommand,
                        Icon = icon,
                    },
                },
        };
    }

    internal void Clear()
    {
        ToolTip.SetTip(this, null);
        ContextMenu = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs eventArgs)
    {
        bool activates = ShouldActivate(eventArgs);
        base.OnPointerPressed(eventArgs);

        if (activates) Activate();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs eventArgs)
    {
        bool activates = ShouldActivate(eventArgs);
        base.OnPointerReleased(eventArgs);

        if (activates) Activate();
    }

    private bool ShouldActivate(PointerEventArgs eventArgs)
    {
        return eventArgs.Properties.PointerUpdateKind is
                   PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased
               && ItemSelectionEventTriggers.ShouldTriggerSelection(this, eventArgs);
    }

    private void Activate()
    {
        if (DataContext is ShellFeatureItemViewModel item) item.ActivateCommand.Execute(null);
    }
}
