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
    public void SampleGeneratingArgs_VelocityChangedFromFullVolumeToNegativeOne_UsesInvariantVolume()
    {
        // Arrange
        SampleGeneratingArgs sample = new("sample.wav") { Volume = 1 };

        // Act
        sample.Velocity = -1;

        // Assert
        sample.Volume.Should().Be(-0.01d);
    }

    [TestMethod]
    public void SampleGeneratingArgs_VelocityReadWithInvariantVolume_ReturnsNegativeOne()
    {
        // Arrange
        SampleGeneratingArgs sample = new("sample.wav") { Volume = -0.01 };

        // Act
        int velocity = sample.Velocity;

        // Assert
        velocity.Should().Be(-1);
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
    public void ReloadCompatible_WithStackWildcard_IsDirectionalAndRequiresMatchingPath()
    {
        // Arrange
        LayerImportArgs wildcard = new(ImportType.Stack) { Path = "map.osu", X = -1, Y = 192 };
        LayerImportArgs specific = new(ImportType.Stack) { Path = "map.osu", X = 256, Y = 192 };
        LayerImportArgs otherPath = new(ImportType.Stack) { Path = "other.osu", X = 256, Y = 192 };

        // Act
        bool wildcardAcceptsSpecific = wildcard.ReloadCompatible(specific);
        bool specificAcceptsWildcard = specific.ReloadCompatible(wildcard);
        bool wildcardAcceptsOtherPath = wildcard.ReloadCompatible(otherPath);

        // Assert
        wildcardAcceptsSpecific.Should().BeTrue();
        specificAcceptsWildcard.Should().BeFalse();
        wildcardAcceptsOtherPath.Should().BeFalse();
    }

    [TestMethod]
    public void ReloadCompatible_WithHitsounds_RequiresSampleAndOptionalVolumeMatch()
    {
        // Arrange
        LayerImportArgs layer = new(ImportType.Hitsounds)
        {
            Path = "map.osu", SamplePath = "sample.wav", Volume = 0.5,
        };
        LayerImportArgs otherVolume = new(ImportType.Hitsounds)
        {
            Path = "map.osu", SamplePath = "sample.wav", Volume = 0.8,
        };
        LayerImportArgs otherSample = new(ImportType.Hitsounds)
        {
            Path = "map.osu", SamplePath = "other.wav", Volume = 0.5,
        };

        // Act
        bool ignoresVolume = layer.ReloadCompatible(otherVolume);
        bool rejectsSample = layer.ReloadCompatible(otherSample);
        layer.DiscriminateVolumes = true;
        bool rejectsVolume = layer.ReloadCompatible(otherVolume);

        // Assert
        ignoresVolume.Should().BeTrue();
        rejectsSample.Should().BeFalse();
        rejectsVolume.Should().BeFalse();
    }

    [TestMethod]
    public void ReloadCompatible_WithMidiSelectors_RequiresSpecifiedValuesOnly()
    {
        // Arrange
        LayerImportArgs wildcard = new(ImportType.MIDI)
        {
            Path = "notes.mid", Bank = -1, Patch = 2, Key = -1, Length = 500, Velocity = -1,
        };
        LayerImportArgs matching = new(ImportType.MIDI)
        {
            Path = "notes.mid", Bank = 3, Patch = 2, Key = 60, Length = 500, Velocity = 80,
        };
        LayerImportArgs otherLength = new(ImportType.MIDI)
        {
            Path = "notes.mid", Bank = 3, Patch = 2, Key = 60, Length = 501, Velocity = 80,
        };

        // Act
        bool compatible = wildcard.ReloadCompatible(matching);
        bool differentLengthCompatible = wildcard.ReloadCompatible(otherLength);
        bool reverseCompatible = matching.ReloadCompatible(wildcard);

        // Assert
        compatible.Should().BeTrue();
        differentLengthCompatible.Should().BeFalse();
        reverseCompatible.Should().BeFalse();
    }

    [TestMethod]
    public void ReloadCompatible_WithDifferentImportTypes_RejectsEvenWildcardNone()
    {
        // Arrange
        LayerImportArgs none = new(ImportType.None);
        LayerImportArgs midi = new(ImportType.MIDI);
        LayerImportArgs anotherNone = new(ImportType.None) { Path = "different" };

        // Act
        bool differentType = none.ReloadCompatible(midi);
        bool bothNone = none.ReloadCompatible(anotherNone);

        // Assert
        differentType.Should().BeFalse();
        bothNone.Should().BeTrue();
    }

    [TestMethod]
    public void ReloadCompatible_WithMidiSelectors_RejectsEachSpecifiedSelectorMismatch()
    {
        // Arrange
        LayerImportArgs selected = new(ImportType.MIDI)
        {
            Path = "notes.mid", Bank = 1, Patch = 2, Key = 60, Length = 500, Velocity = 80,
        };
        LayerImportArgs[] changed =
        [
            new(ImportType.MIDI) { Path = "other.mid", Bank = 1, Patch = 2, Key = 60, Length = 500, Velocity = 80 },
            new(ImportType.MIDI) { Path = "notes.mid", Bank = 3, Patch = 2, Key = 60, Length = 500, Velocity = 80 },
            new(ImportType.MIDI) { Path = "notes.mid", Bank = 1, Patch = 3, Key = 60, Length = 500, Velocity = 80 },
            new(ImportType.MIDI) { Path = "notes.mid", Bank = 1, Patch = 2, Key = 61, Length = 500, Velocity = 80 },
            new(ImportType.MIDI) { Path = "notes.mid", Bank = 1, Patch = 2, Key = 60, Length = 500, Velocity = 81 },
        ];

        // Act
        bool[] compatible = changed.Select(selected.ReloadCompatible).ToArray();

        // Assert
        compatible.Should().OnlyContain(value => !value);
    }

    [TestMethod]
    public void ReloadCompatible_WithDiscriminatedVolumeAtToleranceBoundary_RejectsDifference()
    {
        // Arrange
        LayerImportArgs layer = new(ImportType.Hitsounds)
        {
            Path = "map.osu", SamplePath = "sample.wav", Volume = 0,
            DiscriminateVolumes = true,
        };
        LayerImportArgs other = new(ImportType.Hitsounds)
        {
            Path = "map.osu", SamplePath = "sample.wav", Volume = Precision.DOUBLE_EPSILON,
        };

        // Act
        bool compatible = layer.ReloadCompatible(other);

        // Assert
        compatible.Should().BeFalse();
    }

    [TestMethod]
    public void ReloadCompatible_WithDefaultSelectors_AcceptsAnyMidiNote()
    {
        // Arrange
        LayerImportArgs wildcard = new(ImportType.MIDI) { Path = "notes.mid" };
        LayerImportArgs specific = new(ImportType.MIDI)
        {
            Path = "notes.mid", Bank = 1, Patch = 2, Key = 60, Length = 500, Velocity = 80,
        };

        // Act
        bool compatible = wildcard.ReloadCompatible(specific);

        // Assert
        compatible.Should().BeTrue();
        wildcard.Bank.Should().Be(-1);
        wildcard.Patch.Should().Be(-1);
        wildcard.Key.Should().Be(-1);
        wildcard.Length.Should().Be(-1);
        wildcard.Velocity.Should().Be(-1);
    }

    [TestMethod]
    public void HitsoundLayer_RemoveDuplicatesUsesDomainPrecision()
    {
        // Arrange
        var layer = new HitsoundLayer
        {
            Times = [1000, 1000, 1250, 1250],
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
            ["normal-hitnormal3"] = [sample],
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
        customIndex.Samples["normal-hitnormal"].ToArray().Should().BeEquivalentTo([valid]);
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
