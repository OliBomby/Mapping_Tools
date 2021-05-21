using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public class GraphStateValueGetter {
        private readonly GraphState _graphState;
        private readonly double _multiplier;
        private readonly double _offset;

        public GraphStateValueGetter(GraphState graphState, double multiplier = 1, double offset = 0) {
            _graphState = graphState;
            _multiplier = multiplier;
            _offset = offset;
        }

        public double GetValue(double x) {
            return _offset + _multiplier * _graphState.GetValue(x);
        }

        public double GetDerivative(double x) {
            return _offset + _multiplier * _graphState.GetDerivative(x);
        }

        public double GetIntegral(double x) {
            return _offset + _multiplier * _graphState.GetIntegral(0, x);
        }
    }
}