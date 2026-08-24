using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Application.Tools.Sliderator;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TumourGenerating;
using Mapping_Tools.Core.Tools.TumourGenerating.Templates;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.MapsetMerger;

[TestClass]
public sealed class LegacyProjectJsonSerializerTests
{
    [TestMethod]
    public void Deserialize_WithLegacyHitsoundStudioRoot_RestoresSavedDataShape()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.HitsoundStudioVm, Mapping Tools",
                              "BaseBeatmap": "C:\\Maps\\base.osu",
                              "ExportFolder": "C:\\Export",
                              "HitsoundDiffName": "Hitsounds",
                              "ExportMap": true,
                              "ExportSamples": true,
                              "UsePreviousSampleSchema": false,
                              "HitsoundLayers": []
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<HitsoundStudioProject>(json);

        // Assert
        project.BaseBeatmap.Should().Be("C:\\Maps\\base.osu");
        project.ExportFolder.Should().Be("C:\\Export");
        project.HitsoundLayers.Should().BeEmpty();
    }

    [TestMethod]
    public void Serialize_WithHitsoundStudioProject_UsesLegacyRootName()
    {
        // Arrange
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(new HitsoundStudioProject());

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.HitsoundStudioVm, Mapping Tools");
    }

    [TestMethod]
    public void Deserialize_WithHitsoundStudioFixture_RestoresSchemaSettingsAndSampleMetadata()
    {
        // Arrange
        string json = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "hsstudioproject.json"));
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<HitsoundStudioProject>(json);

        // Assert
        project.ShowResults.Should().BeTrue();
        project.DeleteAllInExportFirst.Should().BeTrue();
        project.DefaultSample.SampleSet.Should().Be(SampleSet.Soft);
        project.PreviousSampleSchema.Should().ContainKey("normal-hitnormal2");
        project.PreviousSampleSchema!["normal-hitnormal2"].Should().ContainSingle();
    }

    [TestMethod]
    public void Deserialize_WithLegacyMapsetMergerDocument_RestoresProjectAndItems()
    {
        // Arrange
        const string json = """
                            {
                              "$type": "Mapping_Tools.Viewmodels.MapsetMergerVm, Mapping Tools",
                              "ExportPath": "C:\\Export",
                              "MoveSbToBeatmap": true,
                              "Mapsets": [
                                {
                                  "$type": "Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem, Mapping Tools",
                                  "Name": "Pack",
                                  "Path": "C:\\Pack"
                                }
                              ]
                            }
                            """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<MapsetMergerProject>(json);

        // Assert
        project.ExportPath.Should().Be("C:\\Export");
        project.MoveSbToBeatmap.Should().BeTrue();
        project.Mapsets.Should().ContainSingle();
        project.Mapsets[0].Name.Should().Be("Pack");
        project.Mapsets[0].Path.Should().Be("C:\\Pack");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroMapsetMergerFixture_RestoresBothMapsets()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "mapsetmergerproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<MapsetMergerProject>(File.ReadAllText(fixture));

        // Assert
        project.ExportPath.Should().Contain("Mapping Tools");
        project.MoveSbToBeatmap.Should().BeFalse();
        project.Mapsets.Should().HaveCount(2);
        project.Mapsets.Select(mapset => mapset.Name).Should().Equal(
            "1838134 seatrus - ILLEGAL LEGACY",
            "2146761 seatrus - ILLEGAL LEGACY");
    }

    [TestMethod]
    public void Serialize_WithMapsetMergerProject_UsesLegacyTypeNames()
    {
        // Arrange
        MapsetMergerProject project = new()
        {
            ExportPath = "C:\\Export",
            Mapsets = [new MapsetMergerProject.MapsetItem { Name = "Pack", Path = "C:\\Pack" }],
        };
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.MapsetMergerVm, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Viewmodels.MapsetMergerVm+MapsetItem, Mapping Tools");
    }

    [TestMethod]
    public void Deserialize_WithLegacyTumourGeneratorFixture_RestoresCoreLayersAndGraphs()
    {
        // Arrange
        string json = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "Projects",
                "tumourgeneratorproject.json"));
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<TumourGeneratorProject>(json);

        // Assert
        project.TumourLayers.Should().ContainSingle();
        project.TumourLayers[0].TumourTemplateEnum.Should().Be(TumourTemplate.Triangle);
        project.TumourLayers[0].TumourLength.GetValue(0).Should().BeApproximately(34, 1e-9);
        project.TumourLayers[0].TumourDistance.GetValue(0).Should().BeApproximately(118, 1e-9);
        serializer.Serialize(project).Should().NotContain("PreviewHitObject");
    }

    [TestMethod]
    public void Deserialize_WithSlideratorFixture_ReplacesGraphConstructorAnchors()
    {
        // Arrange
        string json = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "Projects",
                "slideratorproject.json"));
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SlideratorProject>(json);

        // Assert
        project.GraphState.Anchors.Should().HaveCount(3);
        project.GraphState.Anchors.Select(anchor => anchor.Pos.X).Should().Equal(0, 5.5, 16);
    }

    [TestMethod]
    public void Serialize_WithTumourGeneratorProject_UsesLegacyRootAndLayerNames()
    {
        // Arrange
        TumourGeneratorProject project = new();
        LegacyProjectJsonSerializer serializer = new();

        // Act
        string json = serializer.Serialize(project);

        // Assert
        json.Should().Contain("Mapping_Tools.Viewmodels.TumourGeneratorVm, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourLayer, Mapping Tools");
        json.Should().Contain("TumourTemplateEnum");
    }
}
