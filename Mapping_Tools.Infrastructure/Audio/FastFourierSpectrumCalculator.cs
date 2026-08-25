using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.Spectrum;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Calculates a Hann-windowed single-frame FFT without exposing a numerical/audio library.</summary>
public sealed class FastFourierSpectrumCalculator : ISpectrumCalculator
{
    /// <inheritdoc />
    public Task<SpectrumFrame> CalculateAsync(
        AudioClip clip,
        SpectrumCalculationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clip);
        options ??= new SpectrumCalculationOptions();
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();

        if (clip.IsEmpty) return Task.FromResult(new SpectrumFrame(clip.Format.SampleRate, options.FftSize, []));

        int start = Math.Clamp(options.StartFrame, 0, clip.FrameCount);
        int available = clip.FrameCount - start;
        int requested = options.FrameCount == 0 ? available : Math.Min(options.FrameCount, available);
        if (requested <= 0) return Task.FromResult(new SpectrumFrame(clip.Format.SampleRate, options.FftSize, []));

        return Task.FromResult(Calculate(clip, options.FftSize, start, requested, cancellationToken));
    }

    private static SpectrumFrame Calculate(
        AudioClip clip,
        int fftSize,
        int start,
        int requested,
        CancellationToken cancellationToken)
    {
        float[] source = clip.CopySamples();
        double[] real = new double[fftSize];
        double[] imaginary = new double[fftSize];
        int channels = clip.Format.Channels;
        for (int index = 0; index < fftSize; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int frame = start + index;
            double sample = 0;
            if (frame < start + requested)
            {
                int sampleOffset = frame * channels;
                for (int channel = 0; channel < channels; channel++) sample += source[sampleOffset + channel];

                sample /= channels;
            }

            double window = 0.5 * (1 - Math.Cos(2 * Math.PI * index / Math.Max(1, fftSize - 1)));
            real[index] = sample * window;
        }

        FourierTransform(real, imaginary, cancellationToken);
        double[] magnitudes = new double[fftSize / 2 + 1];
        for (int index = 0; index < magnitudes.Length; index++)
        {
            double scale = index == 0 || index == fftSize / 2 ? 1d / fftSize : 2d / fftSize;
            magnitudes[index] = Math.Sqrt(real[index] * real[index] + imaginary[index] * imaginary[index]) * scale;
        }

        return new SpectrumFrame(clip.Format.SampleRate, fftSize, magnitudes);
    }

    private static void FourierTransform(double[] real, double[] imaginary, CancellationToken cancellationToken)
    {
        int length = real.Length;
        for (int index = 1, reverse = 0; index < length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int bit = length >> 1;
            for (; (reverse & bit) != 0; bit >>= 1) reverse ^= bit;

            reverse ^= bit;
            if (index < reverse)
            {
                (real[index], real[reverse]) = (real[reverse], real[index]);
                (imaginary[index], imaginary[reverse]) = (imaginary[reverse], imaginary[index]);
            }
        }

        for (int size = 2; size <= length; size <<= 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
            double angle = -2 * Math.PI / size;
            double phaseReal = Math.Cos(angle);
            double phaseImaginary = Math.Sin(angle);
            for (int start = 0; start < length; start += size)
            {
                double currentReal = 1;
                double currentImaginary = 0;
                int half = size / 2;
                for (int offset = 0; offset < half; offset++)
                {
                    int even = start + offset;
                    int odd = even + half;
                    double temporaryReal = currentReal * real[odd] - currentImaginary * imaginary[odd];
                    double temporaryImaginary = currentReal * imaginary[odd] + currentImaginary * real[odd];
                    real[odd] = real[even] - temporaryReal;
                    imaginary[odd] = imaginary[even] - temporaryImaginary;
                    real[even] += temporaryReal;
                    imaginary[even] += temporaryImaginary;
                    (currentReal, currentImaginary) = (
                        currentReal * phaseReal - currentImaginary * phaseImaginary,
                        currentReal * phaseImaginary + currentImaginary * phaseReal);
                }
            }
        }
    }

    private static void Validate(SpectrumCalculationOptions options)
    {
        if (options.FftSize < 2 || options.FftSize > 1 << 20 || (options.FftSize & options.FftSize - 1) != 0)
            throw new ArgumentOutOfRangeException(nameof(options.FftSize), "FFT size must be a power of two between 2 and 1048576.");

        if (options.StartFrame < 0) throw new ArgumentOutOfRangeException(nameof(options.StartFrame));

        if (options.FrameCount < 0) throw new ArgumentOutOfRangeException(nameof(options.FrameCount));
    }
}
