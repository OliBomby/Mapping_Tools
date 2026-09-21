namespace Mapping_Tools.Application.Settings.Models;

/// <summary>Chooses how Mapping Tools reloads osu!'s editor after a save.</summary>
public enum EditorReloadMode
{
    /// <summary>Do not automatically reload the editor.</summary>
    Disabled,

    /// <summary>Reload the editor with the existing simulated keypress adapter.</summary>
    SimulatedKeypress,

    /// <summary>Reload the editor through MTIPC.</summary>
    Mtipc,
}
