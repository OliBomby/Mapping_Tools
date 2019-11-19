﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph {
        private bool _drawAnchors;

        public List<TensionAnchor> TensionAnchors { get; }
        public List<Anchor> Anchors { get; }
        public List<GraphMarker> Markers { get; set; }


        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }

        private Brush _stroke;
        public Brush Stroke {
            get => _stroke;
            set { 
                _stroke = value;
                UpdateVisual();
            }
        }

        private Brush _fill;
        public Brush Fill {
            get => _fill;
            set {
                _fill = value;
                UpdateVisual();
            }
        }

        private Brush _edgesBrush;
        public Brush EdgesBrush {
            get => _edgesBrush;
            set {
                _edgesBrush = value;
                Anchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _anchorStroke;
        public Brush AnchorStroke { get => _anchorStroke;
            set {
                _anchorStroke = value;
                Anchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _anchorFill;
        public Brush AnchorFill { get => _anchorFill;
            set {
                _anchorFill = value;
                Anchors.ForEach(o => o.Fill = value);
            }
        }

        private Brush _tensionAnchorStroke;
        public Brush TensionAnchorStroke { get => _tensionAnchorStroke;
            set {
                _tensionAnchorStroke = value;
                TensionAnchors.ForEach(o => o.Stroke = value);
            }
        }

        private Brush _tensionAnchorFill;
        public Brush TensionAnchorFill { get => _tensionAnchorFill;
            set {
                _tensionAnchorFill = value;
                TensionAnchors.ForEach(o => o.Fill = value);
            }
        }


        public Graph() {
            InitializeComponent();
            TensionAnchors = new List<TensionAnchor>();
            Anchors = new List<Anchor>();
            Markers = new List<GraphMarker>();

            Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0));
            EdgesBrush = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

            XMin = 0;
            YMin = 0;
            XMax = 1;
            YMax = 1;

            // Initialize Anchors
            Anchors.Add(MakeAnchor(new Vector2(0, 0.5)));
            AddAnchor(new Vector2(1, 0.5));
        }

        public void SetBrush(Brush brush) {
            var transparentBrush = brush.Clone();
            transparentBrush.Opacity = 0.2;

            Stroke = brush;
            Fill = transparentBrush;
            AnchorStroke = brush;
            AnchorFill = transparentBrush;
            TensionAnchorStroke = brush;
            TensionAnchorFill = transparentBrush;
        }

        public Anchor AddAnchor(Vector2 pos) {
            // Clamp the position withing bounds
            pos = Vector2.Clamp(pos, Vector2.Zero, Vector2.One);

            // Find the correct index
            var index = Anchors.FindIndex(o => o.Pos.X > pos.X);
            index = index == -1 ? Math.Max(Anchors.Count - 1, 1) : index;

            // Make anchor
            var anchor = MakeAnchor(pos);

            // Make tension anchor
            var tensionAnchor = MakeTensionAnchor(pos, anchor);

            // Link Anchors
            anchor.TensionAnchor = tensionAnchor;
            
            // Insert anchor
            Anchors.Insert(index, anchor);

            // Add tension anchor
            TensionAnchors.Add(tensionAnchor);

            UpdateVisual();

            return anchor;
        }

        public void RemoveAnchor(Anchor anchor) {
            // Dont remove the anchors on the left and right edge
            if (IsEdgeAnchor(anchor)) return;

            Anchors.Remove(anchor);
            TensionAnchors.Remove(anchor.TensionAnchor);
            UpdateVisual();
        }

        public bool IsEdgeAnchor(Anchor anchor) {
            return anchor == Anchors[0] || anchor == Anchors[Anchors.Count - 1];
        }

        private Anchor MakeAnchor(Vector2 pos) {
            var anchor = new Anchor(this, pos) {
                Stroke = AnchorStroke,
                Fill = AnchorFill
            };
            return anchor;
        }

        private TensionAnchor MakeTensionAnchor(Vector2 pos, Anchor parentAnchor) {
            var anchor = new TensionAnchor(this, pos, parentAnchor) {
                Stroke = TensionAnchorStroke,
                Fill = TensionAnchorFill
            };
            return anchor;
        }

        private void ThisMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            var newAnchor = AddAnchor(GetPosition(e.GetPosition(this)));
            newAnchor.EnableDragging();
        }

        /// <summary>
        /// Calculates the height of the curve for a given X value.
        /// </summary>
        /// <param name="x">The progression along the curve (0-1)</param>
        /// <returns>The height of the curve (0-1)</returns>
        public double GetValue(double x) {
            // Find the section
            var previousAnchor = Anchors[0];
            var nextAnchor = Anchors[1];
            foreach (var anchor in Anchors) {
                if (anchor.Pos.X < x) {
                    previousAnchor = anchor;
                } else {
                    nextAnchor = anchor;
                    break;
                }
            }

            // Calculate the value via interpolation
            var diff = nextAnchor.Pos - previousAnchor.Pos;
            if (Math.Abs(diff.X) < Precision.DOUBLE_EPSILON) {
                return previousAnchor.Pos.Y;
            }
            var sectionProgress = (x - previousAnchor.Pos.X) / diff.X;
            return diff.Y * sectionProgress + previousAnchor.Pos.Y;
        }

        private Point GetRelativePoint(Vector2 pos) {
            return new Point(pos.X * Width, Height - pos.Y * Height);
        }

        private Point GetRelativePoint(double x) {
            return GetRelativePoint(new Vector2(x, GetValue(x)));
        }

        private Vector2 GetPosition(Point pos) {
            return new Vector2(pos.X / Width, (Height - pos.Y) / Height);
        }

        private Vector2 GetPosition(GraphMarker marker) {
            return GetPosition(new Point(marker.X, marker.Y));
        }

        public void UpdateVisual() {
            // Clear canvas
            MainCanvas.Children.Clear();

            // Add markers
            foreach (var marker in Markers.Where(marker => !(marker.X < 0) && !(marker.X > Width) && !(marker.Y < 0) && !(marker.Y > Height))) {
                MainCanvas.Children.Add(marker);
            }

            // Add border
            var rect = new Rectangle {
                Stroke = EdgesBrush, Width = Width, Height = Height, StrokeThickness = 2
            };
            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);
            MainCanvas.Children.Add(rect);

            // Check if there are at least 2 anchors
            if (Anchors.Count < 2) return;

            // Calculate interpolation line
            var points = new PointCollection();
            for (int i = 1; i < Anchors.Count; i++) {
                var previous = Anchors[i - 1];
                var next = Anchors[i];

                points.Add(GetRelativePoint(previous.Pos));

                for (int k = 1; k < Width * (next.Pos.X - previous.Pos.X); k++) {
                    var x = previous.Pos.X + k / Width;

                    points.Add(GetRelativePoint(x));
                }
            }
            points.Add(GetRelativePoint(Anchors[Anchors.Count - 1].Pos));

            // Draw line
            var line = new Polyline {Points = points, Stroke = Stroke, StrokeThickness = 2,
                StrokeEndLineCap = PenLineCap.Round, StrokeStartLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round};
            MainCanvas.Children.Add(line);

            // Draw area under line
            var points2 = new PointCollection(points) {
                GetRelativePoint(new Vector2(1, 0)), GetRelativePoint(new Vector2(0, 0))
            };

            var polygon = new Polygon {Points = points2, Fill = Fill};
            MainCanvas.Children.Add(polygon);

            // Return if we dont draw Anchors
            if (!_drawAnchors) return;

            // Add tension Anchors
            foreach (var tensionAnchor in TensionAnchors) {
                // Find x position in the middle
                var next = tensionAnchor.ParentAnchor;
                var previous = Anchors[Anchors.IndexOf(next) - 1];

                if (Math.Abs(next.Pos.X - previous.Pos.X) < Precision.DOUBLE_EPSILON) {
                    continue;
                }
                var x = (next.Pos.X + previous.Pos.X) / 2;

                // Get y on the graph and set position
                var y = GetValue(x);
                tensionAnchor.Pos = new Vector2(x, y);

                RenderGraphPoint(tensionAnchor);
            }

            // Add Anchors
            foreach (var anchor in Anchors) {
                RenderGraphPoint(anchor);
            }
        }

        private void RenderGraphPoint(GraphPointControl point) {
            MainCanvas.Children.Add(point);
            var p = GetRelativePoint(point.Pos);
            Canvas.SetLeft(point, p.X - point.Width / 2);
            Canvas.SetTop(point, p.Y - point.Height / 2);
        }

        public void MoveAnchorTo(Anchor anchor, Vector2 pos) {
            var index = Anchors.IndexOf(anchor);
            var previous = Anchors.ElementAtOrDefault(index - 1);
            var next = Anchors.ElementAtOrDefault(index + 1);
            if (previous == null || next == null) {
                // Is edge anchor so dont move it
                pos.X = anchor.Pos.X;
            } else {
                // Snap to nearest vertical marker unless left alt is held
                if (!Keyboard.IsKeyDown(Key.LeftAlt)) {
                    // Find the nearest marker
                    GraphMarker nearestMarker = null;
                    double nearestDistance = double.PositiveInfinity;
                    foreach (var marker in Markers.Where(o => o.Orientation == Orientation.Vertical)) {
                        var markerPos = GetPosition(marker);
                        var dist = Math.Abs(pos.X - markerPos.X);
                        if (!(dist < nearestDistance)) continue;
                        nearestDistance = dist;
                        nearestMarker = marker;
                    }
                    // Set X to that marker's value
                    if (nearestMarker != null)
                        pos.X = GetPosition(nearestMarker).X;
                }
                pos.X = MathHelper.Clamp(pos.X, previous.Pos.X, next.Pos.X);
            }

            pos.Y = MathHelper.Clamp(pos.Y, 0, 1);

            anchor.Pos = pos;

            UpdateVisual();
        }

        private void Graph_OnLoaded(object sender, RoutedEventArgs e) {
            UpdateVisual();
        }

        private void Graph_OnMouseEnter(object sender, MouseEventArgs e) {
            if (_drawAnchors) return;
            _drawAnchors = true;
            UpdateVisual();
        }

        private void Graph_OnMouseLeave(object sender, MouseEventArgs e) {
            if (!_drawAnchors) return;
            _drawAnchors = false;
            UpdateVisual();
        }

        public void SetMarkers(List<GraphMarker> markers) {
            foreach (var graphMarker in markers) {
                graphMarker.Stroke = EdgesBrush;
                graphMarker.Width = Width;
                graphMarker.Height = Height;
                if (graphMarker.Orientation == Orientation.Horizontal) {
                    graphMarker.X = 0;
                    graphMarker.Y = Height - Height * ((graphMarker.Value - YMin) / (YMax - YMin));
                } else {
                    graphMarker.X = Width * ((graphMarker.Value - XMin) / (XMax - XMin));
                    graphMarker.Y = 0;
                }
            }

            Markers = markers;
        }
    }
}
