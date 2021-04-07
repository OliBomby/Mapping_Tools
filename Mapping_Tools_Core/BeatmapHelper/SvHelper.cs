using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper {
    public class SvHelper {
        /// <summary>
        /// Calculates the duration of one span of a slider.
        /// </summary>
        /// <param name="pixelLength">The length of the slider in osu! pixels.</param>
        /// <param name="mpb">The milliseconds per beat.</param>
        /// <param name="sv">The slider multiplier from inherited timing point.</param>
        /// <param name="globalSv">The global slider velocity.</param>
        /// <returns>The duration of one span of the slider in milliseconds.</returns>
        public static double CalculateSliderDuration(double pixelLength, double mpb, double sv, double globalSv) {
            return (pixelLength * mpb) /
                   (100 * globalSv * (double.IsNaN(sv) ? 1 : MathHelper.Clamp(sv, 0.1, 10)));
        }
    }
}