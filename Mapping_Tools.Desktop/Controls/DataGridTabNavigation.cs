using System.Collections;
using Avalonia.Controls;
using Avalonia.Input;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Moves Tab navigation between the editable cells in a data grid.</summary>
internal static class DataGridTabNavigation
{
    internal static void HandleKeyDown(DataGrid grid, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Tab || grid.CurrentColumn is not { } currentColumn) return;

        DataGridColumn[] editableColumns = grid.Columns
            .Where(column => !grid.IsReadOnly && !column.IsReadOnly)
            .OrderBy(column => column.DisplayIndex)
            .ToArray();
        int currentColumnIndex = Array.IndexOf(editableColumns, currentColumn);
        if (currentColumnIndex < 0) return;

        if (grid.CollectionView is not IEnumerable itemSource) return;

        List<object> items = itemSource.Cast<object?>().OfType<object>().ToList();
        if (grid.SelectedItem is not { } currentItem) return;

        int currentItemIndex = items.IndexOf(currentItem);
        if (currentItemIndex < 0) return;

        int direction = eventArgs.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
        int nextColumnIndex = currentColumnIndex + direction;
        int nextItemIndex = currentItemIndex;

        if (nextColumnIndex < 0)
        {
            nextItemIndex--;
            nextColumnIndex = editableColumns.Length - 1;
        }
        else if (nextColumnIndex >= editableColumns.Length)
        {
            nextItemIndex++;
            nextColumnIndex = 0;
        }

        if (nextItemIndex < 0 || nextItemIndex >= items.Count) return;

        eventArgs.Handled = true;

        grid.SelectedItem = items[nextItemIndex];
        grid.CurrentColumn = editableColumns[nextColumnIndex];
        grid.BeginEdit();
    }
}
