using Mapping_Tools.Core.Audio;

namespace Mapping_Tools.Core.Audio.Effects;

/// <summary>Processes an audio clip without depending on an audio framework.</summary>
public abstract class AudioEffect
{
    /// <summary>
    ///     Applies this effect and returns a new clip, leaving <paramref name="source" /> unchanged.
    /// </summary>
    /// <param name="source">The clip to process.</param>
    /// <param name="cancellationToken">Token checked while processing samples.</param>
    /// <returns>A newly allocated processed clip.</returns>
    public AudioClip Apply(AudioClip source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        float[] samples = source.CopySamples();
        ApplyCore(samples, source.Format, cancellationToken);
        return new AudioClip(source.Format, samples);
    }

    /// <summary>Applies the effect to a mutable copy of the source samples.</summary>
    /// <param name="samples">The interleaved samples to modify.</param>
    /// <param name="format">The sample rate and channel layout of <paramref name="samples" />.</param>
    /// <param name="cancellationToken">Token checked while processing samples.</param>
    protected abstract void ApplyCore(
        float[] samples,
        AudioFormat format,
        CancellationToken cancellationToken);
}
