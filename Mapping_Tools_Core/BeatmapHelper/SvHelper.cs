using Mapping_Tools_Core.MathUtil;

namespace Mapping_Tools_Core.BeatmapHelper {
    public class SvHelper {
        private static double FilterSv(double sv) {
            return double.IsNaN(sv) ? 1 : MathHelper.Clamp(sv, 0.1, 10);
        }

        /// <summary>
        /// Calculates the duration of one span of a slider in terms of a number of beats.
        /// </summary>
        /// <param name="pixelLength">The length of the slider in osu! pixels.</param>
        /// <param name="sv">The slider multiplier from inherited timing point.</param>
        /// <param name="globalSv">The global slider velocity.</param>
        /// <returns>The number of beats in one span of the slider.</returns>
        public static double CalculateSliderBeatLength(double pixelLength, double sv, double globalSv) {
            return pixelLength / (100 * globalSv * FilterSv(sv));
        }

        /// <summary>
        /// Calculates the duration of one span of a slider.
        /// </summary>
        /// <param name="pixelLength">The length of the slider in osu! pixels.</param>
        /// <param name="mpb">The milliseconds per beat.</param>
        /// <param name="sv">The slider multiplier from inherited timing point.</param>
        /// <param name="globalSv">The global slider velocity.</param>
        /// <returns>The duration of one span of the slider in milliseconds.</returns>
        public static double CalculateSliderDuration(double pixelLength, double mpb, double sv, double globalSv) {
            return (pixelLength * mpb) / (100 * globalSv * FilterSv(sv));
        }

        /// <summary>
        /// Calculates the pixel length of a slider.
        /// </summary>
        /// <param name="duration">The duration of the slider in milliseconds.</param>
        /// <param name="mpb">The milliseconds per beat.</param>
        /// <param name="sv">The slider multiplier from inherited timing point.</param>
        /// <param name="globalSv">The global slider velocity.</param>
        /// <returns>The pixel length of the slider</returns>
        public static double CalculatePixelLength(double duration, double mpb, double sv, double globalSv) {
            return 100 * globalSv * duration * FilterSv(sv) / mpb;
        }

        /// <summary>
        /// Calculates the greenline slider velocity multiplier of a slider.
        /// </summary>
        /// <param name="pixelLength">The length of the slider in osu! pixels.</param>
        /// <param name="mpb">The milliseconds per beat.</param>
        /// <param name="duration">The duration of the slider in milliseconds.</param>
        /// <param name="globalSv">The global slider velocity.</param>
        /// <returns>The greenline slider velocity multiplier of the slider.</returns>
        public static double CalculateSliderVelocity(double pixelLength, double mpb, double duration, double globalSv) {
            return (pixelLength * mpb) / (duration * 100 * globalSv);
        }
    }
}