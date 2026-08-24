using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Mapping_Tools.Application.Interactions.Converters;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.Graph.Markers;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Mapping_Tools.Desktop.Views.Dialogs;
using CoreGraphState = Mapping_Tools.Core.Graph.GraphState;

namespace Mapping_Tools.Desktop.Controls;

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

