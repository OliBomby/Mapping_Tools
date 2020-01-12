using System.ComponentModel;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    [DisplayName("Linear")]
    public class LinearInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        public LinearInterpolator() : base(t => t) {}

        public double GetDerivative(double t) {
            return 1;
        }

        public double GetIntegral(double t1, double t2) {
            return 0.5 * t2 * t2 - 0.5 * t1 * t1;
        }
    }
}