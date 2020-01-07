using System.Collections.Generic;
using System.Windows;

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