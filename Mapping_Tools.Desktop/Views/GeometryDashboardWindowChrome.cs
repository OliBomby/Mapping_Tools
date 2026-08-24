using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Desktop.Interactions;

namespace Mapping_Tools.Desktop.Views;

internal static class GeometryDashboardWindowChrome
{
    public static void Resize(Window window, object? sender, PointerPressedEventArgs eventArgs)
    {
        if (sender is Control { Tag: string edge } && Enum.TryParse(edge, out WindowEdge windowEdge) && eventArgs.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            window.BeginResizeDrag(windowEdge, eventArgs);
    }
}
