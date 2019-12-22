namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IIntegrableInterpolator {
        /// <summary>
        /// For a section running from p1=(x1, C) to p2=(x2, C + F'(x2) - F'(x1)) where F' is the primitive of the full interpolation function of interpolator f
        /// this method will calculate the interpolator g such that the full interpolation function g' interpolates between p1 and p2 and is equivalent to F'.
        /// <code>
        /// g'(x) = y1 + (y2 - y1) * g((x - x1) / (x2 - x1))
        ///</code>
        /// 
        /// g can be calculated as follows with F as the primitive of f:
        /// <code>g(x) = (x * y1 * (x2 - x1) + (x2 - x1) * (y2 - y1) * F(x)) / (y1 * (x2 - x1) + (x2 - x1) * (y2 - y1) * (F(1) - F(0)))</code>
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        IGraphInterpolator GetPrimitiveInterpolator(double x1, double y1, double x2, double y2);

        /// <summary>
        /// Calculates the integral of the interpolator function between t1 and t2.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        double GetIntegral(double t1, double t2);
    }
}