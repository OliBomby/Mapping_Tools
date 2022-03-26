using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options {
    /// <summary>
    /// Parametric shape of a tumour.
    /// </summary>
    public interface ITumourTemplate {
        /// <summary>
        /// Gets the position along the tumour shape at completion <see cref="t"/>.
        /// The value of <see cref="t"/> should correspond linearly to the cumulative length of this function.
        /// Imagine the X-axis as the slider going from left to right.
        /// The Y direction is to go outwards from the slider.
        /// </summary>
        /// <param name="t">Value between 0 (start) and 1 (end).</param>
        /// <returns></returns>
        Vector2 GetOffset(double t);

        /// <summary>
        /// Gets the total length of this tumour shape.
        /// </summary>
        double GetLength();

        /// <summary>
        /// Gets the default distance between the start point and the end point of the tumour.
        /// Could be used to determine the scale of <see cref="GetOffset(double)"/>.
        /// </summary>
        double GetDefaultSpan();

        /// <summary>
        /// Gets a list of t values which should definitely get a point.
        /// This is to ensure sharp corners in the tumour shape stay sharp.
        /// </summary>
        IEnumerable<double> GetCriticalPoints();

        /// <summary>
        /// Gets the list of anchors which reconstructs the shape of this tumour.
        /// </summary>
        List<Vector2> GetReconstructionHint();
    }
}