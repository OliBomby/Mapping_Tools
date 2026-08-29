namespace Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class GeometryDashboardSaveSlot : ICloneable
{
    private string name = string.Empty;
    private GeometryDashboardPreferences preferences = new();
    private Hotkey projectHotkey = new();

    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get => name; set => name = value ?? string.Empty; }

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public Hotkey ProjectHotkey { get => projectHotkey; set => projectHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the preference snapshot stored by this slot.</summary>
    public GeometryDashboardPreferences Preferences
    {
        get => preferences;
        set => preferences = value ?? new GeometryDashboardPreferences();
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new GeometryDashboardSaveSlot
        {
            Name = Name,
            ProjectHotkey = (Hotkey)ProjectHotkey.Clone(),
            Preferences = (GeometryDashboardPreferences)Preferences.Clone(),
        };
    }
}

