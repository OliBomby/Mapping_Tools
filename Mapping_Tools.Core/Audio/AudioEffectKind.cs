namespace Mapping_Tools.Core.Audio;

/// <summary>Identifies a reusable audio effect supported by the hitsound pipeline.</summary>
public enum AudioEffectKind
{
    /// <summary>Delays and then fades the signal to silence.</summary>
    DelayFadeOut,

    /// <summary>Applies the legacy soft clipper/limiter transfer curve.</summary>
    SoftLimiter,
}

