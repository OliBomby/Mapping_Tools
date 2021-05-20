using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Mapping_Tools.Components.Graph {
    public sealed class AnchorCollection : ObservableCollection<Anchor> {
        public event DependencyPropertyChangedEventHandler AnchorsChanged;

        public AnchorCollection() {
            CollectionChanged += OnCollectionChanged;
        }

        public AnchorCollection(IEnumerable<Anchor> anchors) : base(anchors) {
            CollectionChanged += OnCollectionChanged;
            InitAllAnchors();
        }

        public AnchorCollection(List<Anchor> anchors) : base(anchors) {
            CollectionChanged += OnCollectionChanged;
            InitAllAnchors();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (e == null) return;

            if (e.NewItems != null) {
                foreach (var newItem in e.NewItems) {
                    var newAnchor = (Anchor) newItem;
                    newAnchor.GraphStateChangedEvent += AnchorOnGraphStateChangedEvent;
                }
            }

            if (e.OldItems != null) {
                foreach (var oldItem in e.OldItems) {
                    var oldAnchor = (Anchor) oldItem;
                    oldAnchor.GraphStateChangedEvent -= AnchorOnGraphStateChangedEvent;
                }
            }

            UpdateAnchorNeighbors();
        }

        private void InitAllAnchors() {
            foreach (var anchor in this) {
                anchor.GraphStateChangedEvent += AnchorOnGraphStateChangedEvent;
            }
            UpdateAnchorNeighbors();
        }

        private void AnchorOnGraphStateChangedEvent(object sender, DependencyPropertyChangedEventArgs e) {
            AnchorsChanged?.Invoke(sender, e);
        }

        public void UpdateAnchorNeighbors() {
            Anchor previousAnchor = null;
            foreach (var anchor in this) {
                anchor.PreviousAnchor = previousAnchor;
                if (previousAnchor != null) {
                    previousAnchor.NextAnchor = anchor;
                }

                previousAnchor = anchor;
            }
        }

        #region GraphValueGettingStuff

        public double GetValue(double x) {
            return GetValue(x, this);
        }

        public double GetDerivative(double x) {
            return GetDerivative(x, this);
        }

        public double GetIntegral(double t1, double t2) {
            return GetIntegral(t1, t2, this);
        }

        #endregion

        #region StaticGraphValueGettingStuff

        public static double GetValue(double x, IReadOnlyList<IGraphAnchor> anchors) {
            // Find the section
            var previousAnchor = anchors[0];
            var nextAnchor = anchors[anchors.Count - 1];
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

        public static double GetDerivative(double x, IReadOnlyList<IGraphAnchor> anchors) {
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

        public static double GetIntegral(double t1, double t2, IReadOnlyList<IGraphAnchor> anchors) {
            double height = 0;
            IGraphAnchor previousAnchor = null;
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

        public static double GetMaxValue(IReadOnlyList<IGraphAnchor> anchors) {
            var customExtrema = anchors.Any(o =>
                o.Interpolator.GetType().GetCustomAttribute<CustomExtremaAttribute>() != null);

            if (!customExtrema) {
                // It suffices to find the highest Y value of the anchors, because the interpolated parts never stick out above or below the anchors.
                return anchors.Max(o => o.Pos.Y);
            }

            IGraphAnchor previousAnchor = null;
            double maxValue = double.NegativeInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;

                    IEnumerable<double> values;
                    
                    // If the interpolator has a CustomExtremaAttribute than we check the min/max value for all the specified locations
                    var customExtremaAttribute =
                        anchor.Interpolator.GetType().GetCustomAttribute<CustomExtremaAttribute>();

                    if (customExtremaAttribute != null) {
                        // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                        anchor.Interpolator.P = anchor.Tension;

                        values = customExtremaAttribute.ExtremaPositions
                            .Select(o => p1.Y + difference.Y * anchor.Interpolator.GetInterpolation(o));
                    } else {
                        values = new[] {p1.Y, p2.Y};
                    }

                    var localMaxValue = values.Max();

                    if (localMaxValue > maxValue) {
                        maxValue = localMaxValue;
                    }
                }

                previousAnchor = anchor;
            }

            return maxValue;
        }

        public static double GetMaxDerivative(IReadOnlyList<IGraphAnchor> anchors) {
            IGraphAnchor previousAnchor = null;
            double maxValue = double.NegativeInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;
                    
                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;
                    
                    IEnumerable<double> values;

                    if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                        // If the interpolator has a CustomDerivativeExtremaAttribute than we check the min/max derivative for all the specified locations
                        var customExtremaAttribute =
                            anchor.Interpolator.GetType().GetCustomAttribute<CustomDerivativeExtremaAttribute>();

                        if (customExtremaAttribute != null) {
                            values = customExtremaAttribute.ExtremaPositions
                                .Select(o => derivableInterpolator.GetDerivative(o) * difference.Y / difference.X);
                        } else {
                            values = new[] {0, 1}
                                .Select(o => derivableInterpolator.GetDerivative(o) * difference.Y / difference.X);
                        }
                    } else {
                        values = new[] {difference.Y / difference.X};
                    }
                    
                    var localMaxValue = values.Max();

                    if (localMaxValue > maxValue) {
                        maxValue = localMaxValue;
                    }
                }

                previousAnchor = anchor;
            }

            return maxValue;
        }

        public static double GetMaxIntegral(IReadOnlyList<IGraphAnchor> anchors) {
            double height = 0;
            double maxValue = height;
            IGraphAnchor previousAnchor = null;
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

                    double maxIntegral;
                    double endIntegral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        endIntegral = integrableInterpolator.GetIntegral(0, 1) * difference.X * difference.Y +
                                      difference.X * p1.Y;

                        // If the interpolator has a CustomIntegralExtremaAttribute than we check the min/max integral for all the specified locations
                        var customExtremaAttribute =
                            anchor.Interpolator.GetType().GetCustomAttribute<CustomIntegralExtremaAttribute>();

                        if (customExtremaAttribute != null) {
                            maxIntegral = customExtremaAttribute.ExtremaPositions
                                .Select(o => integrableInterpolator.GetIntegral(0, o)).Max()
                                          * difference.X * difference.Y + difference.X * p1.Y;
                        } else {
                            maxIntegral = integrableInterpolator.GetIntegral(0, 1)
                                          * difference.X * difference.Y + difference.X * p1.Y;
                        }

                        // Check if the interpolation passes through 0
                        if (difference.Y * p1.Y < 0) {
                            // Possibility of max/min not at endpoints. Need to calculate the hard way
                            double newMaxIntegral;

                            if (integrableInterpolator is IInvertibleInterpolator invertibleInterpolator) {
                                // Calculate all the zeros of the interpolation
                                var zeros = invertibleInterpolator.GetInverse(-p1.Y / difference.Y).Where(o => o <= 1 && o >= 0).ToArray();
                                if (zeros.Length > 0) {
                                    newMaxIntegral = zeros.Select(z =>
                                        integrableInterpolator.GetIntegral(0, z) * difference.X * difference.Y +
                                        z * difference.X * p1.Y).Max();
                                } else {
                                    newMaxIntegral = double.NegativeInfinity;
                                }
                            } else {
                                double newMaxIntegralPosition = GradientDescentUtil.GradientAscent(
                                    d => integrableInterpolator.GetIntegral(0, d) * difference.X * difference.Y + d * difference.X * p1.Y,
                                    0, 1, 0.1);

                                newMaxIntegral = integrableInterpolator.GetIntegral(0, newMaxIntegralPosition) * difference.X * difference.Y +
                                                     newMaxIntegralPosition * difference.X * p1.Y;
                            }

                            if (newMaxIntegral > maxIntegral) {
                                maxIntegral = newMaxIntegral;
                            }
                        }
                    } else {
                        endIntegral = 0.5 * difference.X * difference.Y + difference.X * p1.Y;
                        maxIntegral = endIntegral;

                        // Check if the interpolation passes through 0
                        if (difference.Y * p1.Y < 0) {
                            // Possibility of max/min not at endpoints. For a linear interpolator this is possible to solve algebraically
                            var x = -p1.Y / difference.Y;
                            if (x >= 0 && x <= 1) {
                                double newMaxIntegral = 0.5 * Math.Pow(x, 2)
                                                            * difference.X * difference.Y + x * difference.X * p1.Y;

                                if (newMaxIntegral > maxIntegral) {
                                    maxIntegral = newMaxIntegral;
                                }
                            }
                        }
                    }

                    var possibleMaxValue = height + maxIntegral;
                    if (possibleMaxValue > maxValue) {
                        maxValue = possibleMaxValue;
                    }

                    height += endIntegral;
                }

                previousAnchor = anchor;
            }

            return maxValue;
        }

        public static double GetMinValue(IReadOnlyList<IGraphAnchor> anchors) {
            var customExtrema = anchors.Any(o =>
                o.Interpolator.GetType().GetCustomAttribute<CustomExtremaAttribute>() != null);

            if (!customExtrema) {
                // It suffices to find the smallest Y value of the anchors, because the interpolated parts never stick out above or below the anchors.
                return anchors.Min(o => o.Pos.Y);
            }

            IGraphAnchor previousAnchor = null;
            double minValue = double.PositiveInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;

                    IEnumerable<double> values;
                    
                    // If the interpolator has a CustomExtremaAttribute than we check the min/max value for all the specified locations
                    var customExtremaAttribute =
                        anchor.Interpolator.GetType().GetCustomAttribute<CustomExtremaAttribute>();

                    if (customExtremaAttribute != null) {
                        // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                        anchor.Interpolator.P = anchor.Tension;

                        values = customExtremaAttribute.ExtremaPositions
                            .Select(o => p1.Y + difference.Y * anchor.Interpolator.GetInterpolation(o));
                    } else {
                        values = new[] {p1.Y, p2.Y};
                    }

                    var localMinValue = values.Min();

                    if (localMinValue < minValue) {
                        minValue = localMinValue;
                    }
                }

                previousAnchor = anchor;
            }

            return minValue;
        }

        public static double GetMinDerivative(IReadOnlyList<IGraphAnchor> anchors) {
            IGraphAnchor previousAnchor = null;
            double minValue = double.PositiveInfinity;
            foreach (var anchor in anchors) {
                if (previousAnchor != null) {
                    var p1 = previousAnchor.Pos;
                    var p2 = anchor.Pos;

                    var difference = p2 - p1;
                    
                    // Update the interpolator with the tension of the anchor. This doesn't happen automatically
                    anchor.Interpolator.P = anchor.Tension;

                    IEnumerable<double> values;

                    if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                        // If the interpolator has a CustomDerivativeExtremaAttribute than we check the min/max derivative for all the specified locations
                        var customExtremaAttribute =
                            anchor.Interpolator.GetType().GetCustomAttribute<CustomDerivativeExtremaAttribute>();

                        if (customExtremaAttribute != null) {
                            values = customExtremaAttribute.ExtremaPositions
                                .Select(o => derivableInterpolator.GetDerivative(o) * difference.Y / difference.X);
                        } else {
                            values = new[] {0, 1}
                                .Select(o => derivableInterpolator.GetDerivative(o) * difference.Y / difference.X);
                        }
                    } else {
                        values = new[] {difference.Y / difference.X};
                    }
                    
                    var localMinValue = values.Min();

                    if (localMinValue < minValue) {
                        minValue = localMinValue;
                    }
                }

                previousAnchor = anchor;
            }

            return minValue;
        }

        public static double GetMinIntegral(IReadOnlyList<IGraphAnchor> anchors) {
            double height = 0;
            double minValue = double.PositiveInfinity;
            IGraphAnchor previousAnchor = null;
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

                    double minIntegral;
                    double endIntegral;
                    if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                        endIntegral = integrableInterpolator.GetIntegral(0, 1) * difference.X * difference.Y +
                                      difference.X * p1.Y;

                        // If the interpolator has a CustomIntegralExtremaAttribute than we check the min/max integral for all the specified locations
                        var customExtremaAttribute =
                            anchor.Interpolator.GetType().GetCustomAttribute<CustomIntegralExtremaAttribute>();

                        if (customExtremaAttribute != null) {
                            minIntegral = customExtremaAttribute.ExtremaPositions
                                .Select(o => integrableInterpolator.GetIntegral(0, o) * difference.X * difference.Y + o * difference.X * p1.Y).Min();
                        } else {
                            minIntegral = integrableInterpolator.GetIntegral(0, 1)
                                          * difference.X * difference.Y + difference.X * p1.Y;
                        }

                        // Check if the interpolation passes through 0
                        if (difference.Y * p1.Y < 0) {
                            // Possibility of max/min not at endpoints. Need to calculate the hard way
                            double newMinIntegral;

                            if (integrableInterpolator is IInvertibleInterpolator invertibleInterpolator) {
                                // Calculate all the zeros of the interpolation
                                var zeros = invertibleInterpolator.GetInverse(-p1.Y / difference.Y).Where(o => o <= 1 && o >= 0).ToArray();
                                if (zeros.Length > 0) {
                                    newMinIntegral = zeros.Select(z =>
                                        integrableInterpolator.GetIntegral(0, z) * difference.X * difference.Y +
                                        z * difference.X * p1.Y).Min();
                                } else {
                                    newMinIntegral = double.PositiveInfinity;
                                }
                            } else {
                                double newMinIntegralPosition = GradientDescentUtil.GradientDescent(
                                    d => integrableInterpolator.GetIntegral(0, d) * difference.X * difference.Y + d * difference.X * p1.Y,
                                    0, 1, 0.1);

                                newMinIntegral = integrableInterpolator.GetIntegral(0, newMinIntegralPosition) * difference.X * difference.Y + 
                                                        newMinIntegralPosition * difference.X * p1.Y;
                            }

                            if (newMinIntegral < minIntegral) {
                                minIntegral = newMinIntegral;
                            }
                        }
                    } else {
                        endIntegral = 0.5 * difference.X * difference.Y + difference.X * p1.Y;
                        minIntegral = endIntegral;

                        // Check if the interpolation passes through 0
                        if (difference.Y * p1.Y < 0) {
                            // Possibility of max/min not at endpoints. For a linear interpolator this is possible to solve algebraically
                            var x = -p1.Y / difference.Y;
                            if (x >= 0 && x <= 1) {
                                double newMinIntegral = 0.5 * Math.Pow(x, 2)
                                                            * difference.X * difference.Y + x * difference.X * p1.Y;

                                if (newMinIntegral > minIntegral) {
                                    minIntegral = newMinIntegral;
                                }
                            }
                        }
                    }

                    var possibleMinValue = height + minIntegral;
                    if (possibleMinValue < minValue) {
                        minValue = possibleMinValue;
                    }

                    height += endIntegral;
                }

                previousAnchor = anchor;
            }

            return minValue;
        }

        #endregion

        #region OtherValueGettingStuff
        
        public double GetDistanceTraveled() {
            double distance = 0;
            IGraphAnchor previousAnchor = null;
            foreach (var anchor in this) {
                if (previousAnchor != null) {
                    distance += Math.Abs(GetValue(anchor.Pos.X) - GetValue(previousAnchor.Pos.X));
                }

                previousAnchor = anchor;
            }

            return distance;
        }

        // Note: This method is not accurate for segments passing through zero
        public double GetIntegralDistanceTraveled() {
            double distance = 0;
            IGraphAnchor previousAnchor = null;
            foreach (var anchor in this) {
                if (previousAnchor != null) {
                    distance += Math.Abs(GetIntegral(previousAnchor.Pos.X, anchor.Pos.X));
                }

                previousAnchor = anchor;
            }

            return distance;
        }

        #endregion
    }
}