using System.Security.Cryptography;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

namespace Mapping_Tools.Components.Graph.Interpolators {
    [IgnoreInterpolator]
    public class LinearInterpolator : IGraphInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        public string Name => "Linear";
        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            return h1 + (h2 - h1) * t;
        }

        public IGraphInterpolator GetDerivative() {
            return new CustomInterpolator(((t, h1, h2, parameter) => h2 - h1));
        }

        public IGraphInterpolator GetPrimitive() {
            return new CustomInterpolator(((t, h1, h2, parameter) => h1 * t + 0.5 * (h2 - h1) * t * t));
        }
    }
}