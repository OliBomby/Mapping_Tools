using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Markers {
    public class DoubleMarkerGenerator : IMarkerGenerator {
        [NotNull]
        public string Unit { get; set; }

        public double Offset { get; set; }
        public double Step { get; set; }

        public bool Snappable { get; set; }

        public DoubleMarkerGenerator(double offset, double step) : this(offset, step, "") {}

        public DoubleMarkerGenerator(double offset, double step, string unit, bool snappable = false) {
            Offset = offset;
            Step = step;
            Unit = unit;
            Snappable = snappable;
        }

        public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation, int maxMarkers) {
            var markers = new List<GraphMarker>();

            double step = Step;
            while ((end - start) / step > maxMarkers) {
                step *= 2;
            }

            var vStart = Math.Ceiling((start - Offset) / step) * step + Offset;
            var v = vStart;
            int i = 0;
            while (v <= end + Precision.DOUBLE_EPSILON) {
                markers.Add(new GraphMarker {Orientation = orientation, Text = $"{v:g2}{Unit}", Value = v, Snappable = Snappable});
                v = vStart + step * ++i;
            }

            return markers;
        }
    }
}