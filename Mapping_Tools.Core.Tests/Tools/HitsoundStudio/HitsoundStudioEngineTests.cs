using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Mapping_Tools.Core.Tools.HitsoundStudio.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.HitsoundStudio;

[TestClass]
public sealed class HitsoundStudioEngineTests
{
    [TestMethod]
    public void ZipLayers_WithLegacyLeniency_AddsEventsToOnePackageAndCopiesDefault()
    {
        // Arrange
        HitsoundLayer normal = new("normal", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("normal.wav"), new LayerImportArgs())
        {
            Times = [1000],
        };
        HitsoundLayer whistle = new("whistle", SampleSet.Drum, Hitsound.Whistle,
            new SampleGeneratingArgs("whistle.wav"), new LayerImportArgs())
        {
            Times = [1014],
        };
        Sample fallback = new(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 99, 1);

        // Act
        var packages = new HitsoundStudioEngine().ZipLayers(
            [normal, whistle], fallback);

        // Assert
        packages.Should().ContainSingle();
        packages[0].Samples.Should().HaveCount(2);
        packages[0].Time.Should().Be(1000);
    }

    [TestMethod]
    public void ZipLayers_WithZeroLeniency_DoesNotAddNormalFallbackOrMergeNearbyEvents()
    {
        // Arrange
        HitsoundLayer whistle = new("whistle", SampleSet.Drum, Hitsound.Whistle,
            new SampleGeneratingArgs("whistle.wav"), new LayerImportArgs())
        {
            Times = [1000, 1001],
        };
        Sample fallback = new(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 99, 1);

        // Act
        var packages = new HitsoundStudioEngine().ZipLayers(
            [whistle], fallback, 0, false);

        // Assert
        packages.Should().HaveCount(2);
        packages.SelectMany(package => package.Samples)
            .Should().OnlyContain(sample => sample.SampleArgs.Path == "whistle.wav");
    }

    [TestMethod]
    public void BuildStandard_WithPreviousSchemaAndGrowthDisabled_ReusesExistingIndex()
    {
        // Arrange
        HitsoundLayer layer = new("kick", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("kick.wav"), new LayerImportArgs())
        {
            Times = [500],
        };
        HitsoundStudioEngine engine = new();
        var packages = engine.ZipLayers(
            [layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("kick.wav"), 0, 1));
        SampleSchema previous = new()
        {
            ["normal-hitnormal"] = [new SampleGeneratingArgs("kick.wav")],
        };

        // Act
        var result = engine.BuildStandard(
            packages,
            previous,
            false,
            1,
            sample => !string.IsNullOrEmpty(sample.Path));

        // Assert
        result.Events.Should().ContainSingle();
        result.Events[0].CustomIndex.Should().Be(1);
        result.Schema["normal-hitnormal"].Single().Path.Should().Be("kick.wav");
    }

    [TestMethod]
    public void BuildNamed_WithInvalidSource_LeavesFilenameEmptyWithoutThrowing()
    {
        // Arrange
        HitsoundLayer layer = new("missing", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("missing.wav"), new LayerImportArgs())
        {
            Times = [100],
        };
        HitsoundStudioEngine engine = new();
        var packages = engine.ZipLayers(
            [layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 0, 1),
            needNormalSample: false);

        // Act
        var result = engine.BuildNamed(
            packages,
            null,
            false,
            true,
            false,
            _ => false);

        // Assert
        result.Events.Should().ContainSingle();
        result.Events[0].Filename.Should().BeEmpty();
        result.Names.Values.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [TestMethod]
    public void GenerateManiaPositions_ClampsKeyCountToOsuMaximum()
    {
        // Arrange
        var samples = Enumerable.Range(0, 24)
            .Select(index => new SampleGeneratingArgs($"sample-{index}.wav"))
            .ToArray();

        // Act
        var positions =
            new HitsoundStudioEngine().GenerateManiaPositions(samples);

        // Assert
        positions.Should().HaveCount(24);
        positions.Values.Should().OnlyContain(position => Precision.AlmostEquals(position.Y, 192));
    }

    [TestMethod]
    public void BalanceVolumes_WithIndividualVolume_MovesSampleGainToEventVolume()
    {
        // Arrange
        SampleGeneratingArgs args = new("kick.wav") { Volume = 0.5 };
        Sample sample = new(SampleSet.Normal, Hitsound.Normal, args, 0, 0.8);
        SamplePackage package = new(1000, [sample]);
        double originalAmplitude = AudioVolume.ToAmplitude(0.8) * AudioVolume.ToAmplitude(0.5);

        // Act
        new HitsoundStudioEngine().BalanceVolumes([package], 0, false, individualVolume: true);

        // Assert
        sample.SampleArgs.Volume.Should().Be(1);
        AudioVolume.ToAmplitude(sample.OutsideVolume).Should().BeApproximately(originalAmplitude, 0.000001);
    }

    [TestMethod]
    public void BuildStandard_WithIncompatiblePreviousSchemaAndGrowthDisabled_RejectsPackage()
    {
        // Arrange
        HitsoundStudioEngine engine = new();
        HitsoundLayer layer = new("kick", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("new-kick.wav"), new LayerImportArgs()) { Times = [500] };
        var packages = engine.ZipLayers([layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 0, 1));
        SampleSchema previous = new()
        {
            ["normal-hitnormal"] = [new SampleGeneratingArgs("old-kick.wav")],
        };

        // Act
        var act = () => engine.BuildStandard(packages, previous, false, 1, _ => true);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*can't fit the sample packages*");
    }

    [TestMethod]
    public void BuildNamed_WithIncompatiblePreviousSchemaAndGrowthDisabled_RejectsNewSample()
    {
        // Arrange
        HitsoundStudioEngine engine = new();
        HitsoundLayer layer = new("kick", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("new-kick.wav"), new LayerImportArgs()) { Times = [500] };
        var packages = engine.ZipLayers([layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 0, 1));
        SampleSchema previous = new()
        {
            ["normal-hitnormal"] = [new SampleGeneratingArgs("old-kick.wav")],
        };

        // Act
        var act = () => engine.BuildNamed(packages, previous, false, true, false, _ => true);

        // Assert
        act.Should().Throw<InvalidDataException>().WithMessage("*growth is disabled*");
    }

    [TestMethod]
    public void BuildStandardTimingPoints_AddsEventIndexAndScalesActiveVolume()
    {
        // Arrange
        TimingPoint redline = new(0, 500, 4, SampleSet.Normal, 0, 100, true, false, false);
        TimingPoint bodySample = new(500, -100, 4, SampleSet.Drum, 3, 80, false, false, false);
        Timing timing = new([redline, bodySample], 1.4);
        HitsoundEvent item = new(1000, 0.5, SampleSet.Drum, SampleSet.Soft, 12, true, false, false);

        // Act
        IReadOnlyList<TimingPoint> points = new HitsoundStudioEngine()
            .BuildStandardTimingPoints(timing, [item]);

        // Assert
        TimingPoint eventPoint = points.Single(point => point.Offset == 1000);
        eventPoint.Uninherited.Should().BeFalse();
        eventPoint.SampleSet.Should().Be(SampleSet.Normal);
        eventPoint.SampleIndex.Should().Be(12);
        eventPoint.Volume.Should().Be(40);
        points.Should().ContainSingle(point => point.Uninherited && point.Offset == 0);
    }

    [TestMethod]
    public void BalanceVolumes_WithoutIndividualMode_PreservesCombinedSampleAndEventAmplitude()
    {
        // Arrange
        Sample quiet = new(SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("quiet.wav") { Volume = 0.5 }, 0, 0.8);
        Sample loud = new(SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("loud.wav") { Volume = 1 }, 0, 0.6);
        SamplePackage package = new(1000, [quiet, loud]);
        double[] originalAmplitudes = new[] { quiet, loud }
            .Select(sample => AudioVolume.ToAmplitude(sample.SampleArgs.Volume)
                * AudioVolume.ToAmplitude(sample.OutsideVolume))
            .ToArray();

        // Act
        new HitsoundStudioEngine().BalanceVolumes([package], 0, alwaysFullVolume: false);

        // Assert
        quiet.SampleArgs.Volume.Should().BeLessThan(1);
        loud.SampleArgs.Volume.Should().BeLessThan(1);
        new[] { quiet, loud }
            .Select(sample => AudioVolume.ToAmplitude(sample.SampleArgs.Volume)
                * AudioVolume.ToAmplitude(sample.OutsideVolume))
            .Should().Equal(originalAmplitudes);
    }

    [TestMethod]
    public void BuildNamed_WhenDifferentSourcesShareFilename_AssignsDistinctStableNames()
    {
        // Arrange
        SampleGeneratingArgs firstArgs = new(@"C:\source-a\kick.wav");
        SampleGeneratingArgs secondArgs = new(@"C:\source-b\kick.wav");
        SamplePackage firstPackage = new(100, [
            new Sample(SampleSet.Normal, Hitsound.Normal, firstArgs, 0, 1),
        ]);
        SamplePackage secondPackage = new(200, [
            new Sample(SampleSet.Normal, Hitsound.Normal, secondArgs, 0, 1),
        ]);

        // Act
        HitsoundStudioNamedResult result = new HitsoundStudioEngine().BuildNamed(
            [firstPackage, secondPackage],
            null,
            maniaPositions: false,
            includeRegularHitsounds: false,
            allowGrowth: true,
            _ => true);

        // Assert
        result.Names.Values.Should().HaveCount(2).And.OnlyHaveUniqueItems();
        result.Events.Select(item => item.Filename).Should().Equal(result.Names[firstArgs], result.Names[secondArgs]);
        result.Names.Values.Should().Contain(name => name.EndsWith("-2", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratePositions_WhenGridNeedsMoreRows_KeepsEverySampleInsidePlayfield()
    {
        // Arrange
        SampleGeneratingArgs[] samples = Enumerable.Range(0, 21)
            .Select(index => new SampleGeneratingArgs($"sample-{index}.wav"))
            .ToArray();

        // Act
        Dictionary<SampleGeneratingArgs, Vector2> positions =
            new HitsoundStudioEngine().GeneratePositions(samples);

        // Assert
        positions.Should().HaveCount(samples.Length);
        positions.Values.Should().OnlyHaveUniqueItems();
        positions.Values.Should().OnlyContain(position =>
            position.X >= 0 && position.X <= 512
            && position.Y >= 0 && position.Y <= 384);
    }
}
