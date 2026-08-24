using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Core.Tools.SnappingTools.Serialization;

/// <summary>A named saved snapshot of Geometry Dashboard preferences.</summary>
public sealed class SnappingToolsSaveSlot : ICloneable
{
    private string name = string.Empty;
    private SnappingToolsPreferences preferences = new();
    private Hotkey projectHotkey = new();

    /// <summary>Gets or sets the user-visible slot name.</summary>
    public string Name { get => name; set => name = value ?? string.Empty; }

    /// <summary>Gets or sets the slot activation hotkey.</summary>
    public Hotkey ProjectHotkey { get => projectHotkey; set => projectHotkey = value ?? new Hotkey(); }

    /// <summary>Gets or sets the preference snapshot stored by this slot.</summary>
    public SnappingToolsPreferences Preferences
    {
        get => preferences;
        set => preferences = value ?? new SnappingToolsPreferences();
    }

    /// <inheritdoc />
    public object Clone()
    {
        return new SnappingToolsSaveSlot
        {
            Name = Name,
            ProjectHotkey = (Hotkey)ProjectHotkey.Clone(),
            Preferences = (SnappingToolsPreferences)Preferences.Clone(),
        };
    }
}

