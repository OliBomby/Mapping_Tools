using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundStudio;

[TestClass]
public sealed class HitsoundStudioProjectTests
{
    [TestMethod]
    public void Clone_CopiesNestedLayersAndSchemaWithoutSharingMutableState()
    {
        // Arrange
        HitsoundLayer layer = new("layer", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("kick.wav"), new LayerImportArgs())
        {
            Times = [100, 200],
        };
        HitsoundStudioProject project = new()
        {
            BaseBeatmap = "base.osu",
            HitsoundLayers = [layer],
            PreviousSampleSchema = new SampleSchema
            {
                ["normal-hitnormal"] = [new SampleGeneratingArgs("kick.wav")],
            },
        };

        // Act
        var clone = project.Clone();
        clone.HitsoundLayers[0].Times.Add(300);
        clone.PreviousSampleSchema!["normal-hitnormal"][0].Path = "other.wav";

        // Assert
        project.HitsoundLayers[0].Times.Should().Equal(100, 200);
        project.PreviousSampleSchema["normal-hitnormal"][0].Path.Should().Be("kick.wav");
        clone.HitsoundLayers[0].Should().NotBeSameAs(layer);
    }

    [TestMethod]
    public void ImportRequest_DefaultsToLegacyMIDIIdentityFields()
    {
        // Arrange
        HitsoundStudioImportRequest request = new();

        // Act
        bool discriminatesLengths = request.DiscriminateLengths;
        bool discriminatesVelocities = request.DiscriminateVelocities;

        // Assert
        discriminatesLengths.Should().BeFalse();
        discriminatesVelocities.Should().BeFalse();
    }
}
