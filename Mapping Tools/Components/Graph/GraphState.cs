using System;
using Mapping_Tools.Classes.SystemTools;
using System.Collections.Generic;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Components.Graph {
    public class GraphState : BindableBase {
        private Graph _parentGraph;

        public Graph ParentGraph {
            get => _parentGraph;
            set => Set(ref _parentGraph, value);
        }

        public List<TensionAnchor> TensionAnchors { get; }
        public List<Anchor> Anchors { get; }

        [NotNull]
        public Type LastInterpolationSet { get; set; }

        public double XMin { get; set; }
        public double YMin { get; set; }
        public double XMax { get; set; }
        public double YMax { get; set; }

        public GraphState(Graph parentGraph) {
            ParentGraph = parentGraph;
            TensionAnchors = new List<TensionAnchor>();
            Anchors = new List<Anchor>();
            LastInterpolationSet = typeof(SingleCurveInterpolator);

            XMin = 0;
            YMin = 0;
            XMax = 1;
            YMax = 1;
        }

        public Anchor AddAnchor(Vector2 pos) {
            // Clamp the position withing bounds
            pos = Vector2.Clamp(pos, Vector2.Zero, Vector2.One);

            // Find the correct index
            var index = Anchors.FindIndex(o => o.Pos.X > pos.X);
            index = index == -1 ? Math.Max(Anchors.Count - 1, 1) : index;

            // Get the next anchor
            Anchor nextAnchor = null;
            if (index < Anchors.Count) {
                nextAnchor = Anchors[index];
            }

            // Make anchor
            var anchor = LastInterpolationSet != null ? MakeAnchor(pos, LastInterpolationSet) : MakeAnchor(pos);

            // Make tension anchor
            var tensionAnchor = MakeTensionAnchor(pos, anchor);

            // Link State.Anchors
            anchor.TensionAnchor = tensionAnchor;

            // Add tension
            anchor.Tension = nextAnchor?.Tension ?? 0;
            
            // Insert anchor
            Anchors.Insert(index, anchor);

            // Add tension anchor
            TensionAnchors.Add(tensionAnchor);
            
            UpdateAnchorNeighbors();
            ParentGraph?.UpdateVisual();

            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos) {
            var anchor = new Anchor(ParentGraph, pos) {
                Stroke = ParentGraph?.AnchorStroke,
                Fill = ParentGraph?.AnchorFill
            };
            return anchor;
        }

        public Anchor MakeAnchor(Vector2 pos, Type interpolator) {
            var anchor = new Anchor(ParentGraph, pos, interpolator) {
                Stroke = ParentGraph?.AnchorStroke,
                Fill = ParentGraph?.AnchorFill
            };
            return anchor;
        }

        public TensionAnchor MakeTensionAnchor(Vector2 pos, Anchor parentAnchor) {
            var anchor = new TensionAnchor(ParentGraph, pos, parentAnchor) {
                Stroke = ParentGraph?.AnchorStroke,
                Fill = ParentGraph?.AnchorFill
            };
            return anchor;
        }

        public void RemoveAnchor(Anchor anchor) {
            // Dont remove the anchors on the left and right edge
            if (IsEdgeAnchor(anchor)) return;

            Anchors.Remove(anchor);
            TensionAnchors.Remove(anchor.TensionAnchor);

            UpdateAnchorNeighbors();
            ParentGraph?.UpdateVisual();
        }

        private void UpdateAnchorNeighbors() {
            Anchor previousAnchor = null;
            foreach (var anchor in Anchors) {
                anchor.PreviousAnchor = previousAnchor;
                if (previousAnchor != null) {
                    previousAnchor.NextAnchor = anchor;
                }

                previousAnchor = anchor;
            }
        }

        public bool IsEdgeAnchor(Anchor anchor) {
            return anchor == Anchors[0] || anchor == Anchors[Anchors.Count - 1];
        }

        /// <summary>
        /// Calculates the height of the curve [0-1] for a given progression allong the graph [0-1].
        /// </summary>
        /// <param name="x">The progression along the curve (0-1)</param>
        /// <returns>The height of the curve (0-1)</returns>
        public double GetPosition(double x) {
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

            var interpolator = nextAnchor.Interpolator;
            interpolator.P = nextAnchor.Tension;

            return previousAnchor.Pos.Y + diff.Y * interpolator.GetInterpolation(sectionProgress);
        }

        /// <summary>
        /// Converts a value from the coordinate system to the absolute coordinate system [0-1]x[0-1].
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Vector2 GetPosition(Vector2 value) {
            return new Vector2((value.X - XMin) / (XMax - XMin), (value.Y - YMin) / (YMax - YMin));
        }

        /// <summary>
        /// Calculates the height of the curve from <see cref="YMin"/> to <see cref="YMax"/> for a given progression allong the graph [0-1].
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double GetValue(double x) {
            return YMin + (YMax - YMin) * GetPosition(x);
        }

        /// <summary>
        /// Converts a value from the absolute coordinate system to a value.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector2 GetValue(Vector2 position) {
            return new Vector2(XMin + (XMax - XMin) * position.X, YMin + (YMax - YMin) * position.Y);
        }
    }
}