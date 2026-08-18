using Mapping_Tools.Core.Audio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Audio;

[TestClass]
public sealed class AudioBoundaryTests
{
    [TestMethod]
    public void AudioClip_CopiesInputSamplesAndPreservesFormat()
    {
        // Arrange
        float[] samples = [0.25f, -0.5f, 0.75f, -1f];
        var format = new AudioFormat(1000, 2);

        // Act
        var clip = new AudioClip(format, samples);
        samples[0] = 99;

        // Assert
        clip.CopySamples().Should().Equal(0.25f, -0.5f, 0.75f, -1f);
        clip.FrameCount.Should().Be(2);
        clip.Duration.Should().Be(TimeSpan.FromMilliseconds(2));
    }

    [TestMethod]
    public void AudioVolume_RoundTripsValuesAcrossTheLowVolumeKnee()
    {
        // Arrange
        double[] volumes = [0, 0.01, 0.05, 0.5, 1];

        // Act
        double[] roundTrips = volumes.Select(volume => AudioVolume.FromAmplitude(AudioVolume.ToAmplitude(volume))).ToArray();

        // Assert
        for (int index = 0; index < volumes.Length; index++)
        {
            roundTrips[index].Should().BeApproximately(volumes[index], 1e-12);
        }
    }

    [TestMethod]
    public void AudioEffectEngine_DelayFadeOutLeavesDelayThenReachesSilence()
    {
        // Arrange
        var source = new AudioClip(new AudioFormat(1000, 1), Enumerable.Repeat(1f, 6));
        AudioEffect effect = AudioEffect.CreateDelayFadeOut(2, 2);

        // Act
        AudioClip result = AudioEffectEngine.Apply(source, [effect]);

        // Assert
        result.CopySamples().Should().Equal(1f, 1f, 1f, 0.5f, 0f, 0f);
        source.CopySamples().Should().AllSatisfy(sample => sample.Should().Be(1f));
    }

    [TestMethod]
    public void AudioEffectEngine_SoftLimiterProducesFiniteSamples()
    {
        // Arrange
        var source = new AudioClip(new AudioFormat(44100, 1), [0f, 0.25f, 1f, -2f]);

        // Act
        AudioClip result = AudioEffectEngine.Apply(
            source,
            [AudioEffect.CreateSoftLimiter(boostDecibels: 6, brickwallDecibels: -0.1)]);

        // Assert
        result.CopySamples().Should().AllSatisfy(sample => float.IsFinite(sample).Should().BeTrue());
        result.CopySamples().Should().NotEqual(source.CopySamples());
    }

    [TestMethod]
    public void AudioEffectEngine_CancellationStopsProcessing()
    {
        // Arrange
        var source = new AudioClip(new AudioFormat(44100, 1), Enumerable.Repeat(1f, 1024));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Action act = () => AudioEffectEngine.Apply(
            source,
            [AudioEffect.CreateSoftLimiter()],
            cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
    }

    [TestMethod]
    public void SpectrumFrame_DoesNotExposeMutableMagnitudeStorage()
    {
        // Arrange
        double[] magnitudes = [1, 2];
        var frame = new Mapping_Tools.Core.Spectrum.SpectrumFrame(44100, 4, magnitudes);

        // Act
        magnitudes[0] = 99;

        // Assert
        frame.Magnitudes[0].Should().Be(1);
        frame.Magnitudes.Should().NotBeAssignableTo<double[]>();
    }
}
