using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class LegacyProjectCompatibilityTests
{
    [TestMethod]
    public void Deserialize_WithLegacyMapCleanerOptionsNamespace_UsesCurrentOptions()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.MapCleanerVm, Mapping Tools",
                             "MapCleanerArgs": {
                               "$type": "Mapping_Tools.Classes.Tools.MapCleanerArgs, Mapping Tools",
                               "ResnapObjects": false
                             }
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        MapCleanerServiceOptions project = serializer.Deserialize<MapCleanerServiceOptions>(json);

        // Assert
        project.MapCleanerArgs.ResnapObjects.Should().BeFalse();
    }

    [TestMethod]
    public void Deserialize_WithUppercaseHitsoundPreviewHelperAlias_UsesCurrentProject()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVM, Mapping Tools",
                             "Items": []
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundPreviewHelperServiceOptions project = serializer.Deserialize<HitsoundPreviewHelperServiceOptions>(json);

        // Assert
        project.Items.Should().BeEmpty();
    }

    [TestMethod]
    public void Deserialize_WithUppercaseHitsoundStudioAlias_UsesCurrentProject()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.HitsoundStudioVM, Mapping Tools",
                             "HitsoundLayers": []
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundStudioServiceOptions project = serializer.Deserialize<HitsoundStudioServiceOptions>(json);

        // Assert
        project.HitsoundLayers.Should().BeEmpty();
    }

    [TestMethod]
    public void Deserialize_WithUppercasePropertyTransformerAliasAndScalarFilter_UsesCurrentFilterArray()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.PropertyTransformerVM, Mapping Tools",
                             "MatchFilter": 10.0
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        PropertyTransformerServiceOptions project = serializer.Deserialize<PropertyTransformerServiceOptions>(json);

        // Assert
        project.MatchFilter.Should().Equal(10d);
    }

    [TestMethod]
    public void Deserialize_WithLegacyObservableCollectionMetadata_ConvertsToCurrentList()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.HitsoundPreviewHelperVm, Mapping Tools",
                             "Items": {
                               "$type": "System.Collections.ObjectModel.ObservableCollection`1[[Mapping_Tools.Classes.HitsoundStuff.HitsoundZone, Mapping Tools]], System",
                               "$values": [
                                 {
                                   "$type": "Mapping_Tools.Classes.HitsoundStuff.HitsoundZone, Mapping Tools",
                                   "Name": "legacy zone",
                                   "Filename": "legacy.wav"
                                 }
                               ]
                             }
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundPreviewHelperServiceOptions project = serializer.Deserialize<HitsoundPreviewHelperServiceOptions>(json);

        // Assert
        project.Items.Should().ContainSingle();
        project.Items[0].Name.Should().Be("legacy zone");
        project.Items[0].Filename.Should().Be("legacy.wav");
    }

    [TestMethod]
    public void Deserialize_WithLegacyHitsoundStudioSchemaListMetadata_ConvertsToCurrentList()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Viewmodels.HitsoundStudioVm, Mapping Tools",
                             "PreviousSampleSchema": {
                               "$type": "Mapping_Tools.Classes.HitsoundStuff.SampleSchema, Mapping Tools",
                               "Normal": {
                                 "$type": "System.Collections.Generic.List`1[[Mapping_Tools.Classes.HitsoundStuff.SampleGeneratingArgs, Mapping Tools]], mscorlib",
                                 "$values": [
                                   {
                                     "$type": "Mapping_Tools.Classes.HitsoundStuff.SampleGeneratingArgs, Mapping Tools",
                                     "Path": "legacy.wav"
                                   }
                                 ]
                               }
                             }
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundStudioServiceOptions project = serializer.Deserialize<HitsoundStudioServiceOptions>(json);

        // Assert
        project.PreviousSampleSchema.Should().NotBeNull();
        project.PreviousSampleSchema!["Normal"].Should().ContainSingle();
        project.PreviousSampleSchema["Normal"][0].Path.Should().Be("legacy.wav");
    }

    [TestMethod]
    public void Deserialize_WithLegacyGenericDictionaryEnumKey_ResolvesMigratedEnum()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "System.Collections.Generic.Dictionary`2[[Mapping_Tools.Classes.HitsoundStuff.HitsoundExporter+SampleExportFormat, Mapping Tools],[System.String, mscorlib]], mscorlib",
                             "Default": "Default"
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        object value = serializer.Deserialize<object>(json);

        // Assert
        value.Should().BeOfType<Dictionary<HitsoundStudioSampleExportFormat, string>>();
        ((Dictionary<HitsoundStudioSampleExportFormat, string>)value)[HitsoundStudioSampleExportFormat.Default]
            .Should().Be("Default");
    }

    [TestMethod]
    public void Deserialize_WithFlattenedHitsoundStudioSamplePaths_PreservesNestedPaths()
    {
        // Arrange
        const string json = """
                           {
                             "DefaultSample": {
                               "SampleSet": 1,
                               "Hitsound": 0,
                               "SamplePath": "default.wav",
                               "Priority": 3
                             },
                             "HitsoundLayers": [
                               {
                                 "Name": "legacy layer",
                                 "ImportType": "Hitsounds",
                                 "Path": "map.osu",
                                 "SamplePath": "layer.ogg",
                                 "Times": [1000.0],
                                 "Priority": 2
                               }
                             ]
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        HitsoundStudioServiceOptions project = serializer.Deserialize<HitsoundStudioServiceOptions>(json);

        // Assert
        project.DefaultSample.SampleArgs.Path.Should().Be("default.wav");
        project.HitsoundLayers.Should().ContainSingle();
        project.HitsoundLayers[0].ImportArgs.Path.Should().Be("map.osu");
        project.HitsoundLayers[0].ImportArgs.SamplePath.Should().Be("layer.ogg");
        project.HitsoundLayers[0].SampleArgs.Path.Should().Be("layer.ogg");
        project.HitsoundLayers[0].Times.Should().Equal(1000d);
    }

    [TestMethod]
    public void Deserialize_WithLegacyComboColourNamespaceAndCollection_UsesCurrentProject()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Classes.ComboColourStudio.ComboColourEngineOptions, Mapping Tools",
                             "ComboColours": {
                               "$type": "System.Collections.ObjectModel.ObservableCollection`1[[Mapping_Tools.Classes.BeatmapHelper.SpecialColour, Mapping Tools]], System",
                               "$values": [
                                 {
                                   "$type": "Mapping_Tools.Classes.BeatmapHelper.SpecialColour, Mapping Tools",
                                   "Name": "Combo1"
                                 }
                               ]
                             }
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        ComboColourEngineOptions project = serializer.Deserialize<ComboColourEngineOptions>(json);

        // Assert
        project.ComboColours.Should().ContainSingle();
        project.ComboColours[0].Name.Should().Be("Combo1");
    }

    [TestMethod]
    public void Deserialize_WithLegacyGeometryObjectNamespaceWithoutTools_LoadsVirtualPoint()
    {
        // Arrange
        const string json = """
                           {
                             "$type": "Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection, Mapping Tools",
                             "Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects.RelevantPoint, Mapping Tools": [
                               {
                                 "$type": "Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.RelevantObjects.RelevantPoint, Mapping Tools",
                                 "Child": {
                                   "$type": "Mapping_Tools.Classes.MathUtil.Vector2, Mapping Tools",
                                   "X": 12.0,
                                   "Y": 34.0
                                 },
                                 "IsLocked": true
                               }
                             ]
                           }
                           """;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        RelevantObjectCollection objects = serializer.Deserialize<RelevantObjectCollection>(json);

        // Assert
        objects[typeof(RelevantPoint)].Should().ContainSingle();
        RelevantPoint point = (RelevantPoint)objects[typeof(RelevantPoint)][0];
        point.Child.Should().Be(new Vector2(12, 34));
        point.IsLocked.Should().BeTrue();
    }
}
