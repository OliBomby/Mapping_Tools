using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Projects;

[TestClass]
public sealed class GeometryDashboardProjectPersistenceTests
{
    [TestMethod]
    public void DeserializeAndSerialize_LegacyGeometryDashboardProject_PreservesSettingsAndTypeNames()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SnappingToolsProject>(File.ReadAllText(fixture));
        string json = serializer.Serialize(project);

        // Assert
        project.CurrentPreferences.VisiblePlayfieldBoundary.Should().BeTrue();
        project.CurrentPreferences.UpdateMode.Should().Be(UpdateMode.HotkeyDown);
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(AnchorPointGenerator));
        project.CurrentPreferences.GeneratorSettings[typeof(SymmetryGenerator)]
            .Should().BeOfType<SymmetryGeneratorSettings>();
        ((SymmetryGeneratorSettings)project.CurrentPreferences.GeneratorSettings[typeof(SymmetryGenerator)])
            .AxisInputPredicate.Predicates.Should().ContainSingle(predicate => predicate.NeedSelected);
        project.CurrentPreferences.RelevantObjectPreferences["Virtual point preferences"].Color
            .Should().Be(RgbaColour.FromArgb(255, 0, 255, 255));
        project.SaveSlots.Should().ContainSingle(slot => slot.Name == "Save 1");
        json.Should().Contain("Mapping_Tools.Classes.Tools.SnappingTools.Serialization.SnappingToolsProject, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Generators.AnchorPointGenerator, Mapping Tools");
        json.Should().Contain("Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorSettingses.SymmetryGeneratorSettings, Mapping Tools");
        json.Should().Contain("\"Color\": \"#FF00FFFF\"");
    }

    [TestMethod]
    public void SnappingToolsProject_SaveAndLoadSlot_UsesIndependentPreferencesSnapshot()
    {
        // Arrange
        SnappingToolsProject project = new();
        SnappingToolsSaveSlot slot = new() { Name = "Test" };
        project.SaveSlots.Add(slot);
        project.CurrentPreferences.AcceptableDifference = 70.1;

        // Act
        project.SaveToSlot(slot);
        project.CurrentPreferences.AcceptableDifference = 2;
        project.CurrentPreferences.SnapHotkey.Key = 1;
        project.LoadFromSlot(slot);

        // Assert
        project.CurrentPreferences.AcceptableDifference.Should().Be(70.1);
        project.CurrentPreferences.SnapHotkey.Key.Should().Be(56);
    }

    [TestMethod]
    public void Deserialize_LegacyGeometryDashboardProjectWithAssemblyVersionMetadata_UsesTheSameCompatibilityAliases()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        string versionedJson = File.ReadAllText(fixture).Replace(
            ", Mapping Tools\"",
            ", Mapping Tools, Version=99.0.0.0, Culture=neutral, PublicKeyToken=null\"",
            StringComparison.Ordinal);
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SnappingToolsProject>(versionedJson);

        // Assert
        project.CurrentPreferences.RelevantObjectPreferences.Should().ContainKey(RelevantPoint.PreferencesNameStatic);
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(AnchorPointGenerator));
    }

    [TestMethod]
    public void Deserialize_IntermediateCoreGeometryDashboardProject_UsesCurrentNamespaceFallbacks()
    {
        // Arrange
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", "geometrydashboardproject.json");
        string intermediateJson = File.ReadAllText(fixture)
            .Replace("Mapping_Tools.Classes", "Mapping_Tools.Core.Classes", StringComparison.Ordinal)
            .Replace("Mapping Tools", "Mapping_Tools.Core", StringComparison.Ordinal);
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SnappingToolsProject>(intermediateJson);

        // Assert
        project.CurrentPreferences.GeneratorSettings.Should().ContainKey(typeof(SymmetryGenerator));
        project.SaveSlots.Should().Contain(slot => slot.Name == "Save 1");
    }

    [TestMethod]
    public void Deserialize_WithWaveZeroSnappingToolsFixture_PreservesPreferences()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Projects",
            "snappingtoolsproject.json");
        LegacyProjectJsonSerializer serializer = new();

        // Act
        var project = serializer.Deserialize<SnappingToolsProject>(File.ReadAllText(fixture));

        // Assert
        project.CurrentPreferences.InceptionLevel.Should().Be(5);
        project.CurrentPreferences.RelevantObjectPreferences.Should().HaveCount(3);
        project.CurrentPreferences.RelevantObjectPreferences.Should().ContainKey("Virtual point preferences");
    }
}
