using System.Windows.Input;
using Mapping_Tools.Desktop.Localization;
using Material.Icons;

namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Describes a shell action or nested submenu without exposing host controls to plugins.</summary>
public sealed class ShellMenuItem : LocalizedObservableObject
{
    private readonly string header;
    private readonly string? toolTip;
    private readonly Func<string>? translatedHeaderGetter;
    private readonly Func<string>? translatedToolTipGetter;

    /// <summary>Creates a menu action or submenu. Labels refresh when the host language changes.</summary>
    /// <param name="header">The label, including an optional access-key underscore.</param>
    /// <param name="command">The action; its CanExecute state controls availability. Omit for a submenu.</param>
    /// <param name="items">Submenu children; omit for an action.</param>
    /// <param name="icon">An optional Material icon.</param>
    /// <param name="toolTip">Optional help text.</param>
    /// <param name="translatedHeaderGetter">An optional getter for the current translated label.</param>
    /// <param name="translatedToolTipGetter">An optional getter for the current translated help text.</param>
    public ShellMenuItem(
        string header,
        ICommand? command = null,
        IEnumerable<ShellMenuItem>? items = null,
        MaterialIconKind? icon = null,
        string? toolTip = null,
        Func<string>? translatedHeaderGetter = null,
        Func<string>? translatedToolTipGetter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);
        var children = items?.ToArray() ?? [];
        if ((command is null) == (children.Length == 0))
            throw new ArgumentException("Supply either a command or non-empty submenu items.");

        this.header = header;
        this.toolTip = toolTip;
        this.translatedHeaderGetter = translatedHeaderGetter;
        this.translatedToolTipGetter = translatedToolTipGetter;
        Command = command;
        Items = Array.AsReadOnly(children);
        Icon = icon;
    }

    /// <summary>Gets the current menu label.</summary>
    public string Header => translatedHeaderGetter?.Invoke() ?? header;

    /// <summary>Gets the current help text.</summary>
    public string? ToolTip => translatedToolTipGetter?.Invoke() ?? toolTip;

    /// <summary>Gets the action, whose CanExecuteChanged event updates the menu's enabled state.</summary>
    public ICommand? Command { get; }

    /// <summary>Gets the submenu children in declaration order.</summary>
    public IReadOnlyList<ShellMenuItem> Items { get; }

    /// <summary>Gets the optional icon.</summary>
    public MaterialIconKind? Icon { get; }
}
