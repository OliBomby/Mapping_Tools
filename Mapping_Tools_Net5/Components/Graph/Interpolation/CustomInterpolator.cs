using System.ComponentModel;
using Newtonsoft.Json;

namespace Mapping_Tools.Components.Graph.Interpolation {
    [IgnoreInterpolator]
    [DisplayName("Custom")]
    public class CustomInterpolator : IGraphInterpolator {
        public delegate double InterpolationDelegate(double t);

        [JsonIgnore]
        public InterpolationDelegate InterpolationFunction { get; set; }
        public double P { get; set; } = 0;

        public CustomInterpolator() {
            InterpolationFunction = t => t;
        }

        public CustomInterpolator(InterpolationDelegate interpolationFunction) {
            InterpolationFunction = interpolationFunction;
        }

        public double GetInterpolation(double t) {
            return InterpolationFunction(t);
        }
    }
}