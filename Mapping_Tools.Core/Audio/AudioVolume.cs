namespace Mapping_Tools.Core.Audio;

/// <summary>
///     Converts osu! linear volume values to the amplitude curve used by hitsound generation.
/// </summary>
public static class AudioVolume
{
    private static readonly double HeightAt005 = 0.995 * Math.Pow(0.05, 1.5) + 0.005;

    /// <summary>
    ///     Converts a normalized osu! volume value to an amplitude multiplier.
    /// </summary>
    /// <param name="volume">The normalized volume value. Values outside the usual range are preserved by the curve.</param>
    /// <returns>The corresponding amplitude multiplier.</returns>
    public static double ToAmplitude(double volume)
    {
        return volume < 0.05
            ? HeightAt005 / 0.05 * volume
            : 0.995 * Math.Pow(volume, 1.5) + 0.005;
    }

    /// <summary>
    ///     Converts an amplitude multiplier back to the osu! volume curve.
    /// </summary>
    /// <param name="amplitude">The amplitude multiplier to convert.</param>
    /// <returns>The corresponding normalized volume value.</returns>
    public static double FromAmplitude(double amplitude)
    {
        return amplitude < HeightAt005
            ? 0.05 / HeightAt005 * amplitude
            : Math.Pow((amplitude - 0.005) / 0.995, 1 / 1.5);
    }
}
