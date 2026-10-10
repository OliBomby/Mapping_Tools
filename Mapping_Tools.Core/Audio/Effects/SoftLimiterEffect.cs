namespace Mapping_Tools.Core.Audio.Effects;

/// <summary>Applies the legacy soft clipper and limiter transfer curve.</summary>
public sealed class SoftLimiterEffect : AudioEffect
{
    private const double amp_db = 8.6562;
    private const double baseline_threshold_db = -9;
    private const double a = 1.017;
    private const double b = -0.025;

    /// <summary>Creates a soft limiter effect.</summary>
    /// <param name="boostDecibels">Input boost in decibels. Must be between 0 and 18.</param>
    /// <param name="brickwallDecibels">Output ceiling in decibels. Must be between -3 and 1.</param>
    public SoftLimiterEffect(double boostDecibels = 0, double brickwallDecibels = -0.1)
    {
        if (!double.IsFinite(boostDecibels) || boostDecibels is < 0 or > 18) throw new ArgumentOutOfRangeException(nameof(boostDecibels));

        if (!double.IsFinite(brickwallDecibels) || brickwallDecibels is < -3 or > 1) throw new ArgumentOutOfRangeException(nameof(brickwallDecibels));

        BoostDecibels = boostDecibels;
        BrickwallDecibels = brickwallDecibels;
    }

    /// <summary>Gets the input boost in decibels.</summary>
    public double BoostDecibels { get; }

    /// <summary>Gets the output ceiling in decibels.</summary>
    public double BrickwallDecibels { get; }

    /// <inheritdoc />
    protected override void ApplyCore(
        float[] samples,
        AudioFormat _,
        CancellationToken cancellationToken)
    {
        double threshold = baseline_threshold_db + BrickwallDecibels;
        for (int index = 0; index < samples.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float sample = samples[index];
            if (sample == 0) continue;

            double decibels = amp_db * Math.Log(Math.Abs(sample)) + BoostDecibels;
            if (decibels > threshold)
            {
                double over = decibels - threshold;
                over = a * over + b * over * over;
                decibels = Math.Min(threshold + over, BrickwallDecibels);
            }

            samples[index] = (float)(Math.Exp(decibels / amp_db) * Math.Sign(sample));
        }
    }
}
