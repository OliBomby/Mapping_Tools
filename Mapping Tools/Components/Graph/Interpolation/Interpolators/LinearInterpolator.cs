using System.ComponentModel;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    [DisplayName("Linear")]
    public class LinearInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        public LinearInterpolator() : base((t, p) => t) {}

        public IGraphInterpolator GetDerivativeInterpolator() {
            return new LinearInterpolator();
        }

        public double GetDerivative(double t) {
            return 1;
        }

        public IGraphInterpolator GetPrimitiveInterpolator() {
            return new CustomInterpolator((t, p) => t * (p * (t - 1) + 1)) {P = 1};
        }

        public double GetIntegral(double t) {
            return 0.5 * t * t;
        }
    }
}