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

