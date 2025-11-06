namespace Mapping_Tools.Domain.Beatmaps.Sections;

/// <summary>
/// Contains all the values in the [Difficulty] section of a .osu file.
/// </summary>
public class SectionDifficulty {
    /// <summary>
    /// Determines how fast the approach circles approach the hit objects.
    /// Higher approach rate means a faster approach.
    /// </summary>
    public float ApproachRate { get; set; } = 5;

    /// <summary>
    /// Determines the size of the hit circles.
    /// Higher circle size means smaller circles.
    /// </summary>
    public float CircleSize { get; set; } = 5;

    /// <summary>
    /// Determines how fast HP drains.
    /// </summary>
    public float HpDrainRate { get; set; } = 5;

    /// <summary>
    /// Determines how tight the hit-windows are.
    /// Higher overall difficulty means smaller hit-windows.
    /// </summary>
    public float OverallDifficulty { get; set; } = 5;

    /// <summary>
    /// Global slider multiplier. Determines how many hundreds of osu! pixels the sliderball travels per beat.
    /// </summary>
    public double SliderMultiplier { get; set; } = 1.4;

    /// <summary>
    /// Determines how many slider ticks get placed in one beat.
    /// </summary>
    public double SliderTickRate { get; set; } = 1;

    /// <summary>
    /// Gets the time in milliseconds between a hit object appearing on screen and getting perfectly hit.
    /// </summary>
    public double ApproachTime => GetApproachTime(ApproachRate);

    /// <summary>
    /// Gets the radius of a hit circle.
    /// </summary>
    public double HitObjectRadius => GetHitObjectRadius(CircleSize);

    /// <summary>
    /// Gets the X and Y offset in osu! pixels between two objects in a stack.
    /// </summary>
    public double StackOffset => GetStackOffset(CircleSize);

    /// <summary>
    /// Maps a difficulty value [0, 10] to a two-piece linear range of values.
    /// </summary>
    /// <param name="difficulty">The difficulty value to be mapped.</param>
    /// <param name="min">Minimum of the resulting range which will be achieved by a difficulty value of 0.</param>
    /// <param name="mid">Midpoint of the resulting range which will be achieved by a difficulty value of 5.</param>
    /// <param name="max">Maximum of the resulting range which will be achieved by a difficulty value of 10.</param>
    /// <returns>Value to which the difficulty value maps in the specified range.</returns>
    public static double DifficultyRange(double difficulty, double min, double mid, double max) {
        if (difficulty > 5)
            return mid + (max - mid) * (difficulty - 5) / 5;
        if (difficulty < 5)
            return mid - (mid - min) * (5 - difficulty) / 5;

        return mid;
    }

    /// <summary>
    /// Calculates the time in milliseconds between a hit object appearing on screen and getting perfectly hit for a given approach rate value.
    /// </summary>
    /// <param name="approachRate">The approach rate difficulty setting.</param>
    /// <returns>The time in milliseconds between a hit object appearing on screen and getting perfectly hit.</returns>
    public static double GetApproachTime(double approachRate) {
        return SectionDifficulty.DifficultyRange(approachRate, 1800, 1200, 450);
    }

    /// <summary>
    /// Calculates the radius of a hit circle from a given Circle Size difficulty.
    /// </summary>
    /// <param name="circleSize">The circle size difficulty setting.</param>
    /// <returns>The radius of a hit circle.</returns>
    public static double GetHitObjectRadius(double circleSize) {
        return (109 - 9 * circleSize) / 2;
    }

    /// <summary>
    /// Calculates in osu! pixels the X and Y offset between two objects in a stack.
    /// </summary>
    /// <param name="circleSize">The circle size difficulty setting.</param>
    /// <returns>The X and Y offset in osu! pixels between two objects in a stack.</returns>
    public static double GetStackOffset(double circleSize) {
        return GetHitObjectRadius(circleSize) / 10;
    }
}