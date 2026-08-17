namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Selects how a combo-colour point is applied to subsequent combos.
/// </summary>
public enum ColourPointMode
{
    /// <summary>Uses the sequence until another point replaces it.</summary>
    Normal,

    /// <summary>Uses the sequence for one eligible combo only.</summary>
    Burst
}
