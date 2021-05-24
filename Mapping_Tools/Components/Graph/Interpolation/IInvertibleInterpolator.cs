using System.Collections.Generic;

namespace Mapping_Tools.Components.Graph.Interpolation {
    public interface IInvertibleInterpolator {
        /// <summary>
        /// Calculates all X values that correspond to specified Y value.
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        IEnumerable<double> GetInverse(double y);
    }
}