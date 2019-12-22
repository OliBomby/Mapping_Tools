namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IDerivableInterpolator {
        /// <summary>
        /// For a section running from p1=(x1, F'(x1)) to p2=(x2, F'(x2)) where F' is the derivative of the full interpolation function of interpolator f
        /// this method will calculate the interpolator g such that the full interpolation function G interpolates between p1 and p2 and is equivalent to F'.
        /// 
        /// g can be calculated as follows with f' as the derivative of f:
        /// <code>
        /// G(x) = p1.y + (p2.y - p1.y) * g((x - p1.x) / (p2.x - p1.x))
        /// F(x) = y1 + (y2 - y1) * g((x - x1) / (x2 - x1))
        /// F'(x) = G(x) = ((y2 - y1) * f'((x - x1) / (x2 - x1))) / (x2 - x1)
        /// </code>
        /// Solve this to get:
        /// <code>
        /// g(x) = (f'(x) - f'(0)) / (f'(1) - f'(0))
        /// </code>
        /// </summary>
        /// <returns></returns>
        IGraphInterpolator GetDerivativeInterpolator();

        /// <summary>
        /// Calculates the derivative of the interpolator function at t.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        double GetDerivative(double t);
    }
}