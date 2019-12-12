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

        /// <summary>
        ///     Specifies which graph state to use for the values.
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

        protected override double GetCurrentValueCore(double defaultOriginValue, double defaultDestinationValue,
            AnimationClock clock) {
            var start = From ?? defaultOriginValue;
            var delta = To - start ?? defaultOriginValue - start;

            if (clock.CurrentProgress == null) return start;

            return GraphState.GetValue(clock.CurrentProgress.Value) * delta + start;
        }

        protected override Freezable CreateInstanceCore() {
            return new GraphDoubleAnimation();
        }
    }
}