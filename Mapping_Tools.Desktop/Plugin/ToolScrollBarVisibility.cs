namespace Mapping_Tools.Desktop.Plugin;

/// <summary>
///     Describes the shell scroll behavior requested by a Desktop tool without
///     depending on a particular UI framework.
/// </summary>
public enum ToolScrollBarVisibility
{
    /// <summary>The shell determines whether the scrollbar is needed.</summary>
    Auto,

    /// <summary>The scrollbar is not shown and content does not scroll in this direction.</summary>
    Disabled,

    /// <summary>The scrollbar is hidden while scrolling remains available.</summary>
    Hidden,

    /// <summary>The scrollbar is always shown.</summary>
    Visible,
}
