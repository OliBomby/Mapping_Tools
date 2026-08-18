using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Application.Audio;

/// <summary>Controls the FFT window used by a spectrum calculation.</summary>
public sealed class SpectrumCalculationOptions
{
    /// <summary>Gets or sets the power-of-two FFT size.</summary>
    public int FftSize { get; set; } = 1024;

    /// <summary>Gets or sets the starting frame offset in the source clip.</summary>
    public int StartFrame { get; set; }

    /// <summary>Gets or sets the number of source frames to inspect; zero uses the available clip.</summary>
    public int FrameCount { get; set; }
}

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
