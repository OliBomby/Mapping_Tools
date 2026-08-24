using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Application.Audio;

/// <summary>Calculates framework-neutral magnitude bins for a decoded audio clip.</summary>
public interface ISpectrumCalculator
{
    /// <summary>Calculates one spectrum frame.</summary>
    /// <param name="clip">The decoded source clip.</param>
    /// <param name="options">FFT and source-window options.</param>
    /// <param name="cancellationToken">Token checked before and during calculation.</param>
    /// <returns>A spectrum frame, including an empty frame for empty input.</returns>
    Task<SpectrumFrame> CalculateAsync(
        AudioClip clip,
        SpectrumCalculationOptions? options = null,
        CancellationToken cancellationToken = default);
}

