namespace Mapping_Tools.Core.Tools.SnappingTools.Serialization;

/// <summary>Selects which editor hit objects become root objects.</summary>
public enum SelectedHitObjectMode
{
    /// <summary>Use every hit object visible around the editor time.</summary>
    AllwaysAllVisible,

    /// <summary>Use selected objects when any are selected, otherwise visible objects.</summary>
    VisibleOrSelected,

    /// <summary>Use only selected objects.</summary>
    OnlySelected,
}

