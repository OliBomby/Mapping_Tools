using System.Windows.Input;
using Material.Icons;

namespace Mapping_Tools.Desktop.Models;

/// <summary>Describes one command rendered in the shell's active project menu.</summary>
public sealed class ShellProjectMenuItem
{
    /// <summary>Creates a project-menu item.</summary>
    /// <param name="header">The menu label, including an optional access-key underscore.</param>
    /// <param name="toolTip">The tooltip shown for the command.</param>
    /// <param name="command">The command executed by the item.</param>
    /// <param name="icon">The Material icon rendered beside the label.</param>
    public ShellProjectMenuItem(string header, string toolTip, ICommand command, MaterialIconKind icon)
    {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        ToolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Icon = icon;
    }

    /// <summary>Gets the menu label.</summary>
    public string Header { get; }

    /// <summary>Gets the menu tooltip.</summary>
    public string ToolTip { get; }

    /// <summary>Gets the command invoked by the item.</summary>
    public ICommand Command { get; }

    /// <summary>Gets the icon rendered beside the item.</summary>
    public MaterialIconKind Icon { get; }
}

