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

internal static class GraphKeyModifiersExtensions
{
    public static bool HasAllFlags(this KeyModifiers value, KeyModifiers flags)
    {
        return (value & flags) == flags;
    }
}
