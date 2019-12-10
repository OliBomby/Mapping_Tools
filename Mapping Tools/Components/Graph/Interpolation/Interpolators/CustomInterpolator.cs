using System.ComponentModel;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Custom")]
    public class CustomInterpolator : IGraphInterpolator {
        public delegate double InterpolationFunction(double t, double p);

        public delegate double FullInterpolationFunction(double t, double h1, double h2, double parameter);

        private readonly InterpolationFunction _function;
        private readonly FullInterpolationFunction _fullFunction;
        private readonly bool _fullCustom;

        public CustomInterpolator(InterpolationFunction function) {
            _function = function;
            _fullCustom = false;
        }

        public CustomInterpolator(FullInterpolationFunction fullFunction) {
            _fullFunction = fullFunction;
            _fullCustom = true;
        }

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            if (_fullCustom) {
                return _fullFunction(t, h1, h2, parameter);
            }
            return h1 + (h2 - h1) * _function(t, parameter);
        }
    }
}