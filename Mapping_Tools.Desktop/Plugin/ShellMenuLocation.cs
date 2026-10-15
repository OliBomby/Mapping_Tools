namespace Mapping_Tools.Desktop.Plugin;

/// <summary>Identifies a shell menu independently of its translated label or active tool.</summary>
public enum ShellMenuLocation
{
    /// <summary>Appends an action or submenu to the menu bar.</summary>
    MenuBar,

    /// <summary>Appends an action or submenu to File.</summary>
    File,

    /// <summary>Appends an action or submenu to About.</summary>
    About,
}
