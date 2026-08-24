using CoreGraphState = Mapping_Tools.Core.Graph.GraphState;

namespace Mapping_Tools.Desktop.Controls.Graph;

/// <summary>Provides the edited state after a graph gesture or menu operation.</summary>
public sealed class GraphStateChangedEventArgs : EventArgs
{
    /// <summary>Creates graph change information.</summary>
    /// <param name="state">The cloned state after the edit.</param>
    public GraphStateChangedEventArgs(CoreGraphState state)
    {
        State = state;
    }

    /// <summary>Gets the cloned state after the edit.</summary>
    public CoreGraphState State { get; }
}

