using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Components.Graph.Markers {
    public class DividedBeatMarkerGenerator : IMarkerGenerator {
        public int BeatDivisor { get; set; }

        public DividedBeatMarkerGenerator() : this(4) {}

        public DividedBeatMarkerGenerator(int beatDivisor) {
            BeatDivisor = beatDivisor;
        }

        public IEnumerable<GraphMarker> GenerateMarkers(double start, double end, Orientation orientation) {
            var markers = new List<GraphMarker>();
            var v = start;
            while (v <= end) {
                Color markerColor;
                double markerLength;

                if (Math.Abs(v % 4) < double.Epsilon) {
                    markerColor = Colors.White;
                    markerLength = 20;
                } else if (Math.Abs(v % 1) < double.Epsilon) {
                    markerColor = Colors.White;
                    markerLength = 12;
                } else if (Math.Abs(v % (1d / 2)) < double.Epsilon) {
                    markerColor = Colors.Red;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 4)) < double.Epsilon) {
                    markerColor = Colors.DodgerBlue;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 8)) < double.Epsilon) {
                    markerColor = Colors.Yellow;
                    markerLength = 7;
                } else if (Math.Abs(v % (1d / 6)) < double.Epsilon) {
                    markerColor = Colors.Purple;
                    markerLength = 7;
                } else {
                    markerColor = Colors.Gray;
                    markerLength = 7;
                }

                markers.Add(new GraphMarker {Orientation = orientation, Value = v, DrawMarker = true,
                    MarkerColor = markerColor, MarkerLength = markerLength, Text = null
                });

                v += 1d / BeatDivisor;
            }

            return markers;
        }
    }
}