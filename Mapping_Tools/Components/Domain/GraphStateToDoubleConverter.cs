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

namespace Mapping_Tools.Components.Domain {
    public class GraphStateToDoubleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not GraphState state) {
                return 0;
            }

            var first = state.Anchors.FirstOrDefault();
            if (first is null) {
                return 0;
            }

            // Check if its constant
            if (state.Anchors.TrueForAll(o => Precision.AlmostEquals(o.Pos.Y, first.Pos.Y))) {
                return first.Pos.Y;
            }

            // Get average value
            var average = state.GetIntegral(state.MinX, state.MaxX) / (state.MaxX - state.MinX);

            return average;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is not double doubleValue) {
                return null;
            }

            return new GraphState {
                MinX = 0,
                MinY = Math.Min(0, doubleValue * 2),
                MaxX = 1,
                MaxY = Math.Max(0, doubleValue * 2),
                Anchors = new List<AnchorState>() {
                    new() { Pos = new Vector2(0, doubleValue), Interpolator = new SingleCurveInterpolator() },
                    new() { Pos = new Vector2(1, doubleValue), Interpolator = new SingleCurveInterpolator() }
                }
            };
        }
    }
}