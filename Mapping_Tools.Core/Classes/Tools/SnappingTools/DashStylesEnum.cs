namespace Mapping_Tools.Core.Classes.Tools.SnappingTools;

/// <summary>Identifies the line pattern used by a Geometry Dashboard shape.</summary>
public enum DashStylesEnum
{
    /// <summary>A repeating dash pattern.</summary>
    Dash = 0,

    /// <summary>A dotted pattern.</summary>
    Dot = 1,

    /// <summary>A dash followed by a dot.</summary>
    DashSingleDot = 2,

    /// <summary>A dash followed by two dots.</summary>
    DashDoubleDot = 3,

    /// <summary>A continuous line.</summary>
    Solid = 4,
}
