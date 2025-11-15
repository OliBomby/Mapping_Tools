using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Mapping_Tools.Desktop.Helpers;

public abstract class NavigationItem {
    public virtual bool IsSelectable => true;
    public virtual double ContainerHeight => 32;
    public string? ToolTip { get; set; }
    public ICommand? ClickCommand { get; set; }
    public ContextMenu? ContextMenu { get; set; }
}

public sealed class NormalItem  : NavigationItem
{
    public required string Text { get; set; }
    public required Thickness Margin { get; set; }
}
public sealed class SeparatorItem  : NavigationItem
{
    public override bool IsSelectable => false;
    public override double ContainerHeight => 4;
}