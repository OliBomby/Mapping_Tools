using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Application.Tools.Sliderator.Models;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class VersionedProjectJsonSerializerTests
{
    [TestMethod]
    public void Serialize_WithModelShapedProject_WritesVersionAndNoClrMetadata()
    {
        // Arrange
        VersionedProjectJsonSerializer serializer = new();
        SimpleDocument project = new() { Name = "current" };

        // Act
        string json = serializer.Serialize(project);

        // Assert
        json.Should().Contain("\"$schema\": \"mapping-tools.project\"");
        json.Should().Contain("\"$version\": 1");
        json.Should().Contain("\"Name\": \"current\"");
        json.Should().NotContain("$type");
        json.Should().NotContain("Mapping_Tools.");
    }

    [TestMethod]
    public void Deserialize_WithLegacyProject_UsesCompatibilityReaderAndCanonicalSave()
    {
        // Arrange
        const string legacyJson =
            "{\"$type\":\"Mapping_Tools.Viewmodels.HitsoundCopierVm, Mapping Tools\",\"PathFrom\":\"source.osu\",\"PathTo\":\"target.osu\"}";
        VersionedProjectJsonSerializer serializer = new();

        // Act
        HitsoundCopierServiceOptions project = serializer.Deserialize<HitsoundCopierServiceOptions>(legacyJson);
        string canonicalJson = serializer.Serialize(project);

        // Assert
        project.PathFrom.Should().Be("source.osu");
        canonicalJson.Should().Contain("\"$schema\": \"mapping-tools.project\"");
        canonicalJson.Should().NotContain("$type");
        canonicalJson.Should().NotContain("Mapping_Tools.");
    }

    [TestMethod]
    public void Serialize_WithGeometryGeneratorSettings_UsesStableIdentifiers()
    {
        // Arrange
        VersionedProjectJsonSerializer serializer = new();
        GeometryDashboardEngineOptions project = new();
        project.CurrentPreferences.GeneratorSettings[typeof(AnchorPointGenerator)] = new GeneratorSettings();

        // Act
        string json = serializer.Serialize(project);
        var reloaded = serializer.Deserialize<GeometryDashboardEngineOptions>(json);

        // Assert
        json.Should().Contain("\"anchor-point\"");
        json.Should().NotContain("AnchorPointGenerator");
        reloaded.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(AnchorPointGenerator));
    }

    [TestMethod]
    public void DeserializeAndSerialize_WithLegacySlideratorProject_PreservesGraphInCanonicalFormat()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "slideratorproject.json");
        VersionedProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SlideratorServiceOptions>(File.ReadAllText(fixture));
        string canonicalJson = serializer.Serialize(project);
        var reloaded = serializer.Deserialize<SlideratorServiceOptions>(canonicalJson);

        // Assert
        reloaded.GraphState.Anchors.Should().HaveCount(3);
        reloaded.GraphState.MaxX.Should().Be(16);
        canonicalJson.Should().Contain("\"$schema\": \"mapping-tools.project\"");
        canonicalJson.Should().NotContain("$type");
    }

    [TestMethod]
    public void DeserializeAndSerialize_WithLegacyLockedObjects_UsesStableIdentifiers()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "GeometryDashboard",
            "locked-virtual-objects.json");
        VersionedProjectJsonSerializer serializer = new();

        // Act
        RelevantObjectCollection objects = serializer.Deserialize<RelevantObjectCollection>(File.ReadAllText(fixture));
        string canonicalJson = serializer.Serialize(objects);
        RelevantObjectCollection reloaded = serializer.Deserialize<RelevantObjectCollection>(canonicalJson);

        // Assert
        canonicalJson.Should().Contain("\"relevant-point\"");
        canonicalJson.Should().Contain("\"relevant-circle\"");
        canonicalJson.Should().NotContain("$type");
        reloaded[typeof(RelevantPoint)].Should().HaveCount(10);
        reloaded[typeof(RelevantCircle)].Should().HaveCount(2);
        ((RelevantPoint)reloaded[typeof(RelevantPoint)][0]).Child.Should().Be(new Vector2(342, 98));
    }

    [TestMethod]
    public void Deserialize_WithFutureVersion_ThrowsUnsupportedVersionException()
    {
        // Arrange
        VersionedProjectJsonSerializer serializer = new();
        const string json = "{\"$schema\":\"mapping-tools.project\",\"$version\":99,\"Name\":\"future\"}";

        // Act
        Action act = () => serializer.Deserialize<SimpleDocument>(json);

        // Assert
        act.Should().Throw<JsonSerializationException>();
    }

    private sealed class SimpleDocument
    {
        public string Name { get; set; } = "";
    }
}
