namespace Mapping_Tools.Core.Audio;

/// <summary>Describes one effect without retaining a framework or audio-library object.</summary>
public sealed class AudioEffect
{
    private AudioEffect(AudioEffectKind kind, double firstValue, double secondValue)
    {
        Kind = kind;
        FirstValue = firstValue;
        SecondValue = secondValue;
    }

    /// <summary>Gets the effect kind.</summary>
    public AudioEffectKind Kind { get; }

    /// <summary>Gets the first effect value, whose units depend on <see cref="Kind" />.</summary>
    public double FirstValue { get; }

    /// <summary>Gets the second effect value, whose units depend on <see cref="Kind" />.</summary>
    public double SecondValue { get; }

    /// <summary>Creates a delayed fade-out effect.</summary>
    /// <param name="delay">Time in milliseconds before fading starts.</param>
    /// <param name="duration">Fade duration in milliseconds.</param>
    /// <returns>The effect description.</returns>
    public static AudioEffect CreateDelayFadeOut(double delay, double duration)
    {
        if (!double.IsFinite(delay) || delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));

        if (!double.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));

        return new AudioEffect(AudioEffectKind.DelayFadeOut, delay, duration);
    }

    /// <summary>Creates the legacy soft limiter configuration.</summary>
    /// <param name="boostDecibels">Input boost in decibels.</param>
    /// <param name="brickwallDecibels">Output ceiling in decibels.</param>
    /// <returns>The effect description.</returns>
    public static AudioEffect CreateSoftLimiter(double boostDecibels = 0, double brickwallDecibels = -0.1)
    {
        if (!double.IsFinite(boostDecibels) || boostDecibels is < 0 or > 18) throw new ArgumentOutOfRangeException(nameof(boostDecibels));

        if (!double.IsFinite(brickwallDecibels) || brickwallDecibels is < -3 or > 1) throw new ArgumentOutOfRangeException(nameof(brickwallDecibels));

        return new AudioEffect(AudioEffectKind.SoftLimiter, boostDecibels, brickwallDecibels);
    }
}

