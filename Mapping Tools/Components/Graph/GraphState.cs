using Mapping_Tools.Classes.MathUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

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

        public double GetValue(double x) {
            return GetValue(x, Anchors);
        }

        public double GetIntegral(double t1, double t2) {
            return GetIntegral(t1, t2, Anchors);
        }

        #region GraphValueGettingStuff

        public static double GetValue(double x, IReadOnlyList<Anchor> anchors) {
            // Find the section
            var previousAnchor = anchors[0];
            var nextAnchor = anchors[1];
            foreach (var anchor in anchors) {
                if (anchor.Pos.X < x) {
                    previousAnchor = anchor;
                } else {
                    nextAnchor = anchor;
                    break;
                }
            }

            // Calculate the value via interpolation
            var difference = nextAnchor.Pos - previousAnchor.Pos;
            if (Math.Abs(difference.X) < Precision.DOUBLE_EPSILON) {
                return previousAnchor.Pos.Y;
            }

            var interpolator = nextAnchor.Interpolator;

            // Update the interpolator with the tension of the anchor. This doesn't happen automatically
            interpolator.P = nextAnchor.Tension;

            return previousAnchor.Pos.Y + difference.Y * interpolator.GetInterpolation((x - previousAnchor.Pos.X) / difference.X);
        }

        public static double GetDerivative(double x, IReadOnlyList<Anchor> anchors) {
            // Find the section
            var previousAnchor = anchors[0];
            var nextAnchor = anchors[1];
            foreach (var anchor in anchors) {
                if (anchor.Pos.X < x) {
                    previousAnchor = anchor;
                } else {
                    nextAnchor = anchor;
                    break;
                }
            }

            // Calculate the value via interpolation
            var difference = nextAnchor.Pos - previousAnchor.Pos;
            if (Math.Abs(difference.X) < Precision.DOUBLE_EPSILON) {
                return difference.Y > 0 ? double.PositiveInfinity : double.NegativeInfinity;
            }
            
            // Update the interpolator with the tension of the anchor. This doesn't happen automatically
            nextAnchor.Interpolator.P = nextAnchor.Tension;

            double derivative;
            if (nextAnchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                derivative = derivableInterpolator.GetDerivative((x - previousAnchor.Pos.X) / difference.X);
            } else {
                derivative = 1;
            }

            return derivative * difference.Y / difference.X;
        }

        public static double GetIntegral(double t1, double t2, IReadOnlyList<Anchor> anchors) {
            double height = 0;
            Anchor previousAnchor = null;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var p1C = new Vector2(MathHelper.Clamp(p1.X, t1, t2), p1.Y);
                    var p2C = new Vector2(MathHelper.Clamp(p2.X, t1, t2), p2.Y);

                    if (p2.X < t1 || p1.X > t2) {
                        previousAnchor = anchor;
                        continue;
                    }

                    var difference = p2 - p1;
                    var differenceC = p2C - p1C;

                    if (differenceC.X < Precision.DOUBLE_EPSILON) {
                        previousAnchor = anchor;
                        continue;
                    }

                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double integral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        integral = integrableInterpolator.GetIntegral((p1C.X - p1.X) / difference.X, 
                                                                      (p2C.X - p1.X) / difference.X);
                    } else {
                        integral = 0.5 * Math.Pow((p2C.X - p1.X) / difference.X, 2) - 0.5 * Math.Pow((p1C.X - p1.X) / difference.X, 2);
                    }
                    
                    height += integral * difference.X * difference.Y + differenceC.X * p1.Y;
                }

                previousAnchor = anchor;
            }

            return height;
        }

        public static double GetMaxValue(IReadOnlyList<Anchor> anchors) {
            // It suffices to find the highest Y value of the anchors, because the interpolated parts never stick out above or below the anchors.
            return anchors.Max(o => o.Pos.Y);
        }

        public static double GetMaxDerivative(IReadOnlyList<Anchor> anchors) {
            Anchor previousAnchor = null;
            double maxValue = double.NegativeInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;
                    
                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double startSlope;
                    double endSlope;

                    if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                        startSlope = derivableInterpolator.GetDerivative(0) * difference.Y / difference.X;
                        endSlope = derivableInterpolator.GetDerivative(1) * difference.Y / difference.X;
                    } else {
                        startSlope = difference.Y / difference.X;
                        endSlope = startSlope;
                    }

                    if (startSlope > maxValue) {
                        maxValue = startSlope;
                    }
                    if (endSlope > maxValue) {
                        maxValue = endSlope;
                    }
                }

                previousAnchor = anchor;
            }

            return maxValue;
        }

        public static double GetMaxIntegral(IReadOnlyList<Anchor> anchors) {
            double height = 0;
            double maxValue = height;
            Anchor previousAnchor = null;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;

                    if (difference.X < Precision.DOUBLE_EPSILON) {
                        previousAnchor = anchor;
                        continue;
                    }

                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double integral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        integral = integrableInterpolator.GetIntegral(0, 1);
                    } else {
                        integral = 0.5;
                    }
                    
                    height += integral * difference.X * difference.Y + difference.X * p1.Y;

                    if (height > maxValue) {
                        maxValue = height;
                    }
                }

                previousAnchor = anchor;
            }

            return maxValue;
        }

        public static double GetMinValue(IReadOnlyList<Anchor> anchors) {
            // It suffices to find the smallest Y value of the anchors, because the interpolated parts never stick out above or below the anchors.
            return anchors.Min(o => o.Pos.Y);
        }

        public static double GetMinDerivative(IReadOnlyList<Anchor> anchors) {
            Anchor previousAnchor = null;
            double minValue = double.PositiveInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;
                    
                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double startSlope;
                    double endSlope;

                    if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                        startSlope = derivableInterpolator.GetDerivative(0) * difference.Y / difference.X;
                        endSlope = derivableInterpolator.GetDerivative(1) * difference.Y / difference.X;
                    } else {
                        startSlope = difference.Y / difference.X;
                        endSlope = startSlope;
                    }

                    if (startSlope < minValue) {
                        minValue = startSlope;
                    }
                    if (endSlope < minValue) {
                        minValue = endSlope;
                    }
                }

                previousAnchor = anchor;
            }

            return minValue;
        }

        public static double GetMinIntegral(IReadOnlyList<Anchor> anchors) {
            double height = 0;
            double minValue = double.PositiveInfinity;
            Anchor previousAnchor = null;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;

                    if (difference.X < Precision.DOUBLE_EPSILON) {
                        previousAnchor = anchor;
                        continue;
                    }

                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double integral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        integral = integrableInterpolator.GetIntegral(0, 1);
                    } else {
                        integral = 0.5;
                    }
                    
                    height += integral * difference.X * difference.Y + difference.X * p1.Y;

                    if (height < minValue) {
                        minValue = height;
                    }
                }

                previousAnchor = anchor;
            }

            return minValue;
        }

        public static List<Anchor> DifferentiateGraph(double newMinY, double newMaxY, IReadOnlyList<Anchor> anchors, Graph parent = null) {
            var newAnchors = new List<Anchor>();
            Anchor previousAnchor = null;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;
                    
                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double startSlope;
                    double endSlope;
                    IGraphInterpolator derivativeInterpolator;

                    if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                        startSlope = derivableInterpolator.GetDerivative(0) * difference.Y / difference.X;
                        endSlope = derivableInterpolator.GetDerivative(1) * difference.Y / difference.X;
                        derivativeInterpolator = derivableInterpolator.GetDerivativeInterpolator();

                    } else {
                        startSlope = difference.Y / difference.X;
                        endSlope = startSlope;
                        derivativeInterpolator = new LinearInterpolator();
                    }

                    var np1 = new Vector2(previousAnchor.Pos.X, startSlope);
                    var np2 = new Vector2(anchor.Pos.X, endSlope);

                    if (!(newAnchors.Count > 0 && Vector2.DistanceSquared(newAnchors[newAnchors.Count - 1].Pos, np1) < Precision.DOUBLE_EPSILON)) {
                        newAnchors.Add(new Anchor(parent, np1, new LinearInterpolator()));
                    }
                    newAnchors.Add(new Anchor(parent, np2, derivativeInterpolator));
                }

                previousAnchor = anchor;
            }

            return newAnchors;
        }

        public static List<Anchor> IntegrateGraph(double newMinY, double newMaxY, IReadOnlyList<Anchor> anchors, Graph parent = null, double startHeight = 0) {
            var newAnchors = new List<Anchor> {new Anchor(parent, new Vector2(0, -newMinY / (newMaxY - newMinY)))};
            double height = startHeight;
            Anchor previousAnchor = null;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;

                    if (difference.X < Precision.DOUBLE_EPSILON) {
                        previousAnchor = anchor;
                        continue;
                    }

                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    double integral;
                    IGraphInterpolator primitiveInterpolator;

                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        integral = integrableInterpolator.GetIntegral(0, 1);
                        primitiveInterpolator = integrableInterpolator.GetPrimitiveInterpolator(p1.X, p1.Y, p2.X, p2.Y);
                    } else {
                        integral = 0.5;
                        primitiveInterpolator = new LinearInterpolator();
                    }
                    
                    height += integral * difference.X * difference.Y + difference.X * p1.Y;
                    newAnchors.Add(new Anchor(parent, new Vector2(anchor.Pos.X, height), primitiveInterpolator));
                }

                previousAnchor = anchor;
            }

            return newAnchors;
        }

        #endregion
    }
}