namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IDerivableInterpolator {
        /// <summary>
        /// Calculates the derivative of the interpolator function at t.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        double GetDerivative(double t);
    }
}