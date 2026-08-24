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

