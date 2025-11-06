using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Components.Domain;

public class GraphStateToStringConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not GraphState state) {
            return string.Empty;
        }

        var first = state.Anchors.FirstOrDefault();
        if (first is null) {
            return string.Empty;
        }

        // Check if its constant
        if (state.Anchors.TrueForAll(o => Precision.AlmostEquals(o.Pos.Y, first.Pos.Y))) {
            return first.Pos.Y.ToInvariant();
        }

        // Convert anchors to string
        var builder = new StringBuilder();
        builder.AppendJoin('|', state.Anchors.Select(
            o => {
                var interpolator = o.Interpolator ?? new SingleCurveInterpolator();
                return $"{o.Pos.X.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                       $"{o.Pos.Y.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                       $"{InterpolatorHelper.GetInterpolatorIndex(interpolator.GetType()).ToInvariant()}:" +
                       $"{interpolator.P.ToString("0.###", CultureInfo.InvariantCulture)}";
            }));

        return builder.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if (value is not string str) {
            return null;
        }

        if (TypeConverters.TryParseDouble(str, out double doubleValue)) {
            var state2 = new GraphState {
                MinX = 0,
                MinY = Math.Min(0, doubleValue * 2),
                MaxX = 1,
                MaxY = Math.Max(1, doubleValue * 2),
                Anchors = new List<AnchorState>() {
                    new() { Pos = new Vector2(0, doubleValue), Interpolator = new SingleCurveInterpolator() },
                    new() { Pos = new Vector2(1, doubleValue), Interpolator = new SingleCurveInterpolator() }
                }
            };
            state2.Freeze();
            return state2;
        }

        // Parse all anchors
        string[] split = str.Split('|');
        var anchors = new List<AnchorState>(split.Length);
        var min = new Vector2(double.PositiveInfinity);
        var max = new Vector2(double.NegativeInfinity);
        foreach (string anchorString in split) {
            string[] values = anchorString.Split(':');
            if (values.Length < 3) continue;
            if (!TypeConverters.TryParseDouble(values[0], out var x)) continue;
            if (!TypeConverters.TryParseDouble(values[1], out var y)) continue;
            if (!TypeConverters.TryParseInt(values[2], out var i)) continue;
            if (!TypeConverters.TryParseDouble(values[3], out var p)) continue;
            var pos = new Vector2(x, y);
            min = Vector2.ComponentMin(pos, min);
            max = Vector2.ComponentMax(pos, max);
            var interpolator = InterpolatorHelper.GetInterpolator(InterpolatorHelper.GetInterpolatorByIndex(i));
            interpolator.P = p;
            anchors.Add(new AnchorState { Pos = pos, Interpolator = interpolator });
        }

        if (anchors.Count < 2) {
            // Default to something
            var state2 = new GraphState {
                MinX = 0,
                MinY = 0,
                MaxX = 1,
                MaxY = 1,
                Anchors = new List<AnchorState>() {
                    new() { Pos = new Vector2(0, 0), Interpolator = new SingleCurveInterpolator() },
                    new() { Pos = new Vector2(0, 0), Interpolator = new SingleCurveInterpolator() }
                }
            };
            state2.Freeze();
            return state2;
        }

        var size = Vector2.ComponentMax(Vector2.One, max - min);
        var state = new GraphState {
            MinX = min.X,
            MinY = min.Y,
            MaxX = min.X + size.X,
            MaxY = min.Y + size.Y,
            Anchors = anchors
        };
        state.Freeze();
        return state;
    }
}