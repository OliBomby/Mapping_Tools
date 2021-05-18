using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Components.Graph {
    /// <summary>
    /// This class is meant to contain all the defining information for a <see cref="Graph"/> instance.
    /// You can use this for serialization or to transport information of a <see cref="Graph"/> between threads.
    /// </summary>
    public class GraphState : Freezable {
        #region DependencyProperties

        public static readonly DependencyProperty AnchorsProperty =
            DependencyProperty.Register(nameof(Anchors),
                typeof(List<AnchorState>), 
                typeof(GraphState), 
                new FrameworkPropertyMetadata(null));

        public List<AnchorState> Anchors {
            get => (List<AnchorState>) GetValue(AnchorsProperty);
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

        protected override bool FreezeCore(bool isChecking) {
            if (isChecking) {
                if (Anchors.Any(a => !a.CanFreeze)) {
                    return false;
                }
            } else {
                Anchors.ForEach(a => a.Freeze());
            }

            return base.FreezeCore(isChecking);
        }

        public double GetValue(double x) {
            return AnchorCollection.GetValue(x, Anchors);
        }

        public double GetDerivative(double x) {
            return AnchorCollection.GetDerivative(x, Anchors);
        }

        public double GetIntegral(double t1, double t2) {
            return AnchorCollection.GetIntegral(t1, t2, Anchors);
        }
    }
}