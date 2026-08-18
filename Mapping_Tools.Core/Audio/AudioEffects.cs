namespace Mapping_Tools.Core.Audio;

/// <summary>Identifies a reusable audio effect supported by the hitsound pipeline.</summary>
public enum AudioEffectKind
{
    /// <summary>Delays and then fades the signal to silence.</summary>
    DelayFadeOut,

    /// <summary>Applies the legacy soft clipper/limiter transfer curve.</summary>
    SoftLimiter
}

/// <summary>Describes one effect without retaining a framework or audio-library object.</summary>
public sealed class AudioEffect
{
    private AudioEffect(AudioEffectKind kind, double firstValue, double secondValue)
    {
        Kind = kind;
        FirstValue = firstValue;
        SecondValue = secondValue;
    }

    /// <summary>Creates a delayed fade-out effect.</summary>
    /// <param name="delay">Time in milliseconds before fading starts.</param>
    /// <param name="duration">Fade duration in milliseconds.</param>
    /// <returns>The effect description.</returns>
    public static AudioEffect CreateDelayFadeOut(double delay, double duration)
    {
        if (!double.IsFinite(delay) || delay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delay));
        }

        if (!double.IsFinite(duration) || duration < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        return new AudioEffect(AudioEffectKind.DelayFadeOut, delay, duration);
    }

    /// <summary>Creates the legacy soft limiter configuration.</summary>
    /// <param name="boostDecibels">Input boost in decibels.</param>
    /// <param name="brickwallDecibels">Output ceiling in decibels.</param>
    /// <returns>The effect description.</returns>
    public static AudioEffect CreateSoftLimiter(double boostDecibels = 0, double brickwallDecibels = -0.1)
    {
        if (!double.IsFinite(boostDecibels) || boostDecibels is < 0 or > 18)
        {
            throw new ArgumentOutOfRangeException(nameof(boostDecibels));
        }

        if (!double.IsFinite(brickwallDecibels) || brickwallDecibels is < -3 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(brickwallDecibels));
        }

        return new AudioEffect(AudioEffectKind.SoftLimiter, boostDecibels, brickwallDecibels);
    }

    /// <summary>Gets the effect kind.</summary>
    public AudioEffectKind Kind { get; }

    /// <summary>Gets the first effect value, whose units depend on <see cref="Kind"/>.</summary>
    public double FirstValue { get; }

    /// <summary>Gets the second effect value, whose units depend on <see cref="Kind"/>.</summary>
    public double SecondValue { get; }
}

/// <summary>Applies framework-neutral effects to an owned audio clip.</summary>
public static class AudioEffectEngine
{
    private const double AmpDb = 8.6562;
    private const double BaselineThresholdDb = -9;
    private const double A = 1.017;
    private const double B = -0.025;

    /// <summary>
    /// Applies effects in order and returns a new clip, leaving the source unchanged.
    /// </summary>
    /// <param name="source">The source clip.</param>
    /// <param name="effects">Effects in processing order.</param>
    /// <param name="cancellationToken">Token checked between processed frames.</param>
    /// <returns>A newly allocated processed clip.</returns>
    public static AudioClip Apply(
        AudioClip source,
        IEnumerable<AudioEffect> effects,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(effects);

        float[] samples = source.CopySamples();
        foreach (AudioEffect effect in effects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(effect);
            switch (effect.Kind)
            {
                case AudioEffectKind.DelayFadeOut:
                    ApplyFadeOut(samples, source.Format, effect.FirstValue, effect.SecondValue, cancellationToken);
                    break;
                case AudioEffectKind.SoftLimiter:
                    ApplySoftLimiter(samples, effect.FirstValue, effect.SecondValue, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect));
            }
        }

        return new AudioClip(source.Format, samples);
    }

    private static void ApplyFadeOut(
        float[] samples,
        AudioFormat format,
        double delay,
        double duration,
        CancellationToken cancellationToken)
    {
        int delayFrames = (int)Math.Round(delay * format.SampleRate / 1000, MidpointRounding.ToEven);
        int durationFrames = Math.Max(1, (int)Math.Round(duration * format.SampleRate / 1000, MidpointRounding.ToEven));
        for (int frame = delayFrames; frame < samples.Length / format.Channels; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double progress = (double)(frame - delayFrames) / durationFrames;
            float multiplier = (float)Math.Clamp(1 - progress, 0, 1);
            for (int channel = 0; channel < format.Channels; channel++)
            {
                samples[frame * format.Channels + channel] *= multiplier;
            }
        }
    }

    private static void ApplySoftLimiter(
        float[] samples,
        double boost,
        double brickwall,
        CancellationToken cancellationToken)
    {
        double threshold = BaselineThresholdDb + brickwall;
        for (int index = 0; index < samples.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float sample = samples[index];
            if (sample == 0)
            {
                continue;
            }

            double decibels = AmpDb * Math.Log(Math.Abs(sample)) + boost;
            if (decibels > threshold)
            {
                double over = decibels - threshold;
                over = A * over + B * over * over;
                decibels = Math.Min(threshold + over, brickwall);
            }

            samples[index] = (float)(Math.Exp(decibels / AmpDb) * Math.Sign(sample));
        }
    }
}
