namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [IgnoreInterpolator]
    public class ParabolaInterpolator : CustomInterpolator {
        public string Name => "Parabola";

        public ParabolaInterpolator() : base((t, p) => -2 * p * t * t + (2 * p + 1) * t) {}
    }
}