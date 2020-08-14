using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Markers {
    public class DividedBeatMarkerGenerator : IMarkerGenerator {
        public int BeatDivisor { get; set; }

        public bool Snappable { get; set; }

        public DividedBeatMarkerGenerator() : this(4) {}

        public DividedBeatMarkerGenerator(int beatDivisor, bool snappable = false) {
            BeatDivisor = beatDivisor;
            Snappable = snappable;
        }

        public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation, int maxMarkers) {
            var markers = new List<GraphMarker>();

            double step = 1d / BeatDivisor;
            while ((end - start) / step > maxMarkers) {
                step *= 2;
            }

            var v = start;
            int i = 0;
            while (v <= end + Precision.DOUBLE_EPSILON) {
                Color markerColor;
                double markerLength;

                if (Math.Abs(v % 4) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.White;
                    markerLength = 20;
                } else if (Math.Abs(v % 1) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.White;
                    markerLength = 12;
                } else if (Math.Abs(v % (1d / 2)) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.Red;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 4)) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.DodgerBlue;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 8)) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.Yellow;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 6)) < Precision.DOUBLE_EPSILON) {
                    markerColor = Colors.Purple;
                    markerLength = 7;
                } else {
                    markerColor = Colors.Gray;
                    markerLength = 7;
                }

                markers.Add(new GraphMarker {Orientation = orientation, Value = v, DrawMarker = true,
                    MarkerColor = markerColor, MarkerLength = markerLength, Text = null, Snappable = Snappable
                });

                v = start + step * ++i;
            }

            return markers;
        }
    }
}