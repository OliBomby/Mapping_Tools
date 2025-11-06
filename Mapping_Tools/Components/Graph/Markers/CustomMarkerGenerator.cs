using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mapping_Tools.Components.Graph.Markers;

public class CustomMarkerGenerator : IMarkerGenerator {
    public delegate string ToStringFunction(double value);

    public double Offset { get; set; }

    public double StepSize { get; set; }

    public bool Snappable { get; set; }

    public bool Reduce { get; set; }

    public bool DrawMarker { get; set; }

    public double MarkerLength { get; set; }

    public Color MarkerColor { get; set; }

    public ToStringFunction ValueToString { get; set; }

    public CustomMarkerGenerator() { }

    public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation, int maxMarkers) {
        if (StepSize <= 0) {
            yield break;
        }

        double step = StepSize;
        if ((end - start) / step > maxMarkers) {
            if (Reduce) {
                while ((end - start) / step > maxMarkers) {
                    step *= 2;
                }
            } else {
                yield break;
            }
        }

        double vStart = Math.Ceiling((start - Offset) / step) * step + Offset;
        double v = vStart;
        int i = 0;
        while (v <= end + Precision.DoubleEpsilon) {
            string text = ValueToString != null ? ValueToString(v) : null;
            yield return new GraphMarker {
                Orientation = orientation,
                Text = text,
                Value = v,
                Snappable = Snappable,
                DrawMarker = DrawMarker,
                MarkerLength = MarkerLength,
                MarkerColor = MarkerColor
            };

            v = vStart + step * ++i;
        }
    }
}