namespace Mapping_Tools.Desktop.Controls.Graph;

/// <summary>Identifies the active pointer gesture in a graph editor.</summary>
public enum GraphPointerGesture
{
    /// <summary>No graph gesture is active.</summary>
    None,

    /// <summary>An anchor is being moved.</summary>
    Anchor,

    /// <summary>An interpolation tension handle is being moved.</summary>
    Tension,

    /// <summary>The graph viewport is being panned.</summary>
    Pan,
}

