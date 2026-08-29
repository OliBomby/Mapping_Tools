using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class GeometryDashboardSaveSlot : ICloneable
{
    private string name = string.Empty;
    private GeometryDashboardPreferences preferences = new();
    private HotkeySettings projectHotkey = new(0, 0);

    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get => name; set => name = value ?? string.Empty; }

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public HotkeySettings ProjectHotkey { get => projectHotkey; set => projectHotkey = value ?? new HotkeySettings(0, 0); }

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
            ProjectHotkey = ProjectHotkey with { },
            Preferences = (GeometryDashboardPreferences)Preferences.Clone(),
        };
    }
}
