using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

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
            Times = [1000]
        };
        HitsoundLayer whistle = new("whistle", SampleSet.Drum, Hitsound.Whistle,
            new SampleGeneratingArgs("whistle.wav"), new LayerImportArgs())
        {
            Times = [1014]
        };
        Sample fallback = new(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 99, 1);

        // Act
        IReadOnlyList<SamplePackage> packages = new HitsoundStudioEngine().ZipLayers(
            [normal, whistle], fallback, leniency: 15);

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
            Times = [1000, 1001]
        };
        Sample fallback = new(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 99, 1);

        // Act
        IReadOnlyList<SamplePackage> packages = new HitsoundStudioEngine().ZipLayers(
            [whistle], fallback, leniency: 0, needNormalSample: false);

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
            Times = [500]
        };
        HitsoundStudioEngine engine = new();
        IReadOnlyList<SamplePackage> packages = engine.ZipLayers(
            [layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("kick.wav"), 0, 1));
        SampleSchema previous = new()
        {
            ["normal-hitnormal"] = [new SampleGeneratingArgs("kick.wav")]
        };

        // Act
        HitsoundStudioStandardResult result = engine.BuildStandard(
            packages,
            previous,
            allowGrowth: false,
            firstCustomIndex: 1,
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
            Times = [100]
        };
        HitsoundStudioEngine engine = new();
        IReadOnlyList<SamplePackage> packages = engine.ZipLayers(
            [layer],
            new Sample(SampleSet.Normal, Hitsound.Normal, new SampleGeneratingArgs("fallback.wav"), 0, 1),
            needNormalSample: false);

        // Act
        HitsoundStudioNamedResult result = engine.BuildNamed(
            packages,
            previousSchema: null,
            maniaPositions: false,
            includeRegularHitsounds: true,
            allowGrowth: false,
            sample => false);

        // Assert
        result.Events.Should().ContainSingle();
        result.Events[0].Filename.Should().BeEmpty();
        result.Names.Values.Should().ContainSingle().Which.Should().BeEmpty();
    }

    [TestMethod]
    public void GenerateManiaPositions_ClampsKeyCountToOsuMaximum()
    {
        // Arrange
        SampleGeneratingArgs[] samples = Enumerable.Range(0, 24)
            .Select(index => new SampleGeneratingArgs($"sample-{index}.wav"))
            .ToArray();

        // Act
        Dictionary<SampleGeneratingArgs, Mapping_Tools.Core.Classes.MathUtil.Vector2> positions =
            new HitsoundStudioEngine().GenerateManiaPositions(samples);

        // Assert
        positions.Should().HaveCount(24);
        positions.Values.Should().OnlyContain(position => position.Y == 192);
    }
}
