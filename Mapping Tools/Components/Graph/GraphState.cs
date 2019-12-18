using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Mapping_Tools.Components.Graph {
    public class GraphState : Freezable {
        #region DependencyProperties

        public static readonly DependencyProperty TensionAnchorsProperty =
            DependencyProperty.Register(nameof(TensionAnchors),
                typeof(List<TensionAnchor>), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(null));

        public List<TensionAnchor> TensionAnchors {
            get => (List<TensionAnchor>) GetValue(TensionAnchorsProperty);
            set => SetValue(TensionAnchorsProperty, value);
        }

        public static readonly DependencyProperty AnchorsProperty =
            DependencyProperty.Register(nameof(Anchors),
                typeof(List<Anchor>), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(null));

        public List<Anchor> Anchors {
            get => (List<Anchor>) GetValue(AnchorsProperty);
            set => SetValue(AnchorsProperty, value);
        }

        public static readonly DependencyProperty MinXProperty =
            DependencyProperty.Register(nameof(MinX),
                typeof(double), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(0d));

        public double MinX {
            get => (double) GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }

        public static readonly DependencyProperty MinYProperty =
            DependencyProperty.Register(nameof(MinY),
                typeof(double), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(0d));

        public double MinY {
            get => (double) GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }

        public static readonly DependencyProperty MaxXProperty =
            DependencyProperty.Register(nameof(MaxX),
                typeof(double), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(1d));

        public double MaxX {
            get => (double) GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }

        public static readonly DependencyProperty MaxYProperty =
            DependencyProperty.Register(nameof(MaxY),
                typeof(double), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(1d));

        public double MaxY {
            get => (double) GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }

        #endregion

        protected override Freezable CreateInstanceCore() {
            return new GraphState();
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
        /// Calculates the height of the curve from <see cref="MinY"/> to <see cref="MaxY"/> for a given progression allong the graph [0-1].
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public double GetValue(double x) {
            return MinY + (MaxY - MinY) * GetPosition(x);
        }
    }
}