using Mapping_Tools.Core.Audio.Effects;

namespace Mapping_Tools.Core.Audio;

/// <summary>Applies framework-neutral effects to an owned audio clip.</summary>
public static class AudioEffectEngine
{
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

        AudioClip result = source;
        bool appliedEffect = false;
        foreach (var effect in effects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(effect);
            result = effect.Apply(result, cancellationToken);
            appliedEffect = true;
        }

        return appliedEffect ? result : new AudioClip(source.Format, source.CopySamples());
    }
}
