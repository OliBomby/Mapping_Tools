using System.Windows;
using System.Windows.Media.Animation;

namespace Mapping_Tools.Components.Graph {
    public class GraphDoubleAnimation : DoubleAnimationBase {
        public static readonly DependencyProperty GraphStateProperty =
            DependencyProperty.Register("GraphState",
                typeof(GraphState),
                typeof(GraphDoubleAnimation),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register("From",
                typeof(double?),
                typeof(GraphDoubleAnimation),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register("To",
                typeof(double?),
                typeof(GraphDoubleAnimation),
                new PropertyMetadata(null));

        public static readonly DependencyProperty MultiplierProperty =
            DependencyProperty.Register("Multiplier",
                typeof(double),
                typeof(GraphDoubleAnimation),
                new PropertyMetadata(1d));

        public static readonly DependencyProperty OffsetProperty =
            DependencyProperty.Register("Offset",
                typeof(double),
                typeof(GraphDoubleAnimation),
                new PropertyMetadata(0d));

        /// <summary>
        ///     Specifies which graph to use for the values.
        /// </summary>
        public GraphState GraphState {
            get => (GraphState) GetValue(GraphStateProperty);
            set => SetValue(GraphStateProperty, value);
        }

        /// <summary>
        ///     Specifies the starting value of the animation.
        /// </summary>
        public double? From {
            get => (double?) GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        /// <summary>
        ///     Specifies the ending value of the animation.
        /// </summary>
        public double? To {
            get => (double?) GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        /// <summary>
        ///     Specifies the multiplier to the values of the graph.
        /// </summary>
        public double Multiplier {
            get => (double) GetValue(MultiplierProperty);
            set => SetValue(MultiplierProperty, value);
        }

        /// <summary>
        ///     Specifies the offset to the values of the graph.
        /// </summary>
        public double Offset {
            get => (double) GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }

        protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue,
            AnimationClock clock) {
            var start = From ?? defaultOriginValue;
            var delta = To - start ?? defaultOriginValue - start;

            return clock.CurrentProgress == null ? Offset + Multiplier * start : Offset + Multiplier * GraphState.GetValue(start + clock.CurrentProgress.Value * delta);
        }

        protected override Freezable CreateInstanceCore() {
            return new GraphDoubleAnimation();
        }
    }
}