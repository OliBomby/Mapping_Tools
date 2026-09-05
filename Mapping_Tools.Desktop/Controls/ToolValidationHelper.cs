using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Mapping_Tools.Desktop.Controls;

internal static class ToolValidationHelper
{
    internal static event EventHandler? ValidationChanged;

    static ToolValidationHelper()
    {
        DataValidationErrors.HasErrorsProperty.Changed.AddClassHandler<Control>(
            static (_, _) => ValidationChanged?.Invoke(null, EventArgs.Empty));
    }

    public static bool HasErrors(Visual source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Visual scope = FindScope(source);
        return scope.GetSelfAndVisualDescendants()
            .OfType<Control>()
            .Any(DataValidationErrors.GetHasErrors);
    }

    private static Visual FindScope(Visual source)
    {
        Visual scope = source;
        while (scope.GetVisualParent() is Visual parent)
        {
            scope = parent;
            if (scope is UserControl) return scope;
        }

        return scope;
    }
}
