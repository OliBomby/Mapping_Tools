using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mapping_Tools.Desktop.Controls;

internal static class ToolValidationHelper
{
    static ToolValidationHelper()
    {
        DataValidationErrors.HasErrorsProperty.Changed.AddClassHandler<Control>(static (_, _) => ValidationChanged?.Invoke(null, EventArgs.Empty));
    }

    internal static event EventHandler? ValidationChanged;

    public static bool HasErrors(Visual source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var scope = FindScope(source);
        return scope.GetSelfAndVisualDescendants()
            .OfType<Control>()
            .Any(DataValidationErrors.GetHasErrors);
    }

    private static Visual FindScope(Visual source)
    {
        var scope = source;
        while (scope.GetVisualParent() is { } parent)
        {
            scope = parent;
            if (scope is UserControl) return scope;
        }

        return scope;
    }
}
