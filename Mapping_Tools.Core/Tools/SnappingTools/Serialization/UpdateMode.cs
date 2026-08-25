namespace Mapping_Tools.Core.Tools.SnappingTools.Serialization;

/// <summary>Selects the event that refreshes the generated-object graph.</summary>
public enum UpdateMode
{
    /// <summary>Refresh after any relevant editor change.</summary>
    AnyChange,

    /// <summary>Refresh when the editor time changes.</summary>
    TimeChange,

    /// <summary>Refresh when the activation key is pressed.</summary>
    HotkeyDown,

    /// <summary>Refresh when osu! becomes the active window.</summary>
    OsuActivated,
}

