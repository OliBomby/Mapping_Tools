using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff {
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
        /// Gets the minimum number of points required to properly approximate this tumour shape.
        /// </summary>
        /// <returns>The minimum number of points required to properly approximate this tumour shape.</returns>
        int GetResolution();
    }
}