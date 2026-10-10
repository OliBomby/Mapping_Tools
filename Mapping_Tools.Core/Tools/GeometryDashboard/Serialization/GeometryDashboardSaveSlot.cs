using Mapping_Tools.Core.Settings.Models;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class GeometryDashboardSaveSlot : ICloneable
{
    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public HotkeySettings ProjectHotkey { get; set; } = new(0, 0);

    /// <summary>Gets or sets the preference snapshot stored by this slot.</summary>
    public GeometryDashboardPreferences Preferences { get; set; } = new();

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
