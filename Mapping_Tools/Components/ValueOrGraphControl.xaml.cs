using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Markers;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Mapping_Tools.Components {
    public partial class ValueOrGraphControl {
        public static readonly DependencyProperty GraphStateProperty =
            DependencyProperty.Register(nameof(GraphState), typeof(GraphState), typeof(ValueOrGraphControl),
                new FrameworkPropertyMetadata(CreateDefaultGraphState(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public GraphState GraphState {
            get => (GraphState)GetValue(GraphStateProperty);
            set => SetValue(GraphStateProperty, value);
        }

        public ValueOrGraphControl() {
            InitializeComponent();
        }

        private static GraphState CreateDefaultGraphState() {
            return new GraphState {
                MinX = 0,
                MinY = 0,
                MaxX = 1,
                MaxY = 1,
                Anchors = new List<AnchorState>() {
                    new AnchorState { Pos = new Vector2(0, 0) },
                    new AnchorState { Pos = new Vector2(1, 1) }
                }
            };
        }

        public void DialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs) {
            Graph.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));
            Graph.SetGraphState(GraphState ?? CreateDefaultGraphState());
            Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 1 / 4d);
            Graph.HorizontalMarkerGenerator = new DoubleMarkerGenerator(0, 1 / 4d);
        }

        public void DialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs) {
            if (Equals(eventArgs.Parameter, "1")) {
                var newState = Graph.GetGraphState();
                newState.Freeze();
                GraphState = newState;
            }
        }
    }
}
