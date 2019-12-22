using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Windows;
using Mapping_Tools.Components.Graph.Interpolation;

namespace Mapping_Tools.Components.Graph {
    public class GraphState : Freezable {
        #region DependencyProperties

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

        public double GetInterpolation(double x) {
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

        public double GetIntegral(double t1, double t2) {
            double height = 0;
            Anchor previousAnchor = null;
            foreach (var anchor in Anchors) {
                if (previousAnchor != null) {
                    var p1 = new Vector2(MathHelper.Clamp(previousAnchor.Pos.X, t1, t2), previousAnchor.Pos.Y);
                    var p2 = new Vector2(MathHelper.Clamp(anchor.Pos.X, t1, t2), anchor.Pos.Y);

                    if (p2.X < t1 || p1.X > t2) {
                        previousAnchor = anchor;
                        continue;
                    }

                    var difference = p2 - p1;

                    if (difference.X < Precision.DOUBLE_EPSILON) {
                        previousAnchor = anchor;
                        continue;
                    }

                    double integral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        integral = integrableInterpolator.GetIntegral((p1.X - previousAnchor.Pos.X) / (anchor.Pos.X - previousAnchor.Pos.X), 
                                                                      (p2.X - previousAnchor.Pos.X) / (anchor.Pos.X - previousAnchor.Pos.X));
                    } else {
                        integral = 0.5;
                    }
                    
                    // TODO: FIX THIS
                    height += integral * difference.X * difference.Y + difference.X * p1.Y;
                }

                previousAnchor = anchor;
            }

            return height;
        }
    }
}