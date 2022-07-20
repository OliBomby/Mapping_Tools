using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options {
    /// <summary>
    /// Parametric shape of a tumour.
    /// </summary>
    public interface ITumourTemplate {
        /// <summary>
        /// The length along the curve.
        /// </summary>
        double Length { get; set; }

        /// <summary>
        /// The size of the protrusion.
        /// </summary>
        double Width { get; set; }

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
        /// Gets how many points should be used to approximate this tumour shape.
        /// </summary>
        int GetDetailLevel();

        /// <summary>
        /// Gets a list of t values which should definitely get a point.
        /// This is to ensure sharp corners in the tumour shape stay sharp.
        /// This should not contain 0 or 1 and be in increasing order.
        /// </summary>
        IEnumerable<double> GetCriticalPoints();

        /// <summary>
        /// Gets the list of anchors which reconstructs the shape of this tumour.
        /// </summary>
        List<Vector2> GetReconstructionHint();

        /// <summary>
        /// Gets the path type for the anchors which reconstructs the shape of this tumour.
        /// </summary>
        PathType GetReconstructionHintPathType();

        /// <summary>
        /// The relation [0,1] -> [0,1] between cumulative length on the curve and cumulative length on the hint path.
        /// If null, this relation is assumed to be linear.
        /// </summary>
        Func<double, double> GetDistanceRelation();
    }
}