using Mapping_Tools.Core.Audio;

namespace Mapping_Tools.Core.Audio.Effects;

/// <summary>Leaves a clip unchanged for a delay and then linearly fades it to silence.</summary>
public sealed class DelayFadeOutEffect : AudioEffect
{
    /// <summary>Creates a delayed fade-out effect.</summary>
    /// <param name="delay">Time in milliseconds before fading starts.</param>
    /// <param name="duration">Fade duration in milliseconds.</param>
    public DelayFadeOutEffect(double delay, double duration)
    {
        if (!double.IsFinite(delay) || delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));

        if (!double.IsFinite(duration) || duration < 0) throw new ArgumentOutOfRangeException(nameof(duration));

        Delay = delay;
        Duration = duration;
    }

    /// <summary>Gets the time in milliseconds before fading starts.</summary>
    public double Delay { get; }

    /// <summary>Gets the fade duration in milliseconds.</summary>
    public double Duration { get; }

    /// <inheritdoc />
    protected override void ApplyCore(
        float[] samples,
        AudioFormat format,
        CancellationToken cancellationToken)
    {
        int delayFrames = (int)Math.Round(Delay * format.SampleRate / 1000, MidpointRounding.ToEven);
        int durationFrames = Math.Max(1, (int)Math.Round(Duration * format.SampleRate / 1000, MidpointRounding.ToEven));
        for (int frame = delayFrames; frame < samples.Length / format.Channels; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double progress = (double)(frame - delayFrames) / durationFrames;
            float multiplier = (float)Math.Clamp(1 - progress, 0, 1);
            for (int channel = 0; channel < format.Channels; channel++) samples[frame * format.Channels + channel] *= multiplier;
        }
    }
}
