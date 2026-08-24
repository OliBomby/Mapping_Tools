using Avalonia.Controls;
using Avalonia.Input;

namespace Mapping_Tools.Desktop.Views.GeometryDashboard;

internal static class GeometryDashboardWindowChrome
{
    public static void Resize(Window window, object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is Control { Tag: string edge } && Enum.TryParse(edge, out WindowEdge windowEdge) && eventArgs.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            window.BeginResizeDrag(windowEdge, eventArgs);
    }
}
