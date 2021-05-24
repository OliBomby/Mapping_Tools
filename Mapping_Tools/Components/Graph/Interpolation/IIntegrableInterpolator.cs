namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IIntegrableInterpolator {
        /// <summary>
        /// Calculates the integral of the interpolator function between t1 and t2.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        double GetIntegral(double t1, double t2);
    }
}