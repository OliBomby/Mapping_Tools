using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public class GraphStateValueGetter {
        private readonly GraphState graphState;
        private readonly double multiplier;
        private readonly double offset;

        public GraphStateValueGetter(GraphState graphState, double multiplier = 1, double offset = 0) {
            this.graphState = graphState;
            this.multiplier = multiplier;
            this.offset = offset;
        }

        public double GetValue(double x) {
            return offset + multiplier * graphState.GetValue(x);
        }

        public double GetDerivative(double x) {
            return offset + multiplier * graphState.GetDerivative(x);
        }

        public double GetIntegral(double x) {
            return offset + multiplier * graphState.GetIntegral(0, x);
        }
    }
}