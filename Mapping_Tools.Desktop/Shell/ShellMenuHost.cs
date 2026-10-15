using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Mapping_Tools.Desktop.Plugin;
using Material.Icons.Avalonia;

namespace Mapping_Tools.Desktop.Shell;

internal sealed class ShellMenuHost(Menu menuBar, MenuItem fileMenu, MenuItem aboutMenu)
{
    private readonly List<(ItemsControl Parent, MenuItem Item)> addedItems = [];

    internal void SetRegistrations(IEnumerable<ShellMenuRegistration> registrations)
    {
        foreach (var (parent, item) in addedItems) parent.Items.Remove(item);
        addedItems.Clear();

        foreach (var registration in registrations)
        {
            ItemsControl parent = registration.Location switch
            {
                ShellMenuLocation.MenuBar => menuBar,
                ShellMenuLocation.File => fileMenu,
                ShellMenuLocation.About => aboutMenu,
                _ => throw new ArgumentOutOfRangeException(nameof(registrations)),
            };
            var item = CreateItem(registration.Item);
            if (registration.Location == ShellMenuLocation.MenuBar) item.Height = 35;
            parent.Items.Add(item);
            addedItems.Add((parent, item));
        }
    }

    private static MenuItem CreateItem(ShellMenuItem source)
    {
        MenuItem item = new() { DataContext = source, Command = source.Command };
        item.Bind(HeaderedSelectingItemsControl.HeaderProperty, new Binding(nameof(ShellMenuItem.Header)));
        item.Bind(ToolTip.TipProperty, new Binding(nameof(ShellMenuItem.ToolTip)));
        if (source.Icon is { } icon) item.Icon = new MaterialIcon { Kind = icon };
        foreach (var child in source.Items) item.Items.Add(CreateItem(child));
        return item;
    }
}
