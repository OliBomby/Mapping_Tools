using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Application.Audio.Contracts;

/// <summary>Coordinates decoding and spectrum calculation for preview consumers.</summary>
public interface ISpectrumService
{
    /// <summary>Decodes an audio file and calculates its spectrum.</summary>
    /// <param name="request">The source audio request.</param>
    /// <param name="options">FFT and source-window options.</param>
    /// <param name="cancellationToken">Token shared by decoding and calculation.</param>
    /// <returns>The calculated spectrum frame.</returns>
    Task<SpectrumFrame> CalculateFileAsync(
        AudioDecodeRequest request,
        SpectrumCalculationOptions? options = null,
        CancellationToken cancellationToken = default);
}
