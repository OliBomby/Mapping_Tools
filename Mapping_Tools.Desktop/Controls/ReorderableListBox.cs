using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     A ListBox that preserves its bound list's order when an item is dragged
///     onto another item in the same control.
/// </summary>
public sealed class ReorderableListBox : ListBox
{
    private static readonly DataFormat<object> itemFormat =
        DataFormat.CreateInProcessFormat<object>("mapping-tools-reorderable-list-item");

    private bool dragStarted;
    private PointerPressedEventArgs? pressEventArgs;
    private Point pressPoint;
    private int pressedIndex = -1;
    private object? pressedItem;

    /// <summary>Creates a list box with Avalonia 12 drag/drop handlers installed.</summary>
    public ReorderableListBox()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        pressPoint = eventArgs.GetPosition(this);
        var container = FindItemContainer(eventArgs.Source as Visual);
        pressedItem = container?.DataContext;
        pressedIndex = container is null ? -1 : IndexFromContainer(container);
        pressEventArgs = eventArgs;
        dragStarted = false;
    }

    private async void OnPointerMoved(object? sender, PointerEventArgs eventArgs)
    {
        if (dragStarted || pressedItem is null || !eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var currentPoint = eventArgs.GetPosition(this);
        if (Math.Abs(currentPoint.X - pressPoint.X) < 4 && Math.Abs(currentPoint.Y - pressPoint.Y) < 4)
            return;

        dragStarted = true;
        DataTransfer transfer = new();
        if (pressedIndex < 0)
        {
            dragStarted = false;
            return;
        }

        transfer.Add(DataTransferItem.Create(
            itemFormat,
            new DragItem(pressedIndex, pressedItem)));
        await DragDrop.DoDragDropAsync(
            pressEventArgs!,
            transfer,
            DragDropEffects.Move);
        pressedItem = null;
        pressedIndex = -1;
        dragStarted = false;
    }

    private void OnDragOver(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.DragEffects = eventArgs.DataTransfer.TryGetValue(itemFormat) is not null
            ? DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void OnDrop(object? sender, DragEventArgs eventArgs)
    {
        if (eventArgs.DataTransfer.TryGetValue(itemFormat) is not DragItem dragItem || ItemsSource is not IList items || items.IsReadOnly || items.IsFixedSize)
        {
            eventArgs.DragEffects = DragDropEffects.None;
            return;
        }

        int sourceIndex = dragItem.Index;
        if (sourceIndex < 0 || sourceIndex >= items.Count || !ReferenceEquals(items[sourceIndex], dragItem.Item))
        {
            eventArgs.DragEffects = DragDropEffects.None;
            return;
        }

        int targetIndex = FindTargetIndex(eventArgs.GetPosition(this));
        items.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex) targetIndex--;

        targetIndex = Math.Clamp(targetIndex, 0, items.Count);
        items.Insert(targetIndex, dragItem.Item);
        SelectedIndex = targetIndex;
        eventArgs.DragEffects = DragDropEffects.Move;
        eventArgs.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if ((eventArgs.KeyModifiers & KeyModifiers.Control) == 0
            || eventArgs.Key is not (Key.Up or Key.Down)
            || ItemsSource is not IList items
            || items.IsReadOnly
            || items.IsFixedSize
            || SelectedIndex < 0
            || SelectedIndex >= items.Count)
            return;

        int targetIndex = eventArgs.Key == Key.Up ? SelectedIndex - 1 : SelectedIndex + 1;
        if (targetIndex < 0 || targetIndex >= items.Count) return;

        object? selectedItem = items[SelectedIndex];
        items.RemoveAt(SelectedIndex);
        items.Insert(targetIndex, selectedItem);
        SelectedIndex = targetIndex;
        eventArgs.Handled = true;
    }

    private int FindTargetIndex(Point point)
    {
        for (int index = 0; index < ItemCount; index++)
        {
            if (ContainerFromIndex(index) is not Control container) continue;

            if (point.Y < container.Bounds.Center.Y) return index;
        }

        return ItemCount;
    }

    private ListBoxItem? FindItemContainer(Visual? source)
    {
        for (var current = source; current is not null && current != this; current = current.GetVisualParent())
            if (current is ListBoxItem item)
                return item;

        return null;
    }

    private sealed record DragItem(int Index, object Item);
}
