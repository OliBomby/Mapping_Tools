using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph.Interpolation;
using System.Windows;

namespace Mapping_Tools.Components.Graph {
    public class AnchorState : Freezable, IGraphAnchor {
        public static readonly DependencyProperty PosProperty =
            DependencyProperty.Register(nameof(Pos),
                typeof(Vector2), 
                typeof(AnchorState), 
                new FrameworkPropertyMetadata(Vector2.Zero, FrameworkPropertyMetadataOptions.None));

        public Vector2 Pos {
            get => (Vector2) GetValue(PosProperty);
            set => SetValue(PosProperty, value);
        }
        
        
        public static readonly DependencyProperty InterpolatorProperty =
            DependencyProperty.Register(nameof(Interpolator),
                typeof(IGraphInterpolator), 
                typeof(AnchorState), 
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None));
        
        public IGraphInterpolator Interpolator {
            get => (IGraphInterpolator) GetValue(InterpolatorProperty);
            set => SetValue(InterpolatorProperty, value);
        }

        
        public static readonly DependencyProperty TensionProperty =
            DependencyProperty.Register(nameof(Tension),
                typeof(double), 
                typeof(AnchorState), 
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None));
        
        public double Tension {
            get => (double) GetValue(TensionProperty);
            set => SetValue(TensionProperty, value);
        }

        protected override Freezable CreateInstanceCore() {
            return new AnchorState();
        }

        public Anchor GetAnchor() {
            return new Anchor(null, Pos, Interpolator) {Tension = Tension};
        }
    }
}