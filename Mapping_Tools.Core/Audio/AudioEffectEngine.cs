using Mapping_Tools.Core.Audio.Effects;

namespace Mapping_Tools.Core.Audio;

/// <summary>Applies framework-neutral effects to an owned audio clip.</summary>
public static class AudioEffectEngine
{
    private const double amp_db = 8.6562;
    private const double baseline_threshold_db = -9;
    private const double a = 1.017;
    private const double b = -0.025;

    /// <summary>
    ///     Applies effects in order and returns a new clip, leaving the source unchanged.
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
        foreach (var effect in effects)
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
            for (int channel = 0; channel < format.Channels; channel++) samples[frame * format.Channels + channel] *= multiplier;
        }
    }

    private static void ApplySoftLimiter(
        float[] samples,
        double boost,
        double brickwall,
        CancellationToken cancellationToken)
    {
        double threshold = baseline_threshold_db + brickwall;
        for (int index = 0; index < samples.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float sample = samples[index];
            if (sample == 0) continue;

            double decibels = amp_db * Math.Log(Math.Abs(sample)) + boost;
            if (decibels > threshold)
            {
                double over = decibels - threshold;
                over = a * over + b * over * over;
                decibels = Math.Min(threshold + over, brickwall);
            }

            samples[index] = (float)(Math.Exp(decibels / amp_db) * Math.Sign(sample));
        }
    }
}
