namespace Mapping_Tools.Core.Spectrum;

/// <summary>Owns one calculated audio spectrum frame.</summary>
public sealed class SpectrumFrame
{
    /// <summary>Creates a spectrum frame from non-negative magnitude bins.</summary>
    /// <param name="sampleRate">The source sample rate.</param>
    /// <param name="fftSize">The FFT window size used for the calculation.</param>
    /// <param name="magnitudes">Magnitude values ordered from the lowest frequency upward.</param>
    public SpectrumFrame(int sampleRate, int fftSize, IEnumerable<double> magnitudes)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (fftSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fftSize));
        }

        ArgumentNullException.ThrowIfNull(magnitudes);
        double[] values = magnitudes.ToArray();
        if (values.Any(value => !double.IsFinite(value) || value < 0))
        {
            throw new ArgumentException("Spectrum magnitudes must be finite and non-negative.", nameof(magnitudes));
        }

        SampleRate = sampleRate;
        FftSize = fftSize;
        Magnitudes = Array.AsReadOnly(values);
        PeakMagnitude = values.Length == 0 ? 0 : values.Max();
    }

    /// <summary>Gets the source sample rate.</summary>
    public int SampleRate { get; }

    /// <summary>Gets the FFT window size.</summary>
    public int FftSize { get; }

    /// <summary>Gets the immutable magnitude bins.</summary>
    public IReadOnlyList<double> Magnitudes { get; }

    /// <summary>Gets the largest magnitude in the frame, or zero when empty.</summary>
    public double PeakMagnitude { get; }

    /// <summary>Gets whether the frame has no drawable bins.</summary>
    public bool IsEmpty => Magnitudes.Count == 0;
}
