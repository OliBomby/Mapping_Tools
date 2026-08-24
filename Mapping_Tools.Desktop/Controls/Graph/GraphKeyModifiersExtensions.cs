using Avalonia.Input;

namespace Mapping_Tools.Desktop.Controls.Graph;

internal static class GraphKeyModifiersExtensions
{
    public static bool HasAllFlags(this KeyModifiers value, KeyModifiers flags)
    {
        return (value & flags) == flags;
    }
}
