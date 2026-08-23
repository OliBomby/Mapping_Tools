using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.HitsoundStuff;

[TestClass]
public class HitsoundDomainTests
{
    [TestMethod]
    public void HitsoundFilename_ParsesStandardSampleName()
    {
        // Arrange
        // Act
        const string filename = "drum-hitclap12";

        // Assert
        HitsoundFilename.GetSampleSet(filename).Should().Be(SampleSet.Drum);
        HitsoundFilename.GetHitsound(filename).Should().Be(Hitsound.Clap);
        HitsoundFilename.GetIndex(filename).Should().Be(12);
    }

    [TestMethod]
    public void SampleGeneratingArgs_CopyPreservesGenerationSettings()
    {
        // Arrange
        var source = new SampleGeneratingArgs(
            "samples/piano.sf2",
            0.75,
            -0.2,
            0.1,
            2,
            3,
            4,
            60,
            500);

        // Act
        var copy = source.Copy();

        // Assert
        copy.Should().Be(source);
        copy.Should().NotBeSameAs(source);
        copy.UsesSoundFont.Should().BeTrue();
        copy.GetFilename().Should().Contain("piano");
    }

    [TestMethod]
    public void HitsoundZone_DistanceHonoursWildcardAxesAndCopyIsIndependent()
    {
        // Arrange
        // Act
        var zone = new HitsoundZone(
            "centre line", "normal-hitnormal.wav",
            -1, 100,
            Hitsound.Normal, SampleSet.Normal, SampleSet.None, 1);

        // Assert
        zone.Distance(new Vector2(400, 80)).Should().BeApproximately(20, 0.0001);

        var copy = zone.Copy();
        copy.YPos = 120;

        zone.YPos.Should().Be(100);
        copy.YPos.Should().Be(120);
    }

    [TestMethod]
    public void LayerImportArgs_ExposesImportAndReloadRules()
    {
        // Arrange
        // Act
        var stack = new LayerImportArgs(ImportType.Stack)
        {
            Path = "map.osu",
            X = -1,
            Y = 192,
        };
        var matchingStack = new LayerImportArgs(ImportType.Stack)
        {
            Path = "map.osu",
            X = 256,
            Y = 192,
        };

        // Assert
        stack.ImportType.Should().Be(ImportType.Stack);
        stack.ReloadCompatible(matchingStack).Should().BeTrue();
    }

    [TestMethod]
    public void HitsoundLayer_RemoveDuplicatesUsesDomainPrecision()
    {
        // Arrange
        var layer = new HitsoundLayer
        {
            Times = new List<double> { 1000, 1000, 1250, 1250 },
        };

        // Act
        layer.RemoveDuplicates();

        // Assert
        layer.Times.Should().Equal(new List<double> { 1000, 1250 });
    }

    [TestMethod]
    public void SampleSchema_RoundTripsCustomIndexAssignments()
    {
        // Arrange
        var sample = new SampleGeneratingArgs("kick.wav");
        var schema = new SampleSchema
        {
            ["normal-hitnormal3"] = new List<SampleGeneratingArgs> { sample },
        };

        // Act
        var indices = schema.GetCustomIndices();
        var restored = new SampleSchema(indices);

        // Assert
        indices.Count.Should().Be(1);
        indices[0].Index.Should().Be(3);
        indices[0].Samples["normal-hitnormal"].Contains(sample).Should().BeTrue();
        restored.ContainsKey("normal-hitnormal3").Should().BeTrue();
    }

    [TestMethod]
    public void CustomIndex_CleanInvalidsUsesCallerValidationPolicy()
    {
        // Arrange
        var valid = new SampleGeneratingArgs("valid.wav");
        var invalid = new SampleGeneratingArgs("invalid.wav");
        var customIndex = new CustomIndex(2);
        customIndex.Samples["normal-hitnormal"].Add(valid);
        customIndex.Samples["normal-hitnormal"].Add(invalid);

        // Act
        customIndex.CleanInvalids(sample => sample.Path == "valid.wav");

        // Assert
        customIndex.Samples["normal-hitnormal"].ToArray().Should().BeEquivalentTo(new[] { valid });
    }

    [TestMethod]
    public void HitsoundEvent_EncodesWhistleFinishAndClapBits()
    {
        // Arrange
        // Act
        var hitsound = new HitsoundEvent(
            1000, 1, SampleSet.Normal, SampleSet.Drum, 2,
            true, false, true);

        // Assert
        hitsound.GetHitsounds().Should().Be(10);
    }
}
